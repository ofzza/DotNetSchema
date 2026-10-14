using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetSchema.Fixtures.Schema.Invalid;

/// <summary>
/// Reaches a type written by its own converter that declares no schema for what it writes. Raises DNS0019,
/// and is described by an empty schema rather than by members the converter ignores.
/// </summary>
[ExportForTesting("converted.json")]
public sealed record ConvertedWithoutMapping
{
  /// <summary>A position whose shape only the converter knows.</summary>
  public required Converted Value { get; init; }
}

/// <summary>Written as a bare string by <see cref="ConvertedConverter" />, though it looks like an object.</summary>
[JsonConverter(typeof(ConvertedConverter))]
public sealed record Converted(string Text, int Ignored);

/// <summary>Writes a <see cref="Converted" /> as its text alone.</summary>
public sealed class ConvertedConverter : JsonConverter<Converted>
{
  /// <inheritdoc />
  public override Converted Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
    new(reader.GetString()!, 0);

  /// <inheritdoc />
  public override void Write(Utf8JsonWriter writer, Converted value, JsonSerializerOptions options) =>
    writer.WriteStringValue(value.Text);
}
