namespace DotNetSchema.Fixtures.Schema;

/// <summary>
/// A physical site of a <see cref="School" />.
/// </summary>
public sealed record Campus
{
  /// <summary>Stable identity of the campus.</summary>
  public required Guid Id { get; init; }

  /// <summary>Campus name, e.g. "Riverside Campus".</summary>
  public required string Name { get; init; }

  /// <summary>Where the campus sits. A nested value record, always present.</summary>
  public required Address Address { get; init; }

  /// <summary>
  /// Date the site opened to students. Required but nullable: always stated, <c>null</c> when the records are lost.
  /// </summary>
  public required DateOnly? OpenedOn { get; init; }

  /// <summary>Site area in hectares. Optional but never null: <c>0</c> until surveyed.</summary>
  public double AreaHectares { get; init; }

  /// <summary>Buildings on the site.</summary>
  public required IReadOnlyList<Building> Buildings { get; init; }
}
