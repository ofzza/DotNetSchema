namespace DotNetSchema.Fixtures.Schema.Invalid;

/// <summary>Pulls two same-named types into one document, forcing both to be qualified. Raises DNS0005.</summary>
[ExportForTesting("colliding.json")]
public sealed record CollidingRoot
{
  /// <summary>One of the two <c>Duplicate</c> types.</summary>
  public required Left.Duplicate Left { get; init; }

  /// <summary>The other.</summary>
  public required Right.Duplicate Right { get; init; }
}
