namespace DotNetSchema.Fixtures.Schema.Invalid.Left;

/// <summary>One half of a short-name collision; see <see cref="Right.Duplicate" />.</summary>
public sealed record Duplicate
{
  /// <summary>Any member.</summary>
  public required string Name { get; init; }
}
