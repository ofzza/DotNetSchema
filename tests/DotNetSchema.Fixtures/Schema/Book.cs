using DotNetSchema.Fixtures.Schema.Enums;

namespace DotNetSchema.Fixtures.Schema;

/// <summary>
/// A title on the reading list. Points back at the class that prescribes it, which is the shortest
/// indirect cycle in the schema: Class -> Book -> Class.
/// </summary>
public sealed record Book
{
  /// <summary>Stable identity of the title.</summary>
  public required Guid Id { get; init; }

  /// <summary>Which title this is.</summary>
  public required BookName Name { get; init; }

  /// <summary>ISBN-13, formatted with separators.</summary>
  public required string Isbn { get; init; }

  /// <summary>Edition number.</summary>
  public required byte Edition { get; init; }

  /// <summary>Number of pages.</summary>
  public required ushort PageCount { get; init; }

  /// <summary>List price.</summary>
  public required decimal Price { get; init; }

  /// <summary>Publication date.</summary>
  public required DateOnly PublishedOn { get; init; }

  /// <summary>Whether the library holds a digital licence.</summary>
  public required bool HasDigitalLicence { get; init; }

  /// <summary>The offering that prescribes the title. Indirectly recursive: Book -> Class -> Book.</summary>
  public Class? Class { get; init; }

  /// <summary>Authors of the title.</summary>
  public required IReadOnlyList<Person> Authors { get; init; }

  /// <summary>Table of contents.</summary>
  public required IReadOnlyList<Chapter> Chapters { get; init; }
}
