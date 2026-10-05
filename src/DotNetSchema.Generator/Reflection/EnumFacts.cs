using System.Reflection;

namespace DotNetSchema.Generator.Reflection;

/// <summary>Reads an enum's members, and how System.Text.Json will write them.</summary>
/// <remarks>
/// An enum has two JSON shapes, and under the default converter they disagree. As a <em>value</em> it is
/// written as a number. As a <em>dictionary key</em> it is written as the member <em>name</em>, because
/// a JSON property name is a string and the converter populates its write-name cache for every member
/// regardless of whether the numeric or the string form was asked for. So
/// <c>Assessment.Kind</c> serialises as <c>3</c> while the keys of
/// <c>IReadOnlyDictionary&lt;AssessmentKind, decimal&gt;</c> serialise as <c>"Essay"</c> — same CLR type,
/// two representations, in the same document, with no attribute anywhere to hint at it.
/// </remarks>
public static class EnumFacts
{
  private const string JsonConverterName = "System.Text.Json.Serialization.JsonConverterAttribute";
  private const string StringEnumConverterName = "System.Text.Json.Serialization.JsonStringEnumConverter";
  private const string StringEnumConverterGenericName = "System.Text.Json.Serialization.JsonStringEnumConverter`1";
  private const string StringEnumMemberName = "System.Text.Json.Serialization.JsonStringEnumMemberNameAttribute";

  /// <summary>
  /// The enum's members, ordered by ascending underlying value and then by declaration. That is the order
  /// <c>Enum.GetNames</c> produces — it sorts by value, not by declaration — so it is the order a reader
  /// comparing against the runtime will expect.
  /// </summary>
  public static IReadOnlyList<EnumMember> Members(Type type, SchemaGenerationOptions options)
  {
    var members = type
      .GetFields(BindingFlags.Public | BindingFlags.Static)
      .Where(field => field.IsLiteral)
      .Select(field => ToMember(field, options))
      .ToList();

    return [.. members.OrderBy(m => m.SortKey).ThenBy(m => m.MetadataToken)];
  }

  /// <summary>The primitive the enum's values are stored as.</summary>
  /// <remarks>
  /// Read from the <c>value__</c> instance field rather than through <c>Type.GetEnumUnderlyingType</c>,
  /// because the field is what ECMA-335 actually requires an enum to carry and so is readable identically
  /// from a runtime type and from one loaded for metadata only.
  /// </remarks>
  public static Type UnderlyingType(Type type) =>
    type.GetField("value__", BindingFlags.Public | BindingFlags.Instance)?.FieldType ?? type;

  /// <summary>Whether the underlying primitive is unsigned, and so must be written unsigned.</summary>
  public static bool IsUnsigned(Type type) =>
    UnderlyingType(type).FullName is "System.Byte" or "System.UInt16" or "System.UInt32" or "System.UInt64";

  /// <summary>
  /// Whether the enum serialises as a string here, having been given a string converter on the property
  /// or, failing that, on the enum type. Property wins, matching System.Text.Json's own precedence.
  /// </summary>
  public static bool IsStringConverted(Type enumType, string? propertyConverterFullName)
  {
    if (propertyConverterFullName is not null)
    {
      return IsStringEnumConverter(propertyConverterFullName);
    }

    return MetadataFacts.FindAttribute(enumType, JsonConverterName) is { } attribute &&
           MetadataFacts.ConverterTypeFullName(attribute) is { } converter &&
           IsStringEnumConverter(converter);
  }

  /// <summary>Whether a converter named on a property disagrees with the enum type's own default.</summary>
  public static bool DeviatesFromType(Type enumType, string? propertyConverterFullName) =>
    propertyConverterFullName is not null &&
    IsStringConverted(enumType, propertyConverterFullName) != IsStringConverted(enumType, null);

  private static bool IsStringEnumConverter(string fullName) =>
    fullName is StringEnumConverterName ||
    fullName.StartsWith(StringEnumConverterGenericName, StringComparison.Ordinal);

  private static EnumMember ToMember(FieldInfo field, SchemaGenerationOptions options)
  {
    var raw = field.GetRawConstantValue();
    var name = MetadataFacts.FindAttribute(field, StringEnumMemberName)?.ConstructorArguments is
      [{ Value: string declared }]
      ? declared
      : options.EnumNamingPolicy?.ConvertName(field.Name) ?? field.Name;

    return new EnumMember(field.Name, name, raw, ToSortKey(raw), field.MetadataToken);
  }

  private static decimal ToSortKey(object? raw) => raw switch
  {
    sbyte value => value,
    byte value => value,
    short value => value,
    ushort value => value,
    int value => value,
    uint value => value,
    long value => value,
    ulong value => value,
    _ => 0m,
  };
}

/// <summary>One member of an enum.</summary>
/// <param name="ClrName">The name as declared in C#.</param>
/// <param name="JsonName">The name it is written under, after any override or naming policy.</param>
/// <param name="Value">Its constant value, in the enum's underlying primitive type.</param>
/// <param name="SortKey">
/// <paramref name="Value" /> widened for ordering. A decimal rather than a long because it has to hold
/// the whole of both <c>long</c> and <c>ulong</c> exactly.
/// </param>
/// <param name="MetadataToken">Declaration order, used to break ties between members sharing a value.</param>
public sealed record EnumMember(
  string ClrName,
  string JsonName,
  object? Value,
  decimal SortKey,
  int MetadataToken);
