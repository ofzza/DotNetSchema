using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetSchema.Sample.Models;

/// <summary>
/// A value type serialised as a string by its own converter, which also declares that string's schema with
/// <c>[DotNetSchemaType]</c>, so every project exporting a model that uses it gets <c>"ORD-000042"</c>
/// described inline instead of an object definition. Keeping the converter and the declaration side by side
/// is the point: nothing checks that they agree.
/// </summary>
[DotNetSchemaType("string", Pattern = @"^ORD-\d{6}$")]
[JsonConverter(typeof(OrderNumberConverter))]
public readonly record struct OrderNumber(int Value)
{
  /// <inheritdoc />
  public override string ToString() => "ORD-" + Value.ToString("D6", CultureInfo.InvariantCulture);
}

/// <summary>Writes an <see cref="OrderNumber" /> as <c>ORD-</c> and six digits.</summary>
public sealed class OrderNumberConverter : JsonConverter<OrderNumber>
{
  /// <inheritdoc />
  public override OrderNumber Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
    new(int.Parse(reader.GetString()!.AsSpan(4), CultureInfo.InvariantCulture));

  /// <inheritdoc />
  public override void Write(Utf8JsonWriter writer, OrderNumber value, JsonSerializerOptions options) =>
    writer.WriteStringValue(value.ToString());
}

/// <summary>
/// A value type serialised as a string by its converter, with no declaration of its own: the host maps it with
/// a <c>DotNetSchemaType</c> item instead. Without that item the export would warn (DNS0019) and describe it
/// with an empty schema.
/// </summary>
[JsonConverter(typeof(CountryCodeConverter))]
public readonly record struct CountryCode(string Value);

/// <summary>Writes a <see cref="CountryCode" /> as its bare two-letter code.</summary>
public sealed class CountryCodeConverter : JsonConverter<CountryCode>
{
  /// <inheritdoc />
  public override CountryCode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
    new(reader.GetString()!);

  /// <inheritdoc />
  public override void Write(Utf8JsonWriter writer, CountryCode value, JsonSerializerOptions options) =>
    writer.WriteStringValue(value.Value);
}
