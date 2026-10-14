using System.Text.RegularExpressions;
using DotNetSchema.Generator.Diagnostics;
using DotNetSchema.Generator.Reflection;

namespace DotNetSchema.Generator.Model;

/// <summary>Where a type's inline schema was declared.</summary>
public enum TypeMappingSource
{
  /// <summary>A <see cref="SchemaGenerationOptions.TypeMappings" /> entry — the build's <c>DotNetSchemaType</c> item.</summary>
  Configuration,

  /// <summary><c>[DotNetSchemaType]</c> on the type itself.</summary>
  Attribute,
}

/// <summary>A validated type mapping: the inline schema a type is described by, and where it was declared.</summary>
/// <param name="Type">The JSON Schema <c>type</c>, lower-case.</param>
/// <param name="Format">The <c>format</c>, or <see langword="null" />.</param>
/// <param name="Pattern">The <c>pattern</c>, or <see langword="null" />; only ever set on a <c>string</c>.</param>
/// <param name="Source">Where it was declared, for the <c>$comment</c> that explains the missing <c>$ref</c>.</param>
public sealed record ResolvedTypeMapping(string Type, string? Format, string? Pattern, TypeMappingSource Source);

/// <summary>
/// Decides whether a type is described by a declared inline schema, and which one: a configured mapping
/// first, so a host can correct a library; then <c>[DotNetSchemaType]</c> on the type.
/// </summary>
/// <remarks>
/// One instance serves a whole generation run, across every document, because a configured mapping counts as
/// used when <em>any</em> document reaches its type — <see cref="Unused" /> can only be answered at the end.
/// <para>
/// A generic type is looked up by its definition, so one mapping covers every construction. A constructed
/// generic's <c>FullName</c> embeds assembly-qualified arguments that differ between a runtime and a
/// metadata-only read, and is no use as a key anyway.
/// </para>
/// </remarks>
public sealed class TypeMappingResolver
{
  private static readonly string AttributeName = typeof(DotNetSchemaTypeAttribute).FullName!;

  private static readonly string[] JsonTypes = ["string", "number", "integer", "boolean"];

  private readonly Dictionary<string, ResolvedTypeMapping> _configured = new(StringComparer.Ordinal);
  private readonly HashSet<string> _used = new(StringComparer.Ordinal);
  private readonly Dictionary<string, (ResolvedTypeMapping? Mapping, SchemaDiagnostic? Problem)> _declared =
    new(StringComparer.Ordinal);

  /// <summary>Validates the configured mappings.</summary>
  public TypeMappingResolver(SchemaGenerationOptions options)
  {
    List<SchemaDiagnostic> problems = [];

    foreach (var entry in options.TypeMappings)
    {
      var name = entry.TypeName.Trim();
      if (name.Length == 0)
      {
        problems.Add(Invalid("A DotNetSchemaType item names no type and is ignored.", null));
        continue;
      }

      if (Validate(entry.Type, entry.Format, entry.Pattern, TypeMappingSource.Configuration, out var mapping) is { } reason)
      {
        problems.Add(Invalid($"The DotNetSchemaType item for '{name}' is ignored: {reason}.", name));
        continue;
      }

      // Later wins, as a later MSBuild item or property would.
      _configured[name] = mapping!;
    }

    ConfigurationDiagnostics = problems;
  }

  /// <summary>Problems with the configured mappings, each of which was ignored.</summary>
  public IReadOnlyList<SchemaDiagnostic> ConfigurationDiagnostics { get; }

  /// <summary>
  /// The mapping in force for a type, or <see langword="null" /> when it is described by its shape. A malformed
  /// <c>[DotNetSchemaType]</c> is reported to <paramref name="diagnostics" /> every time it is reached, so each
  /// document that depends on it is withheld, and is otherwise ignored.
  /// </summary>
  public ResolvedTypeMapping? Resolve(Type type, IList<SchemaDiagnostic> diagnostics)
  {
    var definition = type.IsGenericType && !type.IsGenericTypeDefinition ? type.GetGenericTypeDefinition() : type;

    if (definition.FullName is { } name && _configured.TryGetValue(name, out var configured))
    {
      _used.Add(name);
      return configured;
    }

    var key = MetadataFacts.TypeKey(definition);
    if (!_declared.TryGetValue(key, out var declared))
    {
      declared = Read(definition);
      _declared[key] = declared;
    }

    if (declared.Problem is { } problem)
    {
      diagnostics.Add(problem);
    }

    return declared.Mapping;
  }

  /// <summary>
  /// A warning for every configured mapping no document reached — almost always a misspelt name, which would
  /// otherwise leave the type quietly described by its shape.
  /// </summary>
  public IEnumerable<SchemaDiagnostic> Unused() =>
    _configured.Keys
      .Where(name => !_used.Contains(name))
      .Order(StringComparer.Ordinal)
      .Select(name => new SchemaDiagnostic(
        SchemaDiagnosticSeverity.Warning,
        SchemaDiagnosticCodes.TypeMappingUnused,
        $"The DotNetSchemaType item for '{name}' matched no type reached by any exported document. Check the " +
        "full name: a nested type is written Outer+Inner, and a generic type by its definition with its " +
        "arity, as Namespace.Name`1.",
        name));

  private static (ResolvedTypeMapping?, SchemaDiagnostic?) Read(Type type)
  {
    if (MetadataFacts.FindAttribute(type, AttributeName) is not { } data)
    {
      return (null, null);
    }

    var kind = data.ConstructorArguments is [{ Value: string value }] ? value : string.Empty;
    var format = Named(data, "Format");
    var pattern = Named(data, "Pattern");

    return Validate(kind, format, pattern, TypeMappingSource.Attribute, out var mapping) is { } reason
      ? (null, Invalid($"[DotNetSchemaType] on '{type.FullName}' is ignored: {reason}.", type.FullName))
      : (mapping, null);
  }

  /// <summary>Checks one mapping, returning why it is unusable, or <see langword="null" /> when it is fine.</summary>
  private static string? Validate(
    string type,
    string? format,
    string? pattern,
    TypeMappingSource source,
    out ResolvedTypeMapping? mapping)
  {
    mapping = null;

    // Case-insensitive, because an MSBuild author writing Type="String" means exactly what they say.
    var kind = JsonTypes.FirstOrDefault(t => string.Equals(t, type.Trim(), StringComparison.OrdinalIgnoreCase));
    if (kind is null)
    {
      return $"its type '{type}' is not one of {string.Join(", ", JsonTypes)}";
    }

    format = string.IsNullOrWhiteSpace(format) ? null : format.Trim();
    pattern = string.IsNullOrEmpty(pattern) ? null : pattern;

    if (pattern is not null)
    {
      // JSON Schema says nothing of a pattern on anything but a string, so one here would constrain nothing.
      if (kind != "string")
      {
        return $"a pattern applies only to the string type, not to '{kind}'";
      }

      try
      {
        _ = new Regex(pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
      }
      catch (ArgumentException exception)
      {
        return $"its pattern is not a valid regular expression ({exception.Message})";
      }
    }

    mapping = new ResolvedTypeMapping(kind, format, pattern, source);
    return null;
  }

  private static SchemaDiagnostic Invalid(string message, string? name) =>
    new(SchemaDiagnosticSeverity.Error, SchemaDiagnosticCodes.InvalidTypeMapping, message, name);

  private static string? Named(System.Reflection.CustomAttributeData data, string name) =>
    data.NamedArguments.FirstOrDefault(a => a.MemberName == name).TypedValue.Value as string;
}
