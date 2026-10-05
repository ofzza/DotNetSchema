using DotNetSchema.Fixtures.Schema.Enums;

namespace DotNetSchema.Fixtures.Schema;

/// <summary>
/// The school library. Holds the catalogue as a map of another record.
/// </summary>
public sealed record Library
{
  /// <summary>Stable identity of the library.</summary>
  public required Guid Id { get; init; }

  /// <summary>Library name, e.g. "The Ashgrove Reading Rooms".</summary>
  public required string Name { get; init; }

  /// <summary>Total number of physical volumes held.</summary>
  public required uint VolumeCount { get; init; }

  /// <summary>Time the doors open on a weekday.</summary>
  public required TimeOnly OpensAt { get; init; }

  /// <summary>Time the doors close on a weekday.</summary>
  public required TimeOnly ClosesAt { get; init; }

  /// <summary>Holdings, keyed by title — a nested map of another record.</summary>
  public required IReadOnlyDictionary<BookName, Book> Catalogue { get; init; }

  /// <summary>Librarians on staff.</summary>
  public required IReadOnlyList<Person> Librarians { get; init; }

  /// <summary>Reading rooms the library occupies.</summary>
  public required IReadOnlyList<Room> ReadingRooms { get; init; }
}
