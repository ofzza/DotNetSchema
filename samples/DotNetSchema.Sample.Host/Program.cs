using DotNetSchema.Sample.Models;

var customer = new Customer(Guid.NewGuid(), "Ada", null, new Address("1 Main St", "Zagreb", "10000", "HR"), []);
Console.WriteLine(customer);
