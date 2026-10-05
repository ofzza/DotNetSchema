namespace DotNetSchema.Fixtures.Schema;

/// <summary>
/// A single practice question. The deepest leaf on the library branch of the graph.
/// </summary>
public sealed record Exercise
{
  /// <summary>Stable identity of the exercise.</summary>
  public required Guid Id { get; init; }

  /// <summary>Label as printed, e.g. "3.2.7".</summary>
  public required string Label { get; init; }

  /// <summary>The question put to the student.</summary>
  public required string Prompt { get; init; }

  /// <summary>Difficulty on a 1.0-5.0 scale.</summary>
  public required float Difficulty { get; init; }

  /// <summary>Whether the worked solution is printed in the back of the book.</summary>
  public required bool HasWorkedSolution { get; init; }

  /// <summary>Marks awarded for a correct answer.</summary>
  public required byte Marks { get; init; }
}
