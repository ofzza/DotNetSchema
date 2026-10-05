using DotNetSchema.Fixtures.Schema.Enums;
using DotNetSchema;

namespace DotNetSchema.Fixtures.Schema;

/// <summary>
/// Root of the fixture graph. A school owns its campuses, its faculties and its library, and links out to
/// the sister schools it shares a charter with (a directly recursive edge).
/// </summary>
[DotNetSchema("school.json")]
public sealed record School
{
  /// <summary>Stable identity of the school.</summary>
  public required Guid Id { get; init; }

  /// <summary>Registered name, e.g. "Northgate Preparatory Academy".</summary>
  public required string Name { get; init; }

  /// <summary>Latin-ish motto printed on the crest.</summary>
  public required string Motto { get; init; }

  /// <summary>Date the charter was granted.</summary>
  public required DateOnly FoundedOn { get; init; }

  /// <summary>Whether the current accreditation is in good standing.</summary>
  public required bool IsAccredited { get; init; }

  /// <summary>Maximum number of enrolled students across all campuses.</summary>
  public required ushort EnrollmentCap { get; init; }

  /// <summary>Endowment, in the school's reporting currency.</summary>
  public required decimal Endowment { get; init; }

  /// <summary>Physical sites the school operates.</summary>
  public required IReadOnlyList<Campus> Campuses { get; init; }

  /// <summary>Faculties, keyed by name — a nested map of another record.</summary>
  public required IReadOnlyDictionary<DepartmentName, Department> Departments { get; init; }

  /// <summary>The school library, if the school runs one of its own.</summary>
  public Library? Library { get; init; }

  /// <summary>Schools under the same charter. Directly recursive: School -> School.</summary>
  public required IReadOnlyList<School> SisterSchools { get; init; }
}
