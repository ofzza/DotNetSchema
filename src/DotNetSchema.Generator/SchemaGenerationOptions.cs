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

  /// <summary>
  /// The shape an enum value takes, and how it carries its member names — whether it is written as a number
  /// or, under a string converter, as a string. A dictionary key is unaffected: it is always a plain
  /// <c>enum</c> of the names it is written under.
  /// </summary>
  public EnumStyle EnumStyle { get; init; } = EnumStyle.OneOfConstTitle;

  /// <summary>How an unmapped property type is reported.</summary>
  public UnmappedTypePolicy UnmappedTypes { get; init; } = UnmappedTypePolicy.Error;

  /// <summary>
  /// The rules every selection starts from: the declarative validation and description attributes a schema
  /// reader is likely to want, and nothing that only matters to the compiler or the runtime.
  /// <see cref="DecoratorRules" /> are applied after these, so a caller can take any of them back.
  /// </summary>
  /// <remarks>
  /// Part of the output contract — changing this list changes every consumer's documents. The DataAnnotations
  /// sub-namespaces are excluded explicitly because <c>*</c> is recursive: without it, Entity Framework's
  /// <c>DataAnnotations.Schema</c> mapping attributes (<c>[Column]</c>, <c>[Table]</c>, …) would come in too.
  /// </remarks>
  public static IReadOnlyList<DecoratorRule> DefaultDecorators { get; } =
  [
    new("System.ComponentModel.DataAnnotations.*", Include: true),
    new("System.ComponentModel.DataAnnotations.*.*", Include: false),
    new("System.ComponentModel.DefaultValueAttribute", Include: true),
    new("System.ComponentModel.DescriptionAttribute", Include: true),
    new("System.ComponentModel.DisplayNameAttribute", Include: true),
    new("System.ObsoleteAttribute", Include: true),
  ];

  /// <summary>
  /// Rules applied after <see cref="DefaultDecorators" />, in order, choosing which attributes are exposed as
  /// <c>x-decorators</c> and translated into standard keywords. For each attribute the <em>last</em> rule whose
  /// pattern matches its full name decides; an attribute no rule matches is left out. In a pattern <c>*</c>
  /// matches any run of characters, dots included, so <c>Ns.*</c> covers sub-namespaces and
  /// <c>(Ns.*.*, exclude)</c> after it narrows that to the types directly in <c>Ns</c>; <c>(*, exclude)</c>
  /// clears everything before it.
  /// </summary>
  public IReadOnlyList<DecoratorRule> DecoratorRules { get; init; } = [];

  /// <summary>
  /// Types described inline by a declared schema rather than by their shape — the build's
  /// <c>DotNetSchemaType</c> items. These take precedence over <c>[DotNetSchemaType]</c> on the type itself and
  /// over the built-in mapping of the framework's primitives; of two entries for one type, the later wins.
  /// </summary>
  /// <remarks>
  /// A mapping asserts how the type is serialised; nothing here can check it. That is the second deliberate
  /// step beyond "the schema describes what System.Text.Json writes", after the decorator keywords.
  /// </remarks>
  public IReadOnlyList<TypeMapping> TypeMappings { get; init; } = [];
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

/// <summary>
/// The shape an enum value takes. Every style keeps <c>type</c> (<c>integer</c>, or <c>string</c> under a string
/// converter); they differ in how the member names travel. A name is always the C# member name, because the
/// value already is the JSON one: a string enum renamed by <c>[JsonStringEnumMemberName]</c> or a naming
/// policy writes <c>"in-person"</c>, and only the name says it is <c>InPerson</c>.
/// </summary>
public enum EnumStyle
{
  /// <summary>
  /// A <c>oneOf</c> of annotated constants — <c>{"const":0,"title":"Student"}</c> — so the names survive
  /// for code generation and review while the constraint stays on the values. Standard JSON Schema.
  /// </summary>
  OneOfConstTitle,

  /// <summary>A bare <c>{"enum":[0,1,...]}</c>. Terser, and the names are lost.</summary>
  Enum,

  /// <summary>
  /// An <c>enum</c> of the values beside an <c>x-enumNames</c> array of the names, in the same order — the
  /// shape NSwag popularised, and the one several code generators look for. <c>x-enumNames</c> is a vendor
  /// keyword: Draft 2020-12 treats an unknown keyword as an annotation, but a strict validator (Ajv in its
  /// default strict mode, for one) rejects the schema unless the keyword is registered with it.
  /// </summary>
  EnumWithNames,
}

/// <summary>What happens when a property's type has no JSON Schema mapping.</summary>
public enum UnmappedTypePolicy
{
  /// <summary>Report an error and produce no document containing it.</summary>
  Error,

  /// <summary>Report a warning and omit the property.</summary>
  Warn,
}
