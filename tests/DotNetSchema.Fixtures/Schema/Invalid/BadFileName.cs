namespace DotNetSchema.Fixtures.Schema.Invalid;

/// <summary>Names a document outside the output directory. Raises DNS0007.</summary>
[ExportForTesting("../escape.json")]
public sealed record BadFileName
{
  /// <summary>Any member; the file name is rejected before the type is walked.</summary>
  public required string Name { get; init; }
}
