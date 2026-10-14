using System.Globalization;
using System.Reflection;
using System.Text.Json.Nodes;
using DotNetSchema.Generator.Diagnostics;
using DotNetSchema.Generator.Reflection;

namespace DotNetSchema.Generator.Model;

/// <summary>
/// Translates the well-known validation and description attributes into the standard JSON Schema keywords
/// that say the same thing, and attaches the <c>x-decorators</c> list beside them.
/// </summary>
/// <remarks>
/// Only attributes the options selected are ever seen here, so excluding one removes its keywords too.
/// <para>
/// These keywords describe validation the model <em>declares</em>. System.Text.Json enforces none of it —
/// it writes a <c>[Range(1, 10)]</c> property holding <c>42</c> without complaint — so they are a deliberate
/// step beyond "the schema describes what System.Text.Json writes", taken because a consumer of the schema
/// is usually the other side of the same validation.
/// </para>
/// <para>
/// DataAnnotations <c>[Required]</c> is deliberately absent. It is a non-null, non-empty check run by
/// validation after deserialisation, not the presence check JSON Schema's <c>required</c> makes; the latter
/// follows the C# <c>required</c> modifier and <c>[JsonRequired]</c>, both of which System.Text.Json enforces.
/// </para>
/// </remarks>
/// <param name="diagnostics">Where an attribute that does not fit its property is reported.</param>
public sealed class DecoratorKeywords(IList<SchemaDiagnostic> diagnostics)
{
  private const string DataAnnotations = "System.ComponentModel.DataAnnotations.";

  /// <summary>
  /// Keywords the mapping writes, in the order they are written, so the output does not depend on which
  /// attribute happened to be read first.
  /// </summary>
  private static readonly string[] Order =
  [
    "title", "description", "default", "deprecated", "minimum", "exclusiveMinimum", "maximum",
    "exclusiveMaximum", "minLength", "maxLength", "minItems", "maxItems", "pattern", "format",
  ];

  private static readonly HashSet<string> Numeric = new(StringComparer.Ordinal)
  {
    "System.SByte", "System.Byte", "System.Int16", "System.UInt16", "System.Int32", "System.UInt32",
    "System.Int64", "System.UInt64", "System.Single", "System.Double", "System.Decimal",
  };

  /// <summary>Annotates a model or enum definition with its type-level decorators.</summary>
  public static void Annotate(JsonObject definition, IReadOnlyList<Decorator> decorators)
  {
    var annotations = new Dictionary<string, JsonNode?>(StringComparer.Ordinal);
    foreach (var decorator in decorators)
    {
      Annotation(decorator, annotations, isProperty: false);
    }

    Write(definition, annotations);
    Attach(definition, decorators);
  }

  /// <summary>
  /// Adds the value constraints the decorators imply to a property's schema, before it is widened for
  /// null. Value keywords belong beside <c>type</c>, where they are type-conditional and say nothing about
  /// a null.
  /// </summary>
  public void Constrain(
    JsonObject body,
    SchemaTypeRef reference,
    IReadOnlyList<Decorator> decorators,
    string owner,
    string member)
  {
    var shape = ShapeOf(body, reference);

    foreach (var decorator in decorators)
    {
      var applied = decorator.Name switch
      {
        DataAnnotations + "RangeAttribute" => Range(body, shape, decorator.Data),
        DataAnnotations + "MinLengthAttribute" => Length(body, shape, minimum: Int(decorator.Data, 0), maximum: null),
        DataAnnotations + "MaxLengthAttribute" => Length(body, shape, minimum: null, maximum: MaxLength(decorator.Data)),
        DataAnnotations + "LengthAttribute" =>
          Length(body, shape, minimum: Int(decorator.Data, 0), maximum: Int(decorator.Data, 1)),
        DataAnnotations + "StringLengthAttribute" => shape == Shape.String &&
          Length(body, shape, minimum: Named<int>(decorator.Data, "MinimumLength"), maximum: Int(decorator.Data, 0)),
        DataAnnotations + "RegularExpressionAttribute" => shape == Shape.String &&
          decorator.Data.ConstructorArguments is [{ Value: string pattern }] &&
          Exclusive(body, "pattern", $"^(?:{pattern})$"),
        DataAnnotations + "EmailAddressAttribute" => shape == Shape.String && Exclusive(body, "format", "email"),
        DataAnnotations + "UrlAttribute" => shape == Shape.String && Exclusive(body, "format", "uri"),
        "System.ComponentModel.DefaultValueAttribute" => DefaultFits(decorator.Data, reference),
        _ => true,
      };

      if (!applied)
      {
        diagnostics.Add(new SchemaDiagnostic(
          SchemaDiagnosticSeverity.Warning,
          SchemaDiagnosticCodes.DecoratorNotApplicable,
          $"Property '{owner}.{member}' carries [{decorator.Name}], which has no meaning for its schema " +
          "here. It is still listed under x-decorators, but contributes no keyword.",
          owner,
          member));
      }
    }
  }

