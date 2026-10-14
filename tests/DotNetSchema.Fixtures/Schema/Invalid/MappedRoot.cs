namespace DotNetSchema.Fixtures.Schema.Invalid;

/// <summary>
/// Marked for export and mapped at once — a document definition and an inline string cannot both be true.
/// Raises DNS0018.
/// </summary>
[ExportForTesting("mapped-root.json")]
[DotNetSchemaType("string")]
public sealed record MappedRoot(string Value);
