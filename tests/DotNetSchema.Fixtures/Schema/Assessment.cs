using DotNetSchema.Fixtures.Schema.Enums;
using DotNetSchema;

namespace DotNetSchema.Fixtures.Schema;

/// <summary>
/// A piece of graded work set for a <see cref="Class" />.
/// </summary>
[DotNetSchema("assessment.json")]
public sealed record Assessment
{
  /// <summary>Stable identity of the assessment.</summary>
  public required Guid Id { get; init; }

  /// <summary>Form the assessment takes.</summary>
  public required AssessmentKind Kind { get; init; }

  /// <summary>Title as announced to students.</summary>
  public required string Title { get; init; }

  /// <summary>Share of the final mark, 0.0-1.0.</summary>
  public required decimal Weight { get; init; }

  /// <summary>Marks available.</summary>
  public required ushort MaximumMarks { get; init; }

  /// <summary>Moment the submission window closes.</summary>
  public required DateTimeOffset DueAt { get; init; }

  /// <summary>Whether students may consult their materials.</summary>
  public required bool IsOpenBook { get; init; }

  /// <summary>The offering the work was set for. Indirectly recursive: Assessment -> Class -> Assessment.</summary>
  public Class? Class { get; init; }

  /// <summary>The marking record. A value record — always present, never truncated.</summary>
  public required AssessmentRecord Record { get; init; }
}