  /// <summary>
  /// Adds a property's annotations and its <c>x-decorators</c> list to the outermost schema, after any
  /// widening for null — beside the <c>anyOf</c>, not inside one alternative of it.
  /// </summary>
  public static void Annotate(JsonObject schema, SchemaTypeRef reference, IReadOnlyList<Decorator> decorators)
  {
    var annotations = new Dictionary<string, JsonNode?>(StringComparer.Ordinal);
    foreach (var decorator in decorators)
    {
      Annotation(decorator, annotations, isProperty: true);
    }

    if (annotations.Remove("default", out var value) && DefaultFits(FindDefault(decorators)!, reference))
    {
      annotations["default"] = value;
    }

    Write(schema, annotations);
    Attach(schema, decorators);
  }

  private static void Annotation(Decorator decorator, Dictionary<string, JsonNode?> annotations, bool isProperty)
  {
    var data = decorator.Data;

    // The first decorator in sorted order to state a keyword keeps it, which makes the choice stable.
    switch (decorator.Name)
    {
      case "System.ComponentModel.DescriptionAttribute" when data.ConstructorArguments is [{ Value: string text }]:
        annotations.TryAdd("description", text);
        break;

      case "System.ComponentModel.DisplayNameAttribute" when data.ConstructorArguments is [{ Value: string text }]:
        annotations.TryAdd("title", text);
        break;

      case DataAnnotations + "DisplayAttribute":
        if (Named<string>(data, "Name") is { } name)
        {
          annotations.TryAdd("title", name);
        }

        if (Named<string>(data, "Description") is { } description)
        {
          annotations.TryAdd("description", description);
        }

        break;

      case "System.ObsoleteAttribute":
        annotations.TryAdd("deprecated", true);
        break;

      case "System.ComponentModel.DefaultValueAttribute" when isProperty && data.ConstructorArguments.Count == 1:
        annotations.TryAdd("default", DecoratorReader.Render(data.ConstructorArguments[0]));
        break;
    }
  }

  private static CustomAttributeData? FindDefault(IReadOnlyList<Decorator> decorators) =>
    decorators.FirstOrDefault(d => d.Name == "System.ComponentModel.DefaultValueAttribute")?.Data;

  /// <summary>
  /// A default fits a scalar position, given as a plain value. The <c>(Type, string)</c> overload names a
  /// value only a converter could produce, and an enum default would need the enum's own value form, so
  /// both are left to <c>x-decorators</c>.
  /// </summary>
  private static bool DefaultFits(CustomAttributeData data, SchemaTypeRef reference) =>
    reference.Kind == SchemaTypeKind.Scalar &&
    data.ConstructorArguments is [{ } argument] &&
    !argument.ArgumentType.IsEnum &&
    argument.Value is not Type and not System.Collections.ObjectModel.ReadOnlyCollection<CustomAttributeTypedArgument>;

  private static void Write(JsonObject schema, Dictionary<string, JsonNode?> keywords)
  {
    foreach (var keyword in Order)
    {
      if (keywords.TryGetValue(keyword, out var value))
      {
        schema[keyword] = value;
      }
    }
  }

  private static void Attach(JsonObject schema, IReadOnlyList<Decorator> decorators)
  {
    if (decorators.Count > 0)
    {
      schema["x-decorators"] = new JsonArray([.. decorators.Select(d => d.Rendered.DeepClone())]);
    }
  }

