namespace DotNetSchema.Fixtures.Schema.Invalid.Right;

/// <summary>The other half of a short-name collision; see <see cref="Left.Duplicate" />.</summary>
public sealed record Duplicate
{
  /// <summary>Any member.</summary>
  public required int Count { get; init; }
}
