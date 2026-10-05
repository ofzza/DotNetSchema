namespace DotNetSchema.Fixtures.Schema.Invalid;

/// <summary>Keys a map by a floating-point type, whose keys will not agree across languages. Raises DNS0012.</summary>
[ExportForTesting("floating-key.json")]
public sealed record FloatingKey
{
  /// <summary>A map keyed by a double; the key text is whatever shortest-round-trip produced.</summary>
  public required IReadOnlyDictionary<double, string> ByThreshold { get; init; }
}
