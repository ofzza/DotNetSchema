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

  /// <summary>The attributes on a type or property could not be read; its decorators were skipped.</summary>
  public const string DecoratorUnreadable = "DNS0014";

  /// <summary>A selected attribute's standard keyword does not apply to the property's schema.</summary>
  public const string DecoratorNotApplicable = "DNS0015";

  /// <summary>
  /// A type mapping, configured or declared with <c>[DotNetSchemaType]</c>, is malformed; it was ignored.
  /// </summary>
  public const string InvalidTypeMapping = "DNS0016";

  /// <summary>A configured type mapping matched no type any document reached.</summary>
  public const string TypeMappingUnused = "DNS0017";

  /// <summary>A type marked for export is also mapped, so it cannot be a document's definition.</summary>
  public const string MappedRoot = "DNS0018";

  /// <summary>
  /// A model position is governed by a <c>[JsonConverter]</c> and has no type mapping, so its shape is unknown.
  /// </summary>
  public const string ConverterWithoutMapping = "DNS0019";

  /// <summary>A <c>[DotNetSchemaFormat]</c> on a property is malformed; it was ignored.</summary>
  public const string InvalidPropertyFormat = "DNS0020";

  /// <summary>
  /// A <c>[DotNetSchemaFormat]</c> is on a property whose value takes no format or pattern — a model, an
  /// enum, or a pattern on a value that is not a string; it was ignored.
  /// </summary>
  public const string PropertyFormatNotApplicable = "DNS0021";
}
