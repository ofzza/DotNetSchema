using System.Reflection;

namespace DotNetSchema.Generator.Reflection;

/// <summary>
/// Reads a model's properties the way System.Text.Json would, so the schema describes what the type
/// actually serialises as rather than how it happens to be declared.
/// </summary>
public static class JsonMemberReader
{
  private const string JsonIgnoreName = "System.Text.Json.Serialization.JsonIgnoreAttribute";
  private const string JsonPropertyNameName = "System.Text.Json.Serialization.JsonPropertyNameAttribute";
  private const string JsonPropertyOrderName = "System.Text.Json.Serialization.JsonPropertyOrderAttribute";
  private const string JsonConverterName = "System.Text.Json.Serialization.JsonConverterAttribute";
  private const string RequiredMemberName = "System.Runtime.CompilerServices.RequiredMemberAttribute";

  /// <summary>
  /// <c>JsonIgnoreCondition.Always</c>, the value <c>[JsonIgnore]</c> carries when written without one.
  /// The other conditions are about whether a value is written, not about whether the property exists, so
  /// they leave the property in the schema.
  /// </summary>
  private const int IgnoreAlways = 1;

  /// <summary>
  /// The serialisable properties of a type, in the order System.Text.Json writes them: by
  /// <c>[JsonPropertyOrder]</c>, then by declaration.
  /// </summary>
  public static IReadOnlyList<JsonMember> Read(Type type, SchemaGenerationOptions options)
  {
    var members = type
      .GetProperties(BindingFlags.Public | BindingFlags.Instance)
      .Where(IsSerialisable)
      .Select(property => ToMember(property, options))
      .ToList();

    // A stable sort, so that properties sharing an order keep declaration order between them. Reflection
    // does not promise any particular order from GetProperties, which is why the tie-break is the
    // metadata token rather than the position in that array.
    return [.. members.OrderBy(m => m.Order).ThenBy(m => m.MetadataToken)];
  }

  private static bool IsSerialisable(PropertyInfo property)
  {
    if (property.GetIndexParameters().Length > 0)
    {
      return false;
    }

    if (property.GetMethod is not { IsPublic: true, IsStatic: false })
    {
      return false;
    }

    return MetadataFacts.FindAttribute(property, JsonIgnoreName) is not { } ignore || !IsIgnoredAlways(ignore);
  }

  private static bool IsIgnoredAlways(CustomAttributeData ignore)
  {
    var condition = ignore.NamedArguments.FirstOrDefault(a => a.MemberName == "Condition");
    return condition.TypedValue.Value is not int value || value == IgnoreAlways;
  }

  private static JsonMember ToMember(PropertyInfo property, SchemaGenerationOptions options)
  {
    var declared = MetadataFacts.FindAttribute(property, JsonPropertyNameName)?.ConstructorArguments is
      [{ Value: string name }]
      ? name
      : options.PropertyNamingPolicy.ConvertName(property.Name);

    var order = MetadataFacts.FindAttribute(property, JsonPropertyOrderName)?.ConstructorArguments is
      [{ Value: int value }]
      ? value
      : 0;

    var converter = MetadataFacts.FindAttribute(property, JsonConverterName) is { } attribute
      ? MetadataFacts.ConverterTypeFullName(attribute)
      : null;

    return new JsonMember(
      property,
      declared,
      order,
      property.MetadataToken,
      MetadataFacts.FindAttribute(property, RequiredMemberName) is not null,
      NullableFlagsReader.Read(property),
      converter);
  }
}
