namespace DotNetSchema.Sample.Models;

/// <summary>Exported to the default document (reached from <see cref="Customer" />) and to <c>orders.json</c>.</summary>
[DotNetSchema("orders.json")]
public sealed record Order(Guid Id, DateTimeOffset PlacedAt, OrderStatus Status, IReadOnlyList<OrderLine> Lines);

public sealed record OrderLine(string Sku, int Quantity, decimal UnitPrice);

public enum OrderStatus
{
  Pending,
  Shipped,
  Delivered,
  Cancelled,
}
