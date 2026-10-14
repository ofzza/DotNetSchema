using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace DotNetSchema.Generator.Tests;

/// <summary>
/// A validator covering exactly the JSON Schema keywords the generator emits, and nothing else.
/// </summary>
/// <remarks>
/// Hand-rolled rather than taken from a package, for the same reason <c>FixtureDigest</c> is: a
/// third-party validator would be testing itself as much as the schema, and the repository takes no
/// dependencies outside the test runner. Being deliberately narrow is a feature — if the generator starts
/// emitting a keyword this does not know, <see cref="Unsupported" /> fails rather than passing silently,
/// so the coverage cannot quietly fall behind the emitter.
/// </remarks>
public static class JsonSchemaValidator
{
  private static readonly string[] Known =
  [
    "$ref", "$comment", "title", "type", "enum", "const", "oneOf", "anyOf", "properties", "required",
    "items", "propertyNames", "additionalProperties", "minimum", "maximum", "exclusiveMinimum",
    "exclusiveMaximum", "minLength", "maxLength", "minItems", "maxItems", "pattern", "format",
    "contentEncoding",

    // Annotations, never checked: they describe the value beside them and constrain nothing.
    "description", "default", "deprecated", "x-enumNames", "x-decorators",
  ];

  /// <summary>
  /// Keywords whose value is data rather than a schema, so the keyword scan must not mistake the keys inside
  /// it — an <c>x-decorators</c> entry's <c>name</c> and <c>arguments</c>, say — for keywords.
  /// </summary>
  private static readonly string[] Opaque = ["default", "x-decorators"];

  /// <summary>Validates an instance against a document, returning one message per failure.</summary>
  /// <param name="instance">The serialised value to check.</param>
  /// <param name="document">The whole schema document, used to resolve references.</param>
  /// <param name="entryPoint">Name of the definition the instance is expected to match.</param>
  public static IReadOnlyList<string> Validate(JsonNode? instance, JsonObject document, string entryPoint)
  {
    List<string> failures = [];
    var defs = document["$defs"]!.AsObject();
    Check(instance, (JsonObject)defs[entryPoint]!, defs, "$", failures);
    return failures;
  }

  /// <summary>Every keyword the given document uses that this validator does not implement.</summary>
  public static IReadOnlyList<string> Unsupported(JsonObject document)
  {
    SortedSet<string> found = new(StringComparer.Ordinal);
    Walk(document);
    return [.. found.Except(["$schema", "$id", "$defs"], StringComparer.Ordinal)];

    void Walk(JsonNode? node)
    {
      switch (node)
      {
        case JsonObject o:
          foreach (var (key, value) in o)
          {
            // One level below "properties" and "$defs" the keys are names, not keywords, so that level is
            // stepped over rather than collected from.
            if (key is "properties" or "$defs")
            {
              foreach (var (_, nested) in value!.AsObject())
              {
                Walk(nested);
              }

              continue;
            }

            found.Add(key);
            if (!Opaque.Contains(key, StringComparer.Ordinal))
            {
              Walk(value);
            }
          }

          break;

        case JsonArray a:
          foreach (var item in a)
          {
            Walk(item);
          }

          break;
      }
    }
  }

  private static void Check(
    JsonNode? instance,
    JsonObject schema,
    JsonObject defs,
    string path,
    List<string> failures)
  {
    if (schema["$ref"]?.GetValue<string>() is { } reference)
    {
      var name = reference["#/$defs/".Length..].Replace("~1", "/").Replace("~0", "~");
      Check(instance, (JsonObject)defs[name]!, defs, path, failures);
      return;
    }

    if (schema["anyOf"] is JsonArray anyOf)
    {
      if (!anyOf.Any(option => Passes(instance, (JsonObject)option!, defs)))
      {
        failures.Add($"{path}: matched none of the anyOf alternatives.");
      }

      return;
    }

    if (schema["oneOf"] is JsonArray oneOf)
    {
      if (oneOf.Count(option => Passes(instance, (JsonObject)option!, defs)) != 1)
      {
        failures.Add($"{path}: did not match exactly one of the oneOf alternatives.");
      }

      return;
    }

    if (schema["const"] is { } constant && !JsonNode.DeepEquals(instance, constant))
    {
      failures.Add($"{path}: expected const {constant}, found {instance?.ToJsonString() ?? "null"}.");
      return;
    }

    if (!MatchesType(instance, schema["type"]))
    {
      failures.Add($"{path}: expected type {schema["type"]}, found {Describe(instance)}.");
      return;
    }

    if (schema["enum"] is JsonArray permitted && !permitted.Any(v => JsonNode.DeepEquals(instance, v)))
    {
      failures.Add($"{path}: {instance?.ToJsonString()} is not one of the permitted values.");
    }

    switch (instance)
    {
      case JsonValue value when value.TryGetValue<string>(out var text):
        CheckString(text, schema, path, failures);
        break;

      case JsonValue value when value.GetValueKind() == JsonValueKind.Number:
        CheckNumber(value.GetValue<decimal>(), schema, path, failures);
        break;

      case JsonObject o:
        CheckObject(o, schema, defs, path, failures);
        break;

      case JsonArray a:
        CheckArray(a, schema, defs, path, failures);
        break;
    }
  }

