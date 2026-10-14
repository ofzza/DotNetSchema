using System.Text.Json.Serialization;

namespace DotNetSchema.Fixtures.Schema.Enums;

/// <summary>
/// How a copy of a title is bound. The fixtures' one string-converted enum, with one member renamed, so the
/// value written (<c>"spiral-bound"</c>) and the C# name (<c>SpiralBound</c>) part ways.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<BookBinding>))]
public enum BookBinding
{
  Hardcover,
  Paperback,

  [JsonStringEnumMemberName("spiral-bound")]
  SpiralBound
}
