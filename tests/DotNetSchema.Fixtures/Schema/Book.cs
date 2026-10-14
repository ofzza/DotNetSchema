using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using DotNetSchema.Fixtures.Schema.Decorators;
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

  /// <summary>
  /// The catalogue's own identifier for the title. A construction of a generic type, described inline by the
  /// <c>[DotNetSchemaType]</c> on its definition.
  /// </summary>
  public required CatalogueId<Book> CatalogueId { get; init; }

  /// <summary>Which title this is.</summary>
  [SampleDecorator(typeof(SampleDecoratorAttribute.Nested), SampleShade.Red | SampleShade.Blue, "boxed")]
  public required BookName Name { get; init; }

  /// <summary>
  /// ISBN-13, formatted with separators. Required but nullable: <c>null</c> for a title that predates ISBNs.
  /// Required through <c>[JsonRequired]</c> rather than the C# modifier, so the fixtures cover both markers.
  /// Its <c>[DotNetSchemaFormat]</c> gives a plain string a format and a pattern, beside a null.
  /// </summary>
  [JsonRequired]
  [DotNetSchemaFormat("isbn", Pattern = @"^978-\d-\d{3}-\d{5}-\d$")]
  [Display(Name = "ISBN-13", Description = "ISBN-13, formatted with separators.")]
  [SampleDecorator(typeof(IReadOnlyDictionary<string, Book>), SampleShade.Green, 42)]
  public string? Isbn { get; init; }

  /// <summary>Edition number.</summary>
  [Range(1, 99)]
  public required byte Edition { get; init; }

  /// <summary>Number of pages.</summary>
  [Range(typeof(ushort), "1", "5000")]
  public required ushort PageCount { get; init; }

  /// <summary>List price. A <c>[DotNetSchemaFormat]</c> on a number: a format, which needs no string.</summary>
  [DotNetSchemaFormat("decimal")]
  public required decimal Price { get; init; }

  /// <summary>Publication date.</summary>
  public required DateOnly PublishedOn { get; init; }

  /// <summary>How the library's copy is bound. Written as a string, not a number.</summary>
  public required BookBinding Binding { get; init; }

  /// <summary>Whether the library holds a digital licence. Optional but never null: assumed not.</summary>
  [DefaultValue(false)]
  public bool HasDigitalLicence { get; init; }

  /// <summary>The offering that prescribes the title. Indirectly recursive: Book -> Class -> Book.</summary>
  public Class? Class { get; init; }

  /// <summary>Authors of the title. Required but nullable: <c>null</c> when the title is unattributed.</summary>
  public required IReadOnlyList<Person>? Authors { get; init; }

  /// <summary>Table of contents.</summary>
  [MaxLength(64)]
  public required IReadOnlyList<Chapter> Chapters { get; init; }
}
