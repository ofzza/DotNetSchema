using DotNetSchema.Fixtures.Schema.Enums;

namespace DotNetSchema.Fixtures.Schema;

/// <summary>
/// Anybody attached to the school — student, professor, assistant or staff. Carries both a directly
/// recursive edge (mentor / advisees) and an indirectly recursive one (through <see cref="Class" />).
/// </summary>
public sealed record Person
{
  /// <summary>Stable identity of the person.</summary>
  public required Guid Id { get; init; }

  /// <summary>Given name.</summary>
  public required string GivenName { get; init; }

  /// <summary>Family name.</summary>
  public required string FamilyName { get; init; }

  /// <summary>Capacity in which the person is attached to the school.</summary>
  public required PersonRole Role { get; init; }

  /// <summary>Date of birth.</summary>
  public required DateOnly BornOn { get; init; }

  /// <summary>School e-mail address.</summary>
  public required string Email { get; init; }

  /// <summary>Whether the person's record is currently active.</summary>
  public required bool IsActive { get; init; }

  /// <summary>Grade point average across all completed classes.</summary>
  public required double GradePointAverage { get; init; }

  /// <summary>Class offerings attended or taught. Indirectly recursive: Person -> Class -> Person.</summary>
  public required IReadOnlyList<Class> Classes { get; init; }

  /// <summary>Marks per class, on a 1-100 scale. A map keyed by an enum.</summary>
  public required IReadOnlyDictionary<ClassName, int> Grades { get; init; }

  /// <summary>Free-form contact channels, e.g. "mobile" -> "+385 91 ...". A string-to-string map.</summary>
  public required IReadOnlyDictionary<string, string> ContactMethods { get; init; }

  /// <summary>Academic mentor. Directly recursive: Person -> Person.</summary>
  public Person? Mentor { get; init; }

  /// <summary>People mentored by this person. Directly recursive across a collection.</summary>
  public required IReadOnlyList<Person> Advisees { get; init; }

  /// <summary>
  /// Assigned office or home room, if any. Indirectly recursive: Person -> Room -> Class -> Person.
  /// </summary>
  public Room? Office { get; init; }
}
