namespace DotNetSchema.Fixtures.Schema.Invalid;

/// <summary>Carries a property whose type has no JSON Schema representation. Raises DNS0002.</summary>
[ExportForTesting("unmapped.json")]
public sealed record UnmappedProperty
{
  /// <summary>An untyped value; nothing can be said about its shape.</summary>
  public required object Anything { get; init; }
}
