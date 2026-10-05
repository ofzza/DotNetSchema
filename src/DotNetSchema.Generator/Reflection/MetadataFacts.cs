using System.Reflection;

namespace DotNetSchema.Generator.Reflection;

/// <summary>
/// Everything the generator needs to know about a <see cref="Type" />, asked in a way that works on a
/// type loaded for metadata only.
/// </summary>
/// <remarks>
/// This class is a firewall, and the rule it enforces is worth stating plainly: nothing in the generator
/// may compare a <see cref="Type" /> against a runtime type. No <c>typeof(X) == t</c>, no
/// <c>IsAssignableTo</c>, no <c>Nullable.GetUnderlyingType</c>, no <c>Attribute.GetCustomAttribute&lt;T&gt;</c>,
/// no <c>NullabilityInfoContext</c>. A type read through a <c>MetadataLoadContext</c> is never
/// reference-equal to a runtime type, so every one of those silently answers the wrong question rather
/// than failing — <c>Nullable.GetUnderlyingType</c> in particular is a bare
/// <c>ReferenceEquals(genericType, typeof(Nullable&lt;&gt;))</c>.
/// <para>
/// So everything here is a string comparison over <see cref="Type.FullName" />, a walk over
/// <see cref="CustomAttributeData" />, or a metadata read that carries no identity. The test suite pins
/// this by generating the same documents twice — once over runtime assemblies and once over the same
/// assemblies loaded metadata-only — and asserting the two are byte-identical.
/// </para>
/// </remarks>
public static class MetadataFacts
{
  private const string NullableOfT = "System.Nullable`1";

  private static readonly string[] DictionaryDefinitions =
  [
    "System.Collections.Generic.IReadOnlyDictionary`2",
    "System.Collections.Generic.IDictionary`2",
    "System.Collections.Generic.Dictionary`2",
    "System.Collections.Immutable.ImmutableDictionary`2",
    "System.Collections.Immutable.IImmutableDictionary`2",
    "System.Collections.Generic.SortedDictionary`2",
  ];

  private static readonly string[] EnumerableDefinitions =
  [
    "System.Collections.Generic.IReadOnlyList`1",
    "System.Collections.Generic.IReadOnlyCollection`1",
    "System.Collections.Generic.IList`1",
    "System.Collections.Generic.ICollection`1",
    "System.Collections.Generic.IEnumerable`1",
    "System.Collections.Generic.List`1",
    "System.Collections.Generic.HashSet`1",
    "System.Collections.Immutable.ImmutableArray`1",
    "System.Collections.Immutable.ImmutableList`1",
  ];

  /// <summary>
  /// A stable identity for a type, usable as a dictionary key across both a runtime and a metadata-only
  /// read of the same assembly. Namespace-qualified name plus simple assembly name; deliberately not
  /// <see cref="Type.AssemblyQualifiedName" />, whose version and token differ between the two.
  /// </summary>
  public static string TypeKey(Type type) => $"{type.FullName}, {type.Assembly.GetName().Name}";

  /// <summary>Whether the type is <c>Nullable&lt;T&gt;</c>, and if so what <c>T</c> is.</summary>
  public static bool IsNullableOfT(Type type, out Type underlying)
  {
    if (type.IsGenericType && !type.IsGenericTypeDefinition &&
        type.GetGenericTypeDefinition().FullName == NullableOfT)
    {
      underlying = type.GetGenericArguments()[0];
      return true;
    }

    underlying = type;
    return false;
  }

  /// <summary>Strips a <c>Nullable&lt;T&gt;</c> wrapper, if there is one.</summary>
  public static Type Unwrap(Type type) => IsNullableOfT(type, out var underlying) ? underlying : type;

