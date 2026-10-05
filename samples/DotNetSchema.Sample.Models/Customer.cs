namespace DotNetSchema.Sample.Models;

/// <summary>Exported to the host's default document, <c>DotNetSchema.Sample.Host.schema.json</c>.</summary>
[DotNetSchema]
public sealed record Customer(Guid Id, string Name, string? Email, Address BillingAddress, IReadOnlyList<Order> Orders);

/// <summary>Not marked: carried into the document because <see cref="Customer" /> reaches it.</summary>
public sealed record Address(string Street, string City, string PostalCode, string Country);
