using System.ComponentModel.DataAnnotations;
using DotNetSchema.Fixtures.Schema.Decorators;
namespace DotNetSchema.Fixtures.Schema;

/// <summary>
/// A postal address. A pure value record — never truncated by the depth budget.
/// </summary>
public sealed record Address
{
  /// <summary>Street and house number.</summary>
  [SampleDecorator(typeof(List<>), (SampleShade)64, typeof(string))]
  public required string Street { get; init; }

  /// <summary>Town or city.</summary>
  [SampleDecorator(
    new[] { 1, 2, 3 },
    new[] { "x", null },
    new[] { typeof(int), typeof(Address) },
    new object?[] { 1, "two", typeof(Book), SampleShade.Green, new[] { 3, 4 }, null })]
  public required string City { get; init; }

  /// <summary>Postal / ZIP code. Optional and nullable: not every country uses one.</summary>
  [SampleDecorator(typeof(int[]), SampleShade.None, new[] { "a", "b" })]
  public string? PostalCode { get; init; }

  /// <summary>
  /// ISO 3166-1 alpha-2 country code. The format comes from <c>[DotNetSchemaFormat]</c> and the pattern from
  /// <c>[RegularExpression]</c>: the two combine when they do not overlap.
  /// </summary>
  [RegularExpression("[A-Z]{2}")]
  [DotNetSchemaFormat("iso3166-1-alpha-2")]
  [SampleDecorator(new int[0], new string[0], new Type[0], new object[0])]
  public required string CountryCode { get; init; }

  /// <summary>WGS-84 latitude.</summary>
  [Range(-90d, 90d)]
  public required double Latitude { get; init; }

  /// <summary>WGS-84 longitude.</summary>
  [Range(-180d, 180d, MinimumIsExclusive = true)]
  public required double Longitude { get; init; }
}
