namespace DotNetSchema.Generator;

/// <summary>
/// A type, named in configuration, and the inline JSON Schema it is described by wherever it appears.
/// </summary>
/// <param name="TypeName">
/// The type's full name as the runtime spells it: a nested type as <c>Outer+Inner</c>, a generic type by its
/// definition, with its arity, as <c>Ns.Id`1</c> — which then covers every construction of it.
/// </param>
/// <param name="Type">The JSON Schema <c>type</c>: <c>string</c>, <c>number</c>, <c>integer</c> or <c>boolean</c>.</param>
/// <param name="Format">The JSON Schema <c>format</c>, or <see langword="null" /> for none.</param>
/// <param name="Pattern">The JSON Schema <c>pattern</c>, or <see langword="null" /> for none; <c>string</c> only.</param>
public sealed record TypeMapping(string TypeName, string Type, string? Format = null, string? Pattern = null);
