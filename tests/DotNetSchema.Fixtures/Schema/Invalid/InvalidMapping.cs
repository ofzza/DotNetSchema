namespace DotNetSchema.Fixtures.Schema.Invalid;

/// <summary>Reaches a type whose <c>[DotNetSchemaType]</c> names no JSON Schema type. Raises DNS0016.</summary>
[ExportForTesting("invalid-mapping.json")]
public sealed record InvalidMapping
{
  /// <summary>A type with a malformed declaration, which is ignored.</summary>
  public required BadlyMapped Value { get; init; }
}

/// <summary>Declares a JSON Schema type that does not exist.</summary>
[DotNetSchemaType("text")]
public sealed record BadlyMapped(string Text);
