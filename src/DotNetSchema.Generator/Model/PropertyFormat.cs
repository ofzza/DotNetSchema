using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using DotNetSchema.Generator.Diagnostics;
using DotNetSchema.Generator.Reflection;

namespace DotNetSchema.Generator.Model;

/// <summary>
/// Applies <c>[DotNetSchemaFormat]</c>: the <c>format</c> and <c>pattern</c> a property declares for its
/// values, replacing whatever its type's own schema says, without touching the <c>type</c>.
/// </summary>
/// <remarks>
/// Like a type mapping, the declaration is the model author's unchecked assertion — System.Text.Json neither
/// writes nor validates anything differently because of it. It is applied to a property's schema before the
/// DataAnnotations keywords, so an <c>[EmailAddress]</c> that disagrees with it is the one that gives way.
/// </remarks>
/// <param name="diagnostics">Where a malformed or misplaced declaration is reported.</param>
public sealed class PropertyFormat(IList<SchemaDiagnostic> diagnostics)
{
  private static readonly string AttributeName = typeof(DotNetSchemaFormatAttribute).FullName!;

  /// <summary>
  /// Applies the property's declaration, if it has one, to <paramref name="body" />: the property's schema
  /// before it is widened for null. On an array or a dictionary the declaration reaches through to the
  /// elements or values, at any depth; never to dictionary keys.
  /// </summary>
  public void Apply(JsonObject body, SchemaTypeRef reference, PropertyInfo property, string owner)
  {
    if (Read(property) is not { } data)
    {
      return;
    }

    var format = data.ConstructorArguments is [{ Value: string value }] ? value : null;
    var pattern = data.NamedArguments.FirstOrDefault(a => a.MemberName == "Pattern").TypedValue.Value as string;

    if (Validate(format, pattern) is { } reason)
    {
      Report(SchemaDiagnosticSeverity.Error, SchemaDiagnosticCodes.InvalidPropertyFormat, owner, property.Name,
        $"[DotNetSchemaFormat] on property '{owner}.{property.Name}' is ignored: {reason}.");
      return;
    }

    format = string.IsNullOrWhiteSpace(format) ? null : format.Trim();
    pattern = string.IsNullOrEmpty(pattern) ? null : pattern;

    var (leaf, kind) = Leaf(body, reference);

    if (kind is not (SchemaTypeKind.Scalar or SchemaTypeKind.Mapped or SchemaTypeKind.Binary))
    {
      Report(SchemaDiagnosticSeverity.Warning, SchemaDiagnosticCodes.PropertyFormatNotApplicable, owner, property.Name,
        $"[DotNetSchemaFormat] on property '{owner}.{property.Name}' is ignored: its value is " +
        $"{Describe(kind)}, which is described by its own schema and takes no format or pattern.");
      return;
    }

    if (pattern is not null && !IsString(leaf))
    {
      Report(SchemaDiagnosticSeverity.Warning, SchemaDiagnosticCodes.PropertyFormatNotApplicable, owner, property.Name,
        $"[DotNetSchemaFormat] on property '{owner}.{property.Name}' is ignored: a pattern applies only to a " +
        "string value, and this one is not written as a string.");
      return;
    }

    if (format is not null)
    {
      leaf["format"] = format;
    }

    if (pattern is not null)
    {
      leaf["pattern"] = pattern;
    }
  }

  /// <summary>
  /// Finds the attribute on the property or, for a positional record, on the primary-constructor parameter,
  /// which is where it lands when written without a <c>property:</c> target.
  /// </summary>
  private static CustomAttributeData? Read(PropertyInfo property) =>
    MetadataFacts.FindAttribute(property, AttributeName) ??
    (DecoratorReader.PrimaryParameter(property) is { } parameter
      ? parameter.GetCustomAttributesData().FirstOrDefault(d => d.AttributeType.FullName == AttributeName)
      : null);

  /// <summary>Checks a declaration, returning why it is unusable, or <see langword="null" /> when it is fine.</summary>
  private static string? Validate(string? format, string? pattern)
  {
    if (format is not null && string.IsNullOrWhiteSpace(format))
    {
      return "its format is empty";
    }

    if (format is null && string.IsNullOrEmpty(pattern))
    {
      return "it declares neither a format nor a pattern";
    }

    if (!string.IsNullOrEmpty(pattern))
    {
      try
      {
        _ = new Regex(pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
      }
      catch (ArgumentException exception)
      {
        return $"its pattern is not a valid regular expression ({exception.Message})";
      }
    }

    return null;
  }

  /// <summary>
  /// Walks down through arrays and dictionaries to the schema the values are described by. An element that
  /// is itself nullable has been widened by then, but only a scalar is widened in place, with a type array,
  /// and a format or pattern beside that says nothing about the null.
  /// </summary>
  private static (JsonObject Leaf, SchemaTypeKind Kind) Leaf(JsonObject body, SchemaTypeRef reference)
  {
    while (reference.Kind is SchemaTypeKind.Array or SchemaTypeKind.Map)
    {
      var keyword = reference.Kind == SchemaTypeKind.Array ? "items" : "additionalProperties";
      body = (JsonObject)body[keyword]!;
      reference = reference.Item!;
    }

    return (body, reference.Kind);
  }

  private static bool IsString(JsonObject schema) => schema["type"] switch
  {
    JsonValue value => value.GetValue<string>() == "string",
    JsonArray types => types.Any(t => t?.GetValue<string>() == "string"),
    _ => false,
  };

  private static string Describe(SchemaTypeKind kind) => kind switch
  {
    SchemaTypeKind.Model => "a model",
    SchemaTypeKind.Enum => "an enum",
    _ => "written by a converter whose shape is unknown",
  };

  private void Report(SchemaDiagnosticSeverity severity, string code, string owner, string member, string message) =>
    diagnostics.Add(new SchemaDiagnostic(severity, code, message, owner, member));
}
