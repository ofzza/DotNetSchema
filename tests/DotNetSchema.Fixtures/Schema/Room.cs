using DotNetSchema.Fixtures.Schema.Enums;

namespace DotNetSchema.Fixtures.Schema;

/// <summary>
/// A room inside a <see cref="Building" />. Rooms host class offerings, which closes an indirect cycle
/// back into <see cref="Class" />.
/// </summary>
public sealed record Room
{
  /// <summary>Stable identity of the room.</summary>
  public required Guid Id { get; init; }

  /// <summary>Room code as printed on the door, e.g. "B-214".</summary>
  public required string Code { get; init; }

  /// <summary>What the room is fitted out for.</summary>
  public required RoomKind Kind { get; init; }

  /// <summary>Seating capacity.</summary>
  public required ushort Seats { get; init; }

  /// <summary>Floor the room is on; negative values are basement levels.</summary>
  public required sbyte Floor { get; init; }

  /// <summary>Class offerings timetabled into this room. Indirectly recursive: Room -> Class -> Room.</summary>
  public required IReadOnlyList<Class> Classes { get; init; }
}
