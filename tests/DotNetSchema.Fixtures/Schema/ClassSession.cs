namespace DotNetSchema.Fixtures.Schema;

/// <summary>
/// One recurring slot in a <see cref="Class" />'s weekly timetable.
/// </summary>
public sealed record ClassSession
{
  /// <summary>Stable identity of the slot.</summary>
  public required Guid Id { get; init; }

  /// <summary>Weekday the slot repeats on.</summary>
  public required DayOfWeek Day { get; init; }

  /// <summary>Time of day the slot starts.</summary>
  public required TimeOnly StartsAt { get; init; }

  /// <summary>How long the slot runs for.</summary>
  public required TimeSpan Duration { get; init; }

  /// <summary>Whether the slot is delivered online.</summary>
  public required bool IsRemote { get; init; }

  /// <summary>Room the slot is timetabled into, when it is not remote.</summary>
  public Room? Room { get; init; }
}
