using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DotNetSchema.Sample.Models;

/// <summary>Exported to the default document (reached from <see cref="Customer" />) and to <c>orders.json</c>.</summary>
[DotNetSchema("orders.json")]
[Description("An order placed by a customer.")]
public sealed record Order(Guid Id, DateTimeOffset PlacedAt, OrderStatus Status, IReadOnlyList<OrderLine> Lines);

public sealed record OrderLine(
  [RegularExpression("[A-Z0-9-]+")] string Sku,
  [Range(1, 1000)] int Quantity,
  decimal UnitPrice);

public enum OrderStatus
{
  Pending,
  Shipped,
  Delivered,
  Cancelled,
}
