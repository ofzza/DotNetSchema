using DotNetSchema.Generator.Diagnostics;
using DotNetSchema.Generator.Reflection;

namespace DotNetSchema.Generator.Model;

/// <summary>Decides what each C# type becomes in a schema, and complains about the ones that become nothing.</summary>
/// <param name="options">The generation options in force.</param>
/// <param name="diagnostics">Where to report a type that cannot be mapped.</param>
public sealed class TypeClassifier(SchemaGenerationOptions options, IList<SchemaDiagnostic> diagnostics)
{
  /// <summary>Classifies a position, recursing into whatever is nested there.</summary>
  /// <param name="type">The declared type at this position.</param>
  /// <param name="nullability">Its nullability, from <see cref="NullableFlagsReader" />.</param>
  /// <param name="converterFullName">A converter named on the owning property, if any.</param>
  /// <param name="owner">Full name of the type the position belongs to, for diagnostics.</param>
  /// <param name="member">Name of the member the position belongs to, for diagnostics.</param>
  public SchemaTypeRef Classify(
    Type type,
    NullabilityNode nullability,
    string? converterFullName,
    string owner,
    string member)
  {
    var unwrapped = MetadataFacts.Unwrap(type);
    var isNullable = nullability.IsNullable;

    // Order matters and mirrors System.Text.Json's own resolution: a scalar first, because string is an
    // IEnumerable<char> and would otherwise classify as an array; then a dictionary, because a dictionary
    // is also an enumerable of key-value pairs.
    if (ScalarTable.IsScalar(unwrapped))
    {
      return new SchemaTypeRef(SchemaTypeKind.Scalar, unwrapped, isNullable);
    }

    if (unwrapped.IsEnum)
    {
      if (EnumFacts.DeviatesFromType(unwrapped, converterFullName))
      {
        Warn(
          SchemaDiagnosticCodes.EnumConverterDeviates,
          $"Property '{owner}.{member}' names an enum converter that disagrees with the one on " +
          $"'{unwrapped.FullName}'. Its schema is inlined at the property rather than shared.",
          owner,
          member);
      }

      return new SchemaTypeRef(
        SchemaTypeKind.Enum,
        unwrapped,
        isNullable,
        AsString: EnumFacts.IsStringConverted(unwrapped, converterFullName));
    }

    if (IsBinary(unwrapped))
    {
      return new SchemaTypeRef(SchemaTypeKind.Binary, unwrapped, isNullable);
    }

    if (MetadataFacts.TryGetDictionary(unwrapped, out var keyType, out var valueType))
    {
      return ClassifyMap(unwrapped, keyType, valueType, nullability, isNullable, owner, member);
    }

    if (MetadataFacts.TryGetEnumerable(unwrapped, out var elementType))
    {
      var element = Classify(elementType, nullability.Argument(0), converterFullName, owner, member);
      return new SchemaTypeRef(SchemaTypeKind.Array, unwrapped, isNullable, Item: element);
    }

    if (unwrapped.IsGenericType)
    {
      Error(
        SchemaDiagnosticCodes.GenericModel,
        $"Property '{owner}.{member}' has generic type '{unwrapped.FullName}'. Generic models have no " +
        "stable definition name and are not supported.",
        owner,
        member);
      return new SchemaTypeRef(SchemaTypeKind.Unmapped, unwrapped, isNullable);
    }

    if (MetadataFacts.IsModelCandidate(unwrapped))
    {
      return new SchemaTypeRef(SchemaTypeKind.Model, unwrapped, isNullable);
    }

    Report(
      options.UnmappedTypes == UnmappedTypePolicy.Error,
      SchemaDiagnosticCodes.UnmappedType,
      $"Property '{owner}.{member}' has type '{unwrapped.FullName}', which has no JSON Schema mapping.",
      owner,
      member);
    return new SchemaTypeRef(SchemaTypeKind.Unmapped, unwrapped, isNullable);
  }

  private SchemaTypeRef ClassifyMap(
    Type map,
    Type keyType,
    Type valueType,
    NullabilityNode nullability,
    bool isNullable,
    string owner,
    string member)
  {
    var key = ClassifyKey(MetadataFacts.Unwrap(keyType), owner, member);
    var value = Classify(valueType, nullability.Argument(1), null, owner, member);
    return new SchemaTypeRef(SchemaTypeKind.Map, map, isNullable, key, value);
  }

  private SchemaTypeRef ClassifyKey(Type keyType, string owner, string member)
  {
    if (ScalarTable.IsFloatingPoint(keyType))
    {
      Warn(
        SchemaDiagnosticCodes.FloatingPointDictionaryKey,
        $"Property '{owner}.{member}' is keyed by '{keyType.FullName}'. Floating-point keys are written " +
        "as shortest-round-trip strings, which other languages will not reproduce identically.",
        owner,
        member);
      return new SchemaTypeRef(SchemaTypeKind.Scalar, keyType, false);
    }

    if (ScalarTable.IsScalar(keyType))
    {
      return new SchemaTypeRef(SchemaTypeKind.Scalar, keyType, false);
    }

    if (keyType.IsEnum)
    {
      // Always a name, whatever converter is in force: System.Text.Json fills its write-name cache for
      // every member regardless, because a JSON property name cannot be a number.
      return new SchemaTypeRef(SchemaTypeKind.Enum, keyType, false, AsString: true);
    }

    Error(
      SchemaDiagnosticCodes.UnsupportedDictionaryKey,
      $"Property '{owner}.{member}' is keyed by '{keyType.FullName}', which System.Text.Json cannot " +
      "write as a property name.",
      owner,
      member);
    return new SchemaTypeRef(SchemaTypeKind.Unmapped, keyType, false);
  }

  private static bool IsBinary(Type type) =>
    (type.IsArray && type.GetElementType()?.FullName == "System.Byte") ||
    (type.IsGenericType &&
     type.GetGenericTypeDefinition().FullName is "System.Memory`1" or "System.ReadOnlyMemory`1" &&
     type.GetGenericArguments()[0].FullName == "System.Byte");

  private void Warn(string code, string message, string owner, string member) =>
    Report(false, code, message, owner, member);

  private void Error(string code, string message, string owner, string member) =>
    Report(true, code, message, owner, member);

  private void Report(bool isError, string code, string message, string owner, string member) =>
    diagnostics.Add(new SchemaDiagnostic(
      isError ? SchemaDiagnosticSeverity.Error : SchemaDiagnosticSeverity.Warning,
      code,
      message,
      owner,
      member));
}
