namespace DotNetSchema.Fixtures.Schema.Invalid;

/// <summary>Keys a map by a record, which cannot be a JSON property name. Raises DNS0004.</summary>
[ExportForTesting("unsupported-key.json")]
public sealed record UnsupportedKey
{
  /// <summary>A map keyed by something System.Text.Json cannot write as a property name.</summary>
  public required IReadOnlyDictionary<Address, string> ByAddress { get; init; }
}
