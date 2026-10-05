namespace DotNetSchema.Fixtures.Generation;

/// <summary>
/// Knobs controlling how far and how wide a fixture graph is expanded.
/// </summary>
/// <remarks>
/// The schema is cyclic; the fixtures it produces are not. Every generated graph is a finite tree,
/// obtained by giving each traversal a <see cref="MaxDepth" /> budget and truncating when it runs out —
/// optional references become <c>null</c> and collections become empty. That keeps a fixture safe to hand
/// to a naive serialiser, a recursive comparer or a plain <c>ToString()</c>, none of which survive a real
/// object cycle.
/// </remarks>
public sealed record FixtureOptions
{
  /// <summary>The options used when a caller does not supply any.</summary>
  public static FixtureOptions Default { get; } = new();

  /// <summary>
  /// How many entity levels a traversal may descend before it is truncated. Value records
  /// (<c>Address</c>, <c>AssessmentRecord</c> and the scalar bundles) do not consume budget and are
  /// always filled in.
  /// </summary>
  public int MaxDepth { get; init; } = 4;

  /// <summary>Smallest number of elements generated into a collection.</summary>
  public int MinCollectionSize { get; init; } = 1;

  /// <summary>Largest number of elements generated into a collection.</summary>
  public int MaxCollectionSize { get; init; } = 3;

  /// <summary>Number of entries generated into a map.</summary>
  public int MapSize { get; init; } = 3;

  /// <summary>Length of every series inside a <c>ScalarSeries</c>.</summary>
  public int SeriesLength { get; init; } = 4;

  /// <summary>
  /// Chance that a nullable reference is filled in rather than left <c>null</c>. Set it to <c>1.0</c>
  /// for a fully populated graph, or to <c>0.0</c> to exercise the null paths.
  /// </summary>
  public double OptionalFillRate { get; init; } = 0.75d;

  /// <summary>
  /// Depth budget granted to a back-reference — <c>Book.Class</c>, <c>Assessment.Class</c> and the like.
  /// At the default of <c>0</c> a back-reference carries the identity and scalars of the node it points
  /// back at, but none of its children, which is what keeps the graph finite while still letting
  /// <c>book.Class.Id == owningClass.Id</c> hold.
  /// </summary>
  public int BackReferenceDepth { get; init; } = 0;
}
