using DotNetSchema.Fixtures.Schema.Enums;

namespace DotNetSchema.Fixtures.Schema;

/// <summary>
/// A faculty of a <see cref="School" />. Departments nest into sub-departments, which is a directly
/// recursive edge.
/// </summary>
public sealed record Department
{
  /// <summary>Stable identity of the department.</summary>
  public required Guid Id { get; init; }

  /// <summary>Which faculty this is.</summary>
  public required DepartmentName Name { get; init; }

  /// <summary>Internal cost-centre code.</summary>
  public required string CostCentre { get; init; }

  /// <summary>Annual operating budget.</summary>
  public required decimal AnnualBudget { get; init; }

  /// <summary>Date of the department's founding.</summary>
  public required DateOnly EstablishedOn { get; init; }

  /// <summary>Head of department, when the post is filled.</summary>
  public Person? Head { get; init; }

  /// <summary>Class offerings the department runs.</summary>
  public required IReadOnlyList<Class> Courses { get; init; }

  /// <summary>Sub-faculties. Directly recursive: Department -> Department.</summary>
  public required IReadOnlyList<Department> SubDepartments { get; init; }
}
