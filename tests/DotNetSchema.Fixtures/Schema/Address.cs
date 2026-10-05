namespace DotNetSchema.Fixtures.Schema;

/// <summary>
/// A postal address. A pure value record — never truncated by the depth budget.
/// </summary>
public sealed record Address
{
  /// <summary>Street and house number.</summary>
  public required string Street { get; init; }

  /// <summary>Town or city.</summary>
  public required string City { get; init; }

  /// <summary>Postal / ZIP code.</summary>
  public required string PostalCode { get; init; }

  /// <summary>ISO 3166-1 alpha-2 country code.</summary>
  public required string CountryCode { get; init; }

  /// <summary>WGS-84 latitude.</summary>
  public required double Latitude { get; init; }

  /// <summary>WGS-84 longitude.</summary>
  public required double Longitude { get; init; }
}
