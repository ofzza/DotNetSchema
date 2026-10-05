using System.Reflection;

namespace DotNetSchema.Generator.Reflection;

/// <summary>One property of a model, as System.Text.Json would see it.</summary>
/// <param name="Property">The property itself.</param>
/// <param name="JsonName">The name it serialises under, after the naming policy and any override.</param>
/// <param name="Order">Its <c>[JsonPropertyOrder]</c>, or zero.</param>
/// <param name="MetadataToken">Declaration order, and the only ordering key that survives a metadata-only read.</param>
/// <param name="IsRequired">Whether the property carries the C# <c>required</c> modifier.</param>
/// <param name="Nullability">Nullability of its type, and of everything nested in it.</param>
/// <param name="ConverterTypeFullName">Full name of a converter named on the property, if any.</param>
public sealed record JsonMember(
  PropertyInfo Property,
  string JsonName,
  int Order,
  int MetadataToken,
  bool IsRequired,
  NullabilityNode Nullability,
  string? ConverterTypeFullName)
{
  /// <summary>The declared type of the property.</summary>
  public Type Type => Property.PropertyType;
}
