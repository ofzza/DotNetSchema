namespace DotNetSchema.Generator.Diagnostics;

/// <summary>
/// The diagnostic codes the generator can raise. Codes are permanent once published — a build that
/// suppresses one by code must keep suppressing the same thing — so retire a code rather than reusing it.
/// </summary>
public static class SchemaDiagnosticCodes
{
  /// <summary>A marked type is not a concrete class, record or struct.</summary>
  public const string MarkedTypeNotSupported = "DNS0001";

  /// <summary>A property's type has no JSON Schema mapping.</summary>
  public const string UnmappedType = "DNS0002";

  /// <summary>A generic type appeared in the closure; generic models are not supported.</summary>
  public const string GenericModel = "DNS0003";

  /// <summary>A dictionary key type that System.Text.Json cannot write as a property name.</summary>
  public const string UnsupportedDictionaryKey = "DNS0004";

  /// <summary>Two types shared a short name; both definitions were qualified to tell them apart.</summary>
  public const string DefinitionNameCollision = "DNS0005";

  /// <summary>Two properties of one type render to the same JSON name.</summary>
  public const string PropertyNameCollision = "DNS0006";

  /// <summary>The file name given to the marker attribute is not a bare JSON file name.</summary>
  public const string InvalidFileName = "DNS0007";

  /// <summary>A property's enum converter disagrees with the one on the enum type.</summary>
  public const string EnumConverterDeviates = "DNS0009";

  /// <summary>An assembly yielded types that could not be loaded; the rest were still scanned.</summary>
  public const string PartialTypeLoad = "DNS0010";

  /// <summary>An assembly carries no nullability annotations; its references were treated as non-null.</summary>
  public const string NullabilityUnknown = "DNS0011";

  /// <summary>A dictionary keyed by a floating-point type, whose keys will not agree across languages.</summary>
  public const string FloatingPointDictionaryKey = "DNS0012";
}
