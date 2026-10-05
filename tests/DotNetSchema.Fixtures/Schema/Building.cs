namespace DotNetSchema.Fixtures.Schema;

/// <summary>
/// A building on a <see cref="Campus" />.
/// </summary>
public sealed record Building
{
  /// <summary>Stable identity of the building.</summary>
  public required Guid Id { get; init; }

  /// <summary>Building name, e.g. "Faraday Hall".</summary>
  public required string Name { get; init; }

  /// <summary>Number of floors above ground.</summary>
  public required short FloorCount { get; init; }

  /// <summary>Whether the building is step-free throughout.</summary>
  public required bool IsAccessible { get; init; }

  /// <summary>Year the building was completed.</summary>
  public required ushort BuiltInYear { get; init; }

  /// <summary>Rooms inside the building.</summary>
  public required IReadOnlyList<Room> Rooms { get; init; }
}
