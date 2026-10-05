using System.Text.Json.Serialization;

namespace DotNetSchema.Fixtures.Schema.Invalid;

/// <summary>Two properties that serialise under one name. Raises DNS0006.</summary>
[ExportForTesting("clashing-names.json")]
public sealed record ClashingNames
{
  /// <summary>Serialises as "label".</summary>
  public required string Label { get; init; }

  /// <summary>Renamed onto the same JSON name as <see cref="Label" />.</summary>
  [JsonPropertyName("label")]
  public required string OtherLabel { get; init; }
}