  private static void CheckString(string text, JsonObject schema, string path, List<string> failures)
  {
    if (schema["minLength"]?.GetValue<int>() is { } min && text.Length < min)
    {
      failures.Add($"{path}: shorter than minLength {min}.");
    }

    if (schema["maxLength"]?.GetValue<int>() is { } max && text.Length > max)
    {
      failures.Add($"{path}: longer than maxLength {max}.");
    }

    if (schema["pattern"]?.GetValue<string>() is { } pattern && !Regex.IsMatch(text, pattern))
    {
      failures.Add($"{path}: '{text}' does not match {pattern}.");
    }

    if (schema["format"]?.GetValue<string>() is { } format && !MatchesFormat(text, format))
    {
      failures.Add($"{path}: '{text}' is not a valid {format}.");
    }
  }

  private static void CheckNumber(decimal value, JsonObject schema, string path, List<string> failures)
  {
    if (schema["minimum"]?.GetValue<decimal>() is { } min && value < min)
    {
      failures.Add($"{path}: {value} is below minimum {min}.");
    }

    if (schema["maximum"]?.GetValue<decimal>() is { } max && value > max)
    {
      failures.Add($"{path}: {value} is above maximum {max}.");
    }

    if (schema["exclusiveMinimum"]?.GetValue<decimal>() is { } floor && value <= floor)
    {
      failures.Add($"{path}: {value} is not above exclusiveMinimum {floor}.");
    }

    if (schema["exclusiveMaximum"]?.GetValue<decimal>() is { } ceiling && value >= ceiling)
    {
      failures.Add($"{path}: {value} is not below exclusiveMaximum {ceiling}.");
    }
  }

  private static void CheckArray(
    JsonArray instance,
    JsonObject schema,
    JsonObject defs,
    string path,
    List<string> failures)
  {
    if (schema["minItems"]?.GetValue<int>() is { } min && instance.Count < min)
    {
      failures.Add($"{path}: fewer than minItems {min}.");
    }

    if (schema["maxItems"]?.GetValue<int>() is { } max && instance.Count > max)
    {
      failures.Add($"{path}: more than maxItems {max}.");
    }

    if (schema["items"] is JsonObject items)
    {
      for (var i = 0; i < instance.Count; i++)
      {
        Check(instance[i], items, defs, $"{path}[{i}]", failures);
      }
    }
  }

  private static void CheckObject(
    JsonObject instance,
    JsonObject schema,
    JsonObject defs,
    string path,
    List<string> failures)
  {
    if (schema["required"] is JsonArray required)
    {
      foreach (var name in required.Select(n => n!.GetValue<string>()).Where(n => !instance.ContainsKey(n)))
      {
        failures.Add($"{path}: required property '{name}' is absent.");
      }
    }

    var properties = schema["properties"] as JsonObject;
    var additional = schema["additionalProperties"] as JsonObject;
    var propertyNames = schema["propertyNames"] as JsonObject;

    foreach (var (name, value) in instance)
    {
      if (propertyNames is not null)
      {
        Check(JsonValue.Create(name), propertyNames, defs, $"{path}.{name}<key>", failures);
      }

      var member = properties?[name] as JsonObject ?? additional;
      if (member is not null)
      {
        Check(value, member, defs, $"{path}.{name}", failures);
      }
    }
  }

  private static bool Passes(JsonNode? instance, JsonObject schema, JsonObject defs)
  {
    List<string> failures = [];
    Check(instance, schema, defs, "$", failures);
    return failures.Count == 0;
  }

  private static bool MatchesType(JsonNode? instance, JsonNode? type) => type switch
  {
    null => true,
    JsonArray options => options.Any(option => MatchesType(instance, option)),
    _ => MatchesType(instance, type.GetValue<string>()),
  };

  private static bool MatchesType(JsonNode? instance, string type) => type switch
  {
    "null" => instance is null || instance.GetValueKind() == JsonValueKind.Null,
    "object" => instance is JsonObject,
    "array" => instance is JsonArray,
    "string" => instance?.GetValueKind() == JsonValueKind.String,
    "boolean" => instance?.GetValueKind() is JsonValueKind.True or JsonValueKind.False,
    "number" => instance?.GetValueKind() == JsonValueKind.Number,
    "integer" => instance?.GetValueKind() == JsonValueKind.Number &&
                 instance.GetValue<decimal>() == decimal.Truncate(instance.GetValue<decimal>()),
    _ => false,
  };

  private static bool MatchesFormat(string text, string format) => format switch
  {
    "uuid" => Guid.TryParseExact(text, "D", out _),
    "date" => DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
    "date-time" => DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out _),

    // Deliberately loose, like the attributes these come from: one '@' with something either side, and an
    // absolute URI.
    "email" => text.IndexOf('@') is > 0 and var at && at == text.LastIndexOf('@') && at < text.Length - 1,
    "uri" => Uri.TryCreate(text, UriKind.Absolute, out _),
    _ => true,
  };

  private static string Describe(JsonNode? instance) =>
    instance is null ? "null" : instance.GetValueKind().ToString().ToLowerInvariant();

  /// <summary>The keywords this validator implements.</summary>
  public static IReadOnlyList<string> KnownKeywords => Known;
}
