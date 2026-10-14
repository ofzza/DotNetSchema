using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetSchema.Fixtures.Schema;

/// <summary>
/// A strongly-typed identifier, generic over what it identifies. A generic model has no definition name and
/// would be refused (DNS0003); the declaration on the generic definition covers every construction, and
/// each is written as the bare GUID whatever <typeparamref name="T" /> is.
/// </summary>
/// <typeparam name="T">The kind of thing identified; it does not reach the wire.</typeparam>
[DotNetSchemaType("string", Format = "uuid")]
[JsonConverter(typeof(CatalogueIdConverterFactory))]
public readonly record struct CatalogueId<T>(Guid Value);

/// <summary>
/// Creates the converter for each construction of <see cref="CatalogueId{T}" />: one factory per generic
/// definition, so the wire shape cannot drift between constructions.
/// </summary>
public sealed class CatalogueIdConverterFactory : JsonConverterFactory
{
  /// <inheritdoc />
  public override bool CanConvert(Type typeToConvert) =>
    typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(CatalogueId<>);

  /// <inheritdoc />
  public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
    (JsonConverter)Activator.CreateInstance(
      typeof(Converter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]))!;

  private sealed class Converter<T> : JsonConverter<CatalogueId<T>>
  {
    public override CatalogueId<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
      new(reader.GetGuid());

    public override void Write(Utf8JsonWriter writer, CatalogueId<T> value, JsonSerializerOptions options) =>
      writer.WriteStringValue(value.Value);
  }
}
