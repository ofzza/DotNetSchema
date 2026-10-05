using System.Text.Json;

namespace DotNetSchema.Generator;

/// <summary>
/// Everything about schema generation that is a choice rather than a fact. Defaults describe what the
/// build integration emits; a caller overrides one only to depart from that.
/// </summary>
public sealed record SchemaGenerationOptions
{
  /// <summary>The options the build integration uses.</summary>
  public static SchemaGenerationOptions Default { get; } = new();

  /// <summary>
  /// Full name of the attribute that marks a type for export. Matched by name rather than by identity,
  /// so a consumer compiled against a different build of the annotations assembly still resolves.
  /// </summary>
  public string MarkerAttributeFullName { get; init; } = typeof(DotNetSchemaAttribute).FullName!;

  /// <summary>Document a type is exported to when its attribute names none.</summary>
  public string DefaultFileName { get; init; } = "schema.json";

  /// <summary>
  /// Prefix the <c>$id</c> of each document is built from. The default is a URN, which is a valid
  /// absolute URI and implies no retrievable location; replace it to publish the schemas somewhere.
  /// </summary>
  public string BaseUri { get; init; } = "urn:dotnetschema:schema:";

  /// <summary>Whether to emit <c>title</c> on each document.</summary>
  public bool EmitTitles { get; init; } = true;

  /// <summary>
  /// Whether to emit <c>$comment</c> where a mapping is lossy or surprising — an integer whose bounds
  /// cannot be stated honestly, a string format that would be a lie, an enum whose key form differs
  /// from its value form. Turning this off makes the documents smaller and harder to review.
  /// </summary>
  public bool EmitComments { get; init; } = true;

  /// <summary>How C# property names are rendered.</summary>
  public JsonNamingPolicy PropertyNamingPolicy { get; init; } = JsonNamingPolicy.CamelCase;

  /// <summary>
  /// How enum member names are rendered, when they are rendered at all. A naming policy configured on
  /// <see cref="JsonSerializerOptions" /> is invisible to a metadata-only read, so an application that
  /// sets one has to repeat it here.
  /// </summary>
  public JsonNamingPolicy? EnumNamingPolicy { get; init; }

  /// <summary>Which integer bounds are worth stating.</summary>
  public IntegerBoundsMode IntegerBounds { get; init; } = IntegerBoundsMode.ExactlyRepresentable;

  /// <summary>How an enum serialised as a number carries its member names.</summary>
  public NumericEnumStyle NumericEnums { get; init; } = NumericEnumStyle.OneOfConstTitle;

  /// <summary>How an unmapped property type is reported.</summary>
  public UnmappedTypePolicy UnmappedTypes { get; init; } = UnmappedTypePolicy.Error;
}

/// <summary>Which <c>minimum</c> / <c>maximum</c> bounds are emitted for the fixed-width integers.</summary>
public enum IntegerBoundsMode
{
  /// <summary>No bounds at all; every integer is a bare <c>{"type":"integer"}</c>.</summary>
  None,

  /// <summary>
  /// Only bounds exactly representable as an IEEE-754 double. Most validators parse JSON numbers into
  /// doubles, where <c>ulong.MaxValue</c> reads back one larger than it was written — a constraint whose
  /// meaning depends on the reader is worse than no constraint.
  /// </summary>
  ExactlyRepresentable,

  /// <summary>Every bound, including those a double-backed reader will widen.</summary>
  All,
}

/// <summary>How an enum that serialises as a number carries its member names.</summary>
public enum NumericEnumStyle
{
  /// <summary>
  /// A <c>oneOf</c> of annotated constants — <c>{"const":0,"title":"Student"}</c> — so the names survive
  /// for code generation and review while the constraint stays numeric.
  /// </summary>
  OneOfConstTitle,

  /// <summary>A bare <c>{"type":"integer","enum":[0,1,...]}</c>. Terser, and the names are lost.</summary>
  Enum,
}

/// <summary>What happens when a property's type has no JSON Schema mapping.</summary>
public enum UnmappedTypePolicy
{
  /// <summary>Report an error and produce no document containing it.</summary>
  Error,

  /// <summary>Report a warning and omit the property.</summary>
  Warn,
}
