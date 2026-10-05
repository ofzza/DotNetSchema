using DotNetSchema.Fixtures.Schema.Enums;

namespace DotNetSchema.Fixtures.Schema;

/// <summary>
/// One offering of a course in one term. The busiest node in the graph: it reaches people, rooms,
/// materials, its own prerequisites and its assessments.
/// </summary>
public sealed record Class
{
  /// <summary>Stable identity of the offering.</summary>
  public required Guid Id { get; init; }

  /// <summary>Which course this is an offering of.</summary>
  public required ClassName Name { get; init; }

  /// <summary>Catalogue code, e.g. "MAT-201-A".</summary>
  public required string Code { get; init; }

  /// <summary>Term the offering runs in.</summary>
  public required Term Term { get; init; }

  /// <summary>Academic year the term belongs to.</summary>
  public required ushort AcademicYear { get; init; }

  /// <summary>Credit value of the course.</summary>
  public required double Credits { get; init; }

  /// <summary>Enrolment ceiling for the offering.</summary>
  public required byte Seats { get; init; }

  /// <summary>Whether enrolment is still open.</summary>
  public required bool IsEnrollmentOpen { get; init; }

  /// <summary>Course description as printed in the catalogue.</summary>
  public required string Synopsis { get; init; }

  /// <summary>Member of staff leading the offering. Indirectly recursive: Class -> Person -> Class.</summary>
  public Person? Professor { get; init; }

  /// <summary>Enrolled students. Indirectly recursive across a collection.</summary>
  public required IReadOnlyList<Person> Students { get; init; }

  /// <summary>Prescribed materials. Indirectly recursive: Class -> Book -> Class.</summary>
  public required IReadOnlyList<Book> Materials { get; init; }

  /// <summary>Offerings that must be passed first. Directly recursive: Class -> Class.</summary>
  public required IReadOnlyList<Class> Prerequisites { get; init; }

  /// <summary>Weekly timetable of the offering.</summary>
  public required IReadOnlyList<ClassSession> Schedule { get; init; }

  /// <summary>Graded work set for the offering.</summary>
  public required IReadOnlyList<Assessment> Assessments { get; init; }

  /// <summary>Default room. Indirectly recursive: Class -> Room -> Class.</summary>
  public Room? Room { get; init; }
}
