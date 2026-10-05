using System.Text.Json;
using System.Text.Json.Nodes;
using DotNetSchema.Generator.Model;
using DotNetSchema.Generator.Reflection;

namespace DotNetSchema.Generator.Emit;

/// <summary>Turns a resolved closure into the text of one JSON Schema document.</summary>
/// <param name="options">The generation options in force.</param>
/// <param name="names">Definition name per type, from <see cref="DefinitionNameResolver" />.</param>
/// <param name="diagnostics">Where the classifier reports anything it cannot map.</param>
public sealed class SchemaDocumentWriter(
  SchemaGenerationOptions options,
  IReadOnlyDictionary<string, string> names,
  IList<SchemaDiagnostic> diagnostics)
{
  private const string Dialect = "https://json-schema.org/draft/2020-12/schema";

  private readonly TypeClassifier _classifier = new(options, diagnostics);

  /// <summary>Writes one document.</summary>
  /// <param name="fileName">Its file name, which also supplies the <c>$id</c> and <c>title</c>.</param>
  /// <param name="types">Every type to define, in the order they are to be emitted.</param>
  /// <param name="roots">The explicitly marked types, named in a comment so the file is self-describing.</param>
  public SchemaDocument Write(string fileName, IReadOnlyList<Type> types, IReadOnlyList<Type> roots)
  {
    var stem = Path.GetFileNameWithoutExtension(fileName);
    var definitions = new JsonObject();

    var ordered = types
      .Select(type => (Name: names[MetadataFacts.TypeKey(type)], Type: type))
      .OrderBy(entry => entry.Name, StringComparer.Ordinal)
      .ToList();

    foreach (var (name, type) in ordered)
    {
      definitions[name] = type.IsEnum ? EnumDefinition(type, asString: null) : ModelDefinition(type);
    }

    var rootNames = roots
      .Select(type => names[MetadataFacts.TypeKey(type)])
      .OrderBy(name => name, StringComparer.Ordinal)
      .ToList();

    var document = new JsonObject { ["$schema"] = Dialect, ["$id"] = options.BaseUri + stem };

    if (options.EmitTitles)
    {
      document["title"] = stem;
    }

    if (options.EmitComments)
    {
      // There is no single root to promote, so the marked types are named here instead. Without this the
      // file gives a consumer no way to tell an entry point from a type merely pulled in behind one.
      document["$comment"] =
        $"Definition bundle. Exported types: {string.Join(", ", rootNames)}. " +
        "Reference one as #/$defs/<name>.";
    }

    document["$defs"] = definitions;

    return new SchemaDocument(fileName, rootNames, [.. ordered.Select(e => e.Name)], Render(document));
  }

  private static string Render(JsonObject document)
  {
    using var buffer = new MemoryStream();
    using (var writer = new Utf8JsonWriter(buffer, JsonWriterDefaults.Options))
    {
      document.WriteTo(writer);
    }

    // Utf8JsonWriter does not terminate the last line, and both .editorconfig files ask for one.
    return System.Text.Encoding.UTF8.GetString(buffer.ToArray()) + "\n";
  }

  private JsonObject ModelDefinition(Type type)
  {
    var properties = new JsonObject();
    var required = new JsonArray();

    foreach (var member in JsonMemberReader.Read(type, options))
    {
      var reference = _classifier.Classify(
        member.Type,
        member.Nullability,
        member.ConverterTypeFullName,
        type.FullName ?? type.Name,
        member.Property.Name);

      if (reference.Kind == SchemaTypeKind.Unmapped)
      {
        continue;
      }

      properties[member.JsonName] = Schema(reference);

      // Driven by the C# required modifier alone. Nullability is a separate question: a required member
      // may still accept null, and an optional one may not.
      if (member.IsRequired)
      {
        required.Add(member.JsonName);
      }
    }

    var definition = new JsonObject { ["type"] = "object", ["properties"] = properties };
    if (required.Count > 0)
    {
      definition["required"] = required;
    }

    return definition;
  }

  private JsonObject EnumDefinition(Type type, bool? asString)
  {
    var members = EnumFacts.Members(type, options);
    var isString = asString ?? EnumFacts.IsStringConverted(type, null);

    if (isString)
    {
      return new JsonObject
      {
        ["type"] = "string",
        ["enum"] = new JsonArray([.. members.Select(m => (JsonNode)m.JsonName)]),
      };
    }

    var definition = new JsonObject { ["type"] = "integer" };

    if (options.EmitComments)
    {
      definition["$comment"] =
        $"{type.FullName} ({EnumFacts.UnderlyingType(type).FullName}). System.Text.Json writes the " +
        "numeric value here; as a dictionary key the same type is written as the member name instead.";
    }

    if (options.NumericEnums == NumericEnumStyle.Enum)
    {
      definition["enum"] = new JsonArray([.. members.Select(Constant)]);
      return definition;
    }

    definition["oneOf"] = new JsonArray(
      [.. members.Select(m => (JsonNode)new JsonObject { ["const"] = Constant(m), ["title"] = m.JsonName })]);
    return definition;
  }

  private static JsonNode Constant(EnumMember member) => member.Value switch
  {
    sbyte value => JsonValue.Create(value),
    byte value => JsonValue.Create(value),
    short value => JsonValue.Create(value),
    ushort value => JsonValue.Create(value),
    int value => JsonValue.Create(value),
    uint value => JsonValue.Create(value),
    long value => JsonValue.Create(value),
    ulong value => JsonValue.Create(value),
    _ => JsonValue.Create(0),
  };

  private JsonObject Schema(SchemaTypeRef reference) => Nullable(Body(reference), reference.IsNullable);

  private JsonObject Body(SchemaTypeRef reference)
  {
    switch (reference.Kind)
    {
      case SchemaTypeKind.Scalar:
        return ScalarTable.Value(reference.Type, options) ?? new JsonObject();

      case SchemaTypeKind.Binary:
        return new JsonObject { ["type"] = "string", ["contentEncoding"] = "base64" };

      case SchemaTypeKind.Model:
        return Reference(reference.Type);

      case SchemaTypeKind.Enum:
        // A property-level converter that disagrees with the enum type's own gives this one position a
        // different shape from the shared definition, so it is spelled out here rather than referenced.
        return reference.AsString == EnumFacts.IsStringConverted(reference.Type, null)
          ? Reference(reference.Type)
          : EnumDefinition(reference.Type, reference.AsString);

      case SchemaTypeKind.Array:
        return new JsonObject { ["type"] = "array", ["items"] = Schema(reference.Item!) };

      case SchemaTypeKind.Map:
        return Map(reference);

      default:
        return new JsonObject();
    }
  }

  private JsonObject Map(SchemaTypeRef reference)
  {
    var map = new JsonObject { ["type"] = "object" };
    var key = reference.Key!;

    if (key.Kind == SchemaTypeKind.Enum)
    {
      if (options.EmitComments)
      {
        map["$comment"] =
          $"Keyed by {key.Type.FullName}. System.Text.Json writes an enum dictionary key as the member " +
          "name even under the default numeric converter, so this differs from the enum's value form.";
      }

      map["propertyNames"] = new JsonObject
      {
        ["type"] = "string",
        ["enum"] = new JsonArray([.. EnumFacts.Members(key.Type, options).Select(m => (JsonNode)m.JsonName)]),
      };
    }
    else if (ScalarTable.Key(key.Type) is { } constraint)
    {
      map["propertyNames"] = constraint;
    }

    map["additionalProperties"] = Schema(reference.Item!);
    return map;
  }

  private JsonObject Reference(Type type) =>
    new() { ["$ref"] = $"#/$defs/{Pointer(names[MetadataFacts.TypeKey(type)])}" };

  /// <summary>Escapes a definition name for use in a JSON Pointer, per RFC 6901.</summary>
  private static string Pointer(string name) => name.Replace("~", "~0").Replace("/", "~1");

  /// <summary>
  /// Widens a schema to admit null. A <c>$ref</c> or an enumeration has to go through <c>anyOf</c>: since
  /// Draft 2019-09 a <c>$ref</c> no longer suppresses its siblings, so <c>{"$ref":…,"type":"null"}</c>
  /// would read as "a Library <em>and</em> null", which nothing can satisfy. Anything else can simply take
  /// a second entry in its type array, because format, pattern and the numeric bounds are all
  /// type-conditional and say nothing about a null.
  /// </summary>
  private static JsonObject Nullable(JsonObject schema, bool isNullable)
  {
    if (!isNullable)
    {
      return schema;
    }

    var needsWrapping =
      schema.ContainsKey("$ref") || schema.ContainsKey("enum") ||
      schema.ContainsKey("const") || schema.ContainsKey("oneOf");

    if (needsWrapping)
    {
      return new JsonObject { ["anyOf"] = new JsonArray(schema, new JsonObject { ["type"] = "null" }) };
    }

    if (schema["type"] is JsonValue value && value.TryGetValue<string>(out var single))
    {
      schema["type"] = new JsonArray(single, "null");
    }

    return schema;
  }
}
