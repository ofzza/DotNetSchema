using System.Text.Json.Serialization;
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

  /// <summary>
  /// ISBN-13, formatted with separators. Required but nullable: <c>null</c> for a title that predates ISBNs.
  /// Required through <c>[JsonRequired]</c> rather than the C# modifier, so the fixtures cover both markers.
  /// </summary>
  [JsonRequired]
  public string? Isbn { get; init; }

  /// <summary>Edition number.</summary>
  public required byte Edition { get; init; }

  /// <summary>Number of pages.</summary>
  public required ushort PageCount { get; init; }

  /// <summary>List price.</summary>
  public required decimal Price { get; init; }

  /// <summary>Publication date.</summary>
  public required DateOnly PublishedOn { get; init; }

  /// <summary>How the library's copy is bound. Written as a string, not a number.</summary>
  public required BookBinding Binding { get; init; }

  /// <summary>Whether the library holds a digital licence. Optional but never null: assumed not.</summary>
  public bool HasDigitalLicence { get; init; }

  /// <summary>The offering that prescribes the title. Indirectly recursive: Book -> Class -> Book.</summary>
  public Class? Class { get; init; }

  /// <summary>Authors of the title. Required but nullable: <c>null</c> when the title is unattributed.</summary>
  public required IReadOnlyList<Person>? Authors { get; init; }

  /// <summary>Table of contents.</summary>
  public required IReadOnlyList<Chapter> Chapters { get; init; }
}
