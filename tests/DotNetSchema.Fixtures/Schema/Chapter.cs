namespace DotNetSchema.Fixtures.Schema;

/// <summary>
/// A chapter of a <see cref="Book" />. Chapters nest into sub-chapters — a directly recursive edge that
/// keeps its own depth budget.
/// </summary>
public sealed record Chapter
{
  /// <summary>Stable identity of the chapter.</summary>
  public required Guid Id { get; init; }

  /// <summary>Position in the table of contents, 1-based.</summary>
  public required int Ordinal { get; init; }

  /// <summary>Chapter title.</summary>
  public required string Title { get; init; }

  /// <summary>Page the chapter starts on.</summary>
  public required ushort StartPage { get; init; }

  /// <summary>Estimated reading time. Optional and nullable: not every publisher states one.</summary>
  public TimeSpan? EstimatedReadingTime { get; init; }

  /// <summary>Body of the chapter.</summary>
  public required IReadOnlyList<Section> Sections { get; init; }

  /// <summary>
  /// Nested chapters. Directly recursive: Chapter -> Chapter. Optional but never null: empty for a flat chapter.
  /// </summary>
  public IReadOnlyList<Chapter> SubChapters { get; init; } = [];
}
