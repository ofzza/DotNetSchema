using System.ComponentModel.DataAnnotations;
using DotNetSchema.Fixtures.Schema.Decorators;
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
  [StringLength(32, MinimumLength = 2)]
  [SampleDecorator(
    false, sbyte.MinValue, byte.MinValue, short.MinValue, ushort.MinValue, int.MinValue, uint.MinValue,
    long.MinValue, ulong.MinValue, float.NegativeInfinity, double.NaN, 'a', null)]
  public required string GivenName { get; init; }

  /// <summary>Family name.</summary>
  [MaxLength(40)]
  [SampleDecorator(
    true, sbyte.MaxValue, byte.MaxValue, short.MaxValue, ushort.MaxValue, int.MaxValue, uint.MaxValue,
    long.MaxValue, ulong.MaxValue, float.MaxValue, double.PositiveInfinity, '"', "quote \" and \u2713")]
  public required string FamilyName { get; init; }

  /// <summary>Capacity in which the person is attached to the school.</summary>
  public required PersonRole Role { get; init; }

  /// <summary>
  /// Matriculation number. Optional and nullable: only students have one. Described inline by the
  /// <c>[DotNetSchemaType]</c> on <see cref="Schema.StudentNumber" />, and widened for null like any scalar.
  /// </summary>
  public StudentNumber? StudentNumber { get; init; }

  /// <summary>Date of birth.</summary>
  public required DateOnly BornOn { get; init; }

  /// <summary>School e-mail address. Optional and nullable: not everyone has been issued one.</summary>
  [EmailAddress]
  public string? Email { get; init; }

  /// <summary>Whether the person's record is currently active.</summary>
  public required bool IsActive { get; init; }

  /// <summary>
  /// Grade point average across all completed classes. Required but nullable: <c>null</c> before the first
  /// class is completed.
  /// </summary>
  [Range(1d, 5d)]
  public required double? GradePointAverage { get; init; }

  /// <summary>Class offerings attended or taught. Indirectly recursive: Person -> Class -> Person.</summary>
  [Length(0, 64)]
  public required IReadOnlyList<Class> Classes { get; init; }

  /// <summary>
  /// Marks per class, on a 1-100 scale. A map keyed by an enum. Optional but never null: empty before any
  /// class has been marked.
  /// </summary>
  public IReadOnlyDictionary<ClassName, int> Grades { get; init; } = new Dictionary<ClassName, int>();

  /// <summary>
  /// Free-form contact channels, e.g. "mobile" -> "+385 91 ...". A string-to-string map. Optional and
  /// nullable: not everyone has left contact details.
  /// </summary>
  public IReadOnlyDictionary<string, string>? ContactMethods { get; init; }

  /// <summary>Academic mentor. Directly recursive: Person -> Person.</summary>
  public Person? Mentor { get; init; }

  /// <summary>
  /// People mentored by this person. Directly recursive across a collection. Optional but never null: empty
  /// for anyone who mentors nobody.
  /// </summary>
  public IReadOnlyList<Person> Advisees { get; init; } = [];

  /// <summary>
  /// Assigned office or home room, if any. Indirectly recursive: Person -> Room -> Class -> Person.
  /// </summary>
  public Room? Office { get; init; }
}
