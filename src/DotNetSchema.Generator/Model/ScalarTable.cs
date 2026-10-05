using System.Text.Json.Nodes;

namespace DotNetSchema.Generator.Model;

/// <summary>
/// The mapping from each portable primitive to the JSON Schema that describes how System.Text.Json
/// actually writes it.
/// </summary>
/// <remarks>
/// Two of these entries are deliberately not what the obvious answer would be, and both are cases where
/// the registered format keyword would be a lie:
/// <list type="bullet">
/// <item><description>
/// <c>TimeSpan</c> is written by <c>Utf8Formatter</c> with the <c>'c'</c> specifier —
/// <c>[-][d.]hh:mm:ss[.fffffff]</c> — which is not an ISO-8601 duration, so <c>format: "duration"</c>
/// would not hold. A pattern is emitted instead.
/// </description></item>
/// <item><description>
/// <c>TimeOnly</c> goes through the same specifier and therefore carries no UTC offset, so it does not
/// satisfy RFC 3339 <c>full-time</c> and <c>format: "time"</c> would not hold either. System.Text.Json's
/// own schema exporter emits it anyway; this one does not.
/// </description></item>
/// </list>
/// A type serialises differently as a dictionary key than as a value — every key is a string — so the
/// two shapes are produced by separate methods.
/// </remarks>
public static class ScalarTable
{
  private const string TimeOnlyPattern = @"^([01]\d|2[0-3]):[0-5]\d:[0-5]\d(\.\d{1,7})?$";
  private const string TimeSpanPattern = @"^-?(\d+\.)?\d{2}:\d{2}:\d{2}(\.\d{1,7})?$";
  private const string IntegerKeyPattern = @"^-?(0|[1-9]\d*)$";

  private const string LongComment =
    "System.Int64. Bounds are omitted: they are not exactly representable in IEEE-754, and values beyond " +
    "2^53 lose precision in readers that parse JSON numbers as doubles.";

  private const string ULongComment =
    "System.UInt64. The upper bound is omitted: it is not exactly representable in IEEE-754.";

  private const string DecimalComment =
    "System.Decimal. Written as an unquoted JSON number with its scale preserved; a reader backed by an " +
    "IEEE-754 double cannot hold it exactly.";

  private const string TimeOnlyComment =
    "System.TimeOnly. System.Text.Json writes hh:mm:ss[.fffffff] with no UTC offset, so the JSON Schema " +
    "\"time\" format (RFC 3339 full-time) does not hold.";

  private const string TimeSpanComment =
    "System.TimeSpan. System.Text.Json writes [-][d.]hh:mm:ss[.fffffff], not an ISO-8601 duration, so the " +
    "\"duration\" format does not hold.";

  private const string DateTimeComment =
    "System.DateTime. The offset suffix is present only when DateTimeKind is Utc or Local; a " +
    "DateTimeKind.Unspecified value is written without one and would not satisfy \"date-time\".";

  /// <summary>Whether the type is one this table maps.</summary>
  public static bool IsScalar(Type type) => type.FullName is { } name && Names.Contains(name);

  /// <summary>
  /// Whether the type is a floating-point one, whose dictionary keys will not agree across languages.
  /// </summary>
  public static bool IsFloatingPoint(Type type) =>
    type.FullName is "System.Single" or "System.Double" or "System.Decimal";

  /// <summary>The schema for the type as a property value, or <see langword="null" /> if it is not a scalar.</summary>
  public static JsonObject? Value(Type type, SchemaGenerationOptions options)
  {
    var comment = options.EmitComments;

    return type.FullName switch
    {
      "System.Boolean" => new JsonObject { ["type"] = "boolean" },
      "System.SByte" => Integer(sbyte.MinValue, sbyte.MaxValue, options),
      "System.Byte" => Integer(byte.MinValue, byte.MaxValue, options),
      "System.Int16" => Integer(short.MinValue, short.MaxValue, options),
      "System.UInt16" => Integer(ushort.MinValue, ushort.MaxValue, options),
      "System.Int32" => Integer(int.MinValue, int.MaxValue, options),
      "System.UInt32" => Integer(uint.MinValue, uint.MaxValue, options),
      "System.Int64" => WideInteger(hasMinimum: false, LongComment, options),
      "System.UInt64" => WideInteger(hasMinimum: true, ULongComment, options),
      "System.Single" or "System.Double" => new JsonObject { ["type"] = "number" },
      "System.Decimal" => Commented(new JsonObject { ["type"] = "number" }, DecimalComment, comment),
      "System.Char" => new JsonObject { ["type"] = "string", ["minLength"] = 1, ["maxLength"] = 1 },
      "System.String" => new JsonObject { ["type"] = "string" },
      "System.Guid" => Formatted("uuid"),
      "System.DateOnly" => Formatted("date"),
      "System.TimeOnly" => Commented(Patterned(TimeOnlyPattern), TimeOnlyComment, comment),
      "System.TimeSpan" => Commented(Patterned(TimeSpanPattern), TimeSpanComment, comment),
      "System.DateTime" => Commented(Formatted("date-time"), DateTimeComment, comment),
      "System.DateTimeOffset" => Formatted("date-time"),
      _ => null,
    };
  }

