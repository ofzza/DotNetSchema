namespace DotNetSchema.Generator.Model;

/// <summary>What a C# type turns into in a schema.</summary>
public enum SchemaTypeKind
{
  /// <summary>A portable primitive; see <see cref="ScalarTable" />.</summary>
  Scalar,

  /// <summary>An enum, which gets a definition of its own.</summary>
  Enum,

  /// <summary>A model, which gets a definition of its own and is only ever referenced by <c>$ref</c>.</summary>
  Model,

  /// <summary>A sequence.</summary>
  Array,

  /// <summary>A dictionary.</summary>
  Map,

  /// <summary>A byte array or byte buffer, which System.Text.Json writes as base64.</summary>
  Binary,

  /// <summary>Something with no JSON Schema representation.</summary>
  Unmapped,

  /// <summary>
  /// A type described by a declared inline schema — a configured mapping or <c>[DotNetSchemaType]</c> —
  /// rather than by its shape. Never a definition, never a <c>$ref</c>.
  /// </summary>
  Mapped,

  /// <summary>
  /// A model position a <c>[JsonConverter]</c> governs with no mapping to say what it writes: an empty
  /// schema, because anything narrower would be a guess.
  /// </summary>
  Opaque,
}

/// <summary>
/// One position in a schema: what goes there, whether it may be null, and what is nested inside it.
/// </summary>
/// <param name="Kind">What the type turns into.</param>
/// <param name="Type">The CLR type at this position, with any <c>Nullable&lt;T&gt;</c> already stripped.</param>
/// <param name="IsNullable">Whether a null is permitted here.</param>
/// <param name="Key">For a <see cref="SchemaTypeKind.Map" />, what constrains its property names.</param>
/// <param name="Item">For an array its element, for a map its value.</param>
/// <param name="AsString">For an <see cref="SchemaTypeKind.Enum" />, whether it serialises as a name.</param>
/// <param name="Mapping">For a <see cref="SchemaTypeKind.Mapped" /> type, the schema declared for it.</param>
public sealed record SchemaTypeRef(
  SchemaTypeKind Kind,
  Type Type,
  bool IsNullable,
  SchemaTypeRef? Key = null,
  SchemaTypeRef? Item = null,
  bool AsString = false,
  ResolvedTypeMapping? Mapping = null);