  private static bool Range(JsonObject body, Shape shape, CustomAttributeData data)
  {
    if (shape != Shape.Number)
    {
      return false;
    }

    JsonNode? minimum;
    JsonNode? maximum;

    switch (data.ConstructorArguments)
    {
      case [{ Value: int low }, { Value: int high }]:
        (minimum, maximum) = (JsonValue.Create(low), JsonValue.Create(high));
        break;

      case [{ Value: double low }, { Value: double high }] when double.IsFinite(low) && double.IsFinite(high):
        (minimum, maximum) = (JsonValue.Create(low), JsonValue.Create(high));
        break;

      // The (Type, string, string) overload states its bounds as text, parsed by the type it names. Only a
      // numeric type has a bound JSON Schema can state; a DateTime range, say, stays in x-decorators.
      case [{ Value: Type type }, { Value: string low }, { Value: string high }]
        when type.FullName is { } name && Numeric.Contains(name) &&
             decimal.TryParse(low, NumberStyles.Float, CultureInfo.InvariantCulture, out var lowValue) &&
             decimal.TryParse(high, NumberStyles.Float, CultureInfo.InvariantCulture, out var highValue):
        (minimum, maximum) = (JsonValue.Create(lowValue), JsonValue.Create(highValue));
        break;

      default:
        return false;
    }

    if (Named<bool>(data, "MinimumIsExclusive") == true)
    {
      Tighten(body, "exclusiveMinimum", minimum, upper: false);
    }
    else
    {
      Tighten(body, "minimum", minimum, upper: false);
    }

    if (Named<bool>(data, "MaximumIsExclusive") == true)
    {
      Tighten(body, "exclusiveMaximum", maximum, upper: true);
    }
    else
    {
      Tighten(body, "maximum", maximum, upper: true);
    }

    return true;
  }

  /// <summary>
  /// Lengths of a string, or counts of an array. Not of a <c>byte[]</c>: its length in the document is that
  /// of its base64 text, which the attribute does not describe.
  /// </summary>
  private static bool Length(JsonObject body, Shape shape, int? minimum, int? maximum)
  {
    var (low, high) = shape switch
    {
      Shape.String => ("minLength", "maxLength"),
      Shape.Array => ("minItems", "maxItems"),
      _ => (null, null),
    };

    if (low is null || high is null)
    {
      return false;
    }

    if (minimum is { } min && min > 0)
    {
      Tighten(body, low, JsonValue.Create(min), upper: false);
    }

    if (maximum is { } max && max >= 0)
    {
      Tighten(body, high, JsonValue.Create(max), upper: true);
    }

    return true;
  }

  /// <summary>
  /// Sets a bound unless the schema already states a tighter one — an <c>int</c> carries its own range, and
  /// a <c>char</c> its own length.
  /// </summary>
  private static void Tighten(JsonObject body, string keyword, JsonNode? bound, bool upper)
  {
    if (bound is null)
    {
      return;
    }

    if (body[keyword] is { } existing)
    {
      var current = AsDouble(existing);
      var proposed = AsDouble(bound);
      if (upper ? proposed >= current : proposed <= current)
      {
        return;
      }
    }

    body[keyword] = bound;
  }

  /// <summary>
  /// Sets a keyword that cannot be combined — a schema has one <c>pattern</c> and one <c>format</c> — and
  /// reports the clash when the type's own mapping already set a different one (a <c>Guid</c>'s
  /// <c>uuid</c>, a <c>TimeOnly</c>'s pattern).
  /// </summary>
  private static bool Exclusive(JsonObject body, string keyword, string value)
  {
    if (body[keyword] is { } existing)
    {
      return existing.GetValue<string>() == value;
    }

    body[keyword] = value;
    return true;
  }

  private static double AsDouble(JsonNode node) =>
    double.Parse(node.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture);

  /// <summary>
  /// What a property's schema is, for the purpose of which keywords can apply to it. A <c>$ref</c>, an
  /// enum or a map takes none of the value keywords.
  /// </summary>
  private static Shape ShapeOf(JsonObject body, SchemaTypeRef reference) => reference.Kind switch
  {
    SchemaTypeKind.Array => Shape.Array,
    SchemaTypeKind.Scalar or SchemaTypeKind.Mapped when body["type"]?.GetValue<string>() is "integer" or "number" =>
      Shape.Number,
    SchemaTypeKind.Scalar or SchemaTypeKind.Mapped when body["type"]?.GetValue<string>() == "string" =>
      Shape.String,
    _ => Shape.Other,
  };

  private static int? Int(CustomAttributeData data, int index) =>
    data.ConstructorArguments.Count > index && data.ConstructorArguments[index].Value is int value ? value : null;

  /// <summary><c>[MaxLength]</c> without an argument, or with -1, means "as long as the platform allows".</summary>
  private static int? MaxLength(CustomAttributeData data) => Int(data, 0) is { } value && value >= 0 ? value : null;

  private static T? Named<T>(CustomAttributeData data, string name)
  {
    foreach (var argument in data.NamedArguments)
    {
      if (argument.MemberName == name && argument.TypedValue.Value is T value)
      {
        return value;
      }
    }

    return default;
  }

  private enum Shape
  {
    Other,
    Number,
    String,
    Array,
  }
}
