using System.ComponentModel.DataAnnotations;

namespace DotNetSchema.Sample.Models;

/// <summary>Exported to the host's default document, <c>DotNetSchema.Sample.Host.schema.json</c>.</summary>
/// <remarks>
/// On a positional record an attribute written without a target lands on the constructor parameter; the
/// exporter reads it from there, so <c>[StringLength]</c> still becomes <c>maxLength</c> — or would, had the
/// host not denied it.
/// </remarks>
[DotNetSchema]
public sealed record Customer(
  Guid Id,
  [StringLength(100)] string Name,
  [EmailAddress][PersonalData] string? Email,
  Address BillingAddress,
  IReadOnlyList<Order> Orders);

/// <summary>Not marked: carried into the document because <see cref="Customer" /> reaches it.</summary>
/// <remarks>
/// A postal code is a plain <c>string</c> to C#; <c>[DotNetSchemaFormat]</c> tells a schema reader what kind
/// of string, for this one property, without a type of its own.
/// </remarks>
public sealed record Address(
  string Street,
  string City,
  [DotNetSchemaFormat("postal-code")] string PostalCode,
  CountryCode Country);
