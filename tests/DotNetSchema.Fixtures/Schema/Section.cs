namespace DotNetSchema.Fixtures.Schema;

/// <summary>
/// A numbered section within a <see cref="Chapter" />.
/// </summary>
public sealed record Section
{
  /// <summary>Stable identity of the section.</summary>
  public required Guid Id { get; init; }

  /// <summary>Section number as printed, e.g. "3.2".</summary>
  public required string Number { get; init; }

  /// <summary>Section heading. Required but nullable: <c>null</c> for an untitled section.</summary>
  public required string? Heading { get; init; }

  /// <summary>Opening paragraph of the section.</summary>
  public required string Body { get; init; }

  /// <summary>Word count of the section. Optional but never null: <c>0</c> until counted.</summary>
  public int WordCount { get; init; }

  /// <summary>Practice set at the end of the section.</summary>
  public required IReadOnlyList<Exercise> Exercises { get; init; }
}
