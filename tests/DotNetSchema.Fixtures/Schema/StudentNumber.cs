using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetSchema.Fixtures.Schema;

/// <summary>
/// A student's matriculation number, serialised by its own converter as <c>S</c> and seven digits, and
/// declaring that shape with <c>[DotNetSchemaType]</c> so it is described inline rather than as an object.
/// The converter and the declaration sit side by side because nothing else keeps them in agreement — except
/// the validating tests, which serialise a real graph through the converter and check it against the schema.
/// </summary>
[DotNetSchemaType("string", Pattern = @"^S\d{7}$")]
[JsonConverter(typeof(StudentNumberConverter))]
public readonly record struct StudentNumber(int Value)
{
  /// <inheritdoc />
  public override string ToString() => "S" + Value.ToString("D7", CultureInfo.InvariantCulture);
}

/// <summary>Writes a <see cref="StudentNumber" /> as <c>S</c> and seven digits.</summary>
public sealed class StudentNumberConverter : JsonConverter<StudentNumber>
{
  /// <inheritdoc />
  public override StudentNumber Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
    new(int.Parse(reader.GetString()!.AsSpan(1), CultureInfo.InvariantCulture));

  /// <inheritdoc />
  public override void Write(Utf8JsonWriter writer, StudentNumber value, JsonSerializerOptions options) =>
    writer.WriteStringValue(value.ToString());
}
