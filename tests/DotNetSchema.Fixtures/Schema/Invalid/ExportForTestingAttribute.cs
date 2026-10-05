namespace DotNetSchema.Fixtures.Schema.Invalid;

/// <summary>
/// Marks a type in this folder as an entry point for the exporter's own diagnostic tests.
/// </summary>
/// <remarks>
/// A separate marker from <c>[DotNetSchema]</c> on purpose. Everything in this folder is deliberately
/// wrong in some way, so marking it with the real attribute would make the fixtures project fail its own
/// build. The exporter reads its marker by name, through <c>SchemaGenerationOptions</c>, so a test can
/// point it at this one instead and get the diagnostics without any of it reaching a real export.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
public sealed class ExportForTestingAttribute : Attribute
{
  /// <summary>Marks the type for export to the default document.</summary>
  public ExportForTestingAttribute()
  {
  }

  /// <summary>Marks the type for export to a named document.</summary>
  /// <param name="fileName">The document name, valid or otherwise.</param>
  public ExportForTestingAttribute(string fileName)
  {
    FileName = fileName;
  }

  /// <summary>The document name given, or <see langword="null" />.</summary>
  public string? FileName { get; }
}
