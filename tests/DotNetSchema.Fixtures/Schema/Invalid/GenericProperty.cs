namespace DotNetSchema.Fixtures.Schema.Invalid;

/// <summary>Reaches a generic model through a property. Raises DNS0003.</summary>
[ExportForTesting("generic.json")]
public sealed record GenericProperty
{
  /// <summary>A generic model, which cannot be given a definition.</summary>
  public required GenericPair<int> Pair { get; init; }
}