  /// <summary>
  /// The constraint for the type used as a dictionary key, or <see langword="null" /> when it is a string
  /// and so constrains nothing worth saying.
  /// </summary>
  /// <remarks>
  /// Every one of these is a string, because a JSON property name is a string. The mapping is therefore
  /// not the value mapping with a different wrapper: an <c>int</c> key is a string matching a decimal
  /// pattern, not an integer.
  /// </remarks>
  public static JsonObject? Key(Type type)
  {
    return type.FullName switch
    {
      "System.String" => null,
      "System.Boolean" => new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("true", "false") },
      "System.SByte" or "System.Byte" or "System.Int16" or "System.UInt16" or "System.Int32" or
        "System.UInt32" or "System.Int64" or "System.UInt64" => Patterned(IntegerKeyPattern),
      "System.Single" or "System.Double" or "System.Decimal" => new JsonObject { ["type"] = "string" },
      "System.Char" => new JsonObject { ["type"] = "string", ["minLength"] = 1, ["maxLength"] = 1 },
      "System.Guid" => Formatted("uuid"),
      "System.DateOnly" => Formatted("date"),
      "System.TimeOnly" => Patterned(TimeOnlyPattern),
      "System.TimeSpan" => Patterned(TimeSpanPattern),
      "System.DateTime" or "System.DateTimeOffset" => Formatted("date-time"),
      _ => null,
    };
  }

  private static readonly HashSet<string> Names = new(StringComparer.Ordinal)
  {
    "System.Boolean", "System.SByte", "System.Byte", "System.Int16", "System.UInt16", "System.Int32",
    "System.UInt32", "System.Int64", "System.UInt64", "System.Single", "System.Double", "System.Decimal",
    "System.Char", "System.String", "System.Guid", "System.DateOnly", "System.TimeOnly", "System.TimeSpan",
    "System.DateTime", "System.DateTimeOffset",
  };

  private static JsonObject Formatted(string format) =>
    new() { ["type"] = "string", ["format"] = format };

  private static JsonObject Patterned(string pattern) =>
    new() { ["type"] = "string", ["pattern"] = pattern };

  private static JsonObject Integer(long minimum, long maximum, SchemaGenerationOptions options)
  {
    var schema = new JsonObject { ["type"] = "integer" };
    if (options.IntegerBounds == IntegerBoundsMode.None)
    {
      return schema;
    }

    schema["minimum"] = minimum;
    schema["maximum"] = maximum;
    return schema;
  }

  private static JsonObject WideInteger(bool hasMinimum, string comment, SchemaGenerationOptions options)
  {
    var schema = new JsonObject { ["type"] = "integer" };

    switch (options.IntegerBounds)
    {
      case IntegerBoundsMode.None:
        return schema;

      case IntegerBoundsMode.All when hasMinimum:
        schema["minimum"] = 0UL;
        schema["maximum"] = ulong.MaxValue;
        return schema;

      case IntegerBoundsMode.All:
        schema["minimum"] = long.MinValue;
        schema["maximum"] = long.MaxValue;
        return schema;

      default:
        // ExactlyRepresentable: only the zero lower bound survives, and only for the unsigned type.
        if (hasMinimum)
        {
          schema["minimum"] = 0;
        }

        return Commented(schema, comment, options.EmitComments);
    }
  }

  private static JsonObject Commented(JsonObject schema, string comment, bool emit)
  {
    if (emit)
    {
      schema["$comment"] = comment;
    }

    return schema;
  }
}
