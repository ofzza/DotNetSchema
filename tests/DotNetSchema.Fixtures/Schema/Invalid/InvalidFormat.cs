namespace DotNetSchema.Fixtures.Schema.Invalid;

/// <summary>A <c>[DotNetSchemaFormat]</c> whose pattern is not a regular expression. Raises DNS0020.</summary>
[ExportForTesting("invalid-format.json")]
public sealed record InvalidFormat
{
  /// <summary>A malformed declaration, which is ignored.</summary>
  [DotNetSchemaFormat(Pattern = "([")]
  public required string Code { get; init; }
}