  /// <summary>
  /// Whether the type is a dictionary, and if so what it is keyed and valued by. Checked before
  /// <see cref="TryGetEnumerable" />, mirroring how System.Text.Json resolves a type that is both.
  /// </summary>
  public static bool TryGetDictionary(Type type, out Type key, out Type value)
  {
    foreach (var candidate in SelfAndInterfaces(type))
    {
      if (!candidate.IsGenericType)
      {
        continue;
      }

      var definition = candidate.GetGenericTypeDefinition().FullName;
      if (definition is null || !DictionaryDefinitions.Contains(definition, StringComparer.Ordinal))
      {
        continue;
      }

      var arguments = candidate.GetGenericArguments();
      key = arguments[0];
      value = arguments[1];
      return true;
    }

    key = value = type;
    return false;
  }

  /// <summary>Whether the type is a sequence, and if so what it holds.</summary>
  public static bool TryGetEnumerable(Type type, out Type element)
  {
    if (type.IsArray && type.GetArrayRank() == 1)
    {
      element = type.GetElementType()!;
      return true;
    }

    foreach (var candidate in SelfAndInterfaces(type))
    {
      if (!candidate.IsGenericType)
      {
        continue;
      }

      var definition = candidate.GetGenericTypeDefinition().FullName;
      if (definition is null || !EnumerableDefinitions.Contains(definition, StringComparer.Ordinal))
      {
        continue;
      }

      element = candidate.GetGenericArguments()[0];
      return true;
    }

    element = type;
    return false;
  }

  /// <summary>Whether the type is one a model definition can be produced for.</summary>
  /// <remarks>
  /// The framework's own types are excluded wholesale, and that exclusion is doing real work. Without it
  /// <c>object</c> is a non-abstract class and <c>Int128</c>, <c>UInt128</c> and <c>Half</c> are non-enum
  /// value types, so every one of them would be walked as a model and given a definition describing its
  /// internal fields — which is not what any of them serialise as. Anything under <c>System</c> that this
  /// generator can describe is in <see cref="Model.ScalarTable" /> already; the rest has no representation
  /// and should say so.
  /// </remarks>
  public static bool IsModelCandidate(Type type) =>
    type is { IsClass: true, IsAbstract: false } or { IsValueType: true, IsEnum: false } &&
    !type.IsArray && !type.IsPointer && !type.IsPrimitive && !IsFrameworkType(type);

  private static bool IsFrameworkType(Type type) =>
    type.Namespace is { } space &&
    (space == "System" || space.StartsWith("System.", StringComparison.Ordinal));

  /// <summary>Finds one attribute by full name, or <see langword="null" /> when it is not applied.</summary>
  public static CustomAttributeData? FindAttribute(MemberInfo member, string fullName) =>
    Find(member.GetCustomAttributesData(), fullName);

  /// <summary>Finds one attribute by full name, or <see langword="null" /> when it is not applied.</summary>
  public static CustomAttributeData? FindAttribute(Assembly assembly, string fullName) =>
    Find(assembly.GetCustomAttributesData(), fullName);

  /// <summary>Finds every application of one attribute, by full name, in declaration order.</summary>
  public static IReadOnlyList<CustomAttributeData> FindAttributes(MemberInfo member, string fullName) =>
    [.. member.GetCustomAttributesData().Where(d => IsNamed(d, fullName))];

  /// <summary>
  /// Reads the single <see cref="Type" /> argument of a <c>[JsonConverter]</c>-shaped attribute as its
  /// full name. The value is a type from whichever load context the attribute was read in, so it can only
  /// ever be compared as a string.
  /// </summary>
  public static string? ConverterTypeFullName(CustomAttributeData attribute) =>
    attribute.ConstructorArguments is [{ Value: Type converter }] ? converter.FullName : null;

  private static CustomAttributeData? Find(IEnumerable<CustomAttributeData> attributes, string fullName) =>
    attributes.FirstOrDefault(d => IsNamed(d, fullName));

  private static bool IsNamed(CustomAttributeData attribute, string fullName) =>
    string.Equals(attribute.AttributeType.FullName, fullName, StringComparison.Ordinal);

  private static IEnumerable<Type> SelfAndInterfaces(Type type)
  {
    yield return type;

    foreach (var contract in type.GetInterfaces())
    {
      yield return contract;
    }
  }
}
