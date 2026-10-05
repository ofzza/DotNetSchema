namespace DotNetSchema;

/// <summary>
/// Marks a data model for export to a JSON Schema document at build time.
/// </summary>
/// <remarks>
/// The export itself is performed by the build integration in
/// <c>buildTransitive/DotNetSchema.targets</c>, which runs after the compile and reads the marked
/// types back out of the produced assembly. Referencing the package alone marks types but exports
/// nothing: the export runs only in a project that sets <c>DotNetSchemaGenerate</c> to <c>true</c>,
/// which scans its own assembly and every project it references. See the project README.
/// <para>
/// Every model reachable from a marked type through its properties is carried into the same document,
/// so only the entry points need marking. A type may be marked more than once to publish it into
/// several documents.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class DotNetSchemaAttribute : Attribute
{
  /// <summary>
  /// Marks the type for export to the default schema document, <c>&lt;AssemblyName&gt;.schema.json</c>
  /// of the exporting project unless <c>DotNetSchemaDefaultFileName</c> says otherwise.
  /// </summary>
  public DotNetSchemaAttribute()
  {
  }

  /// <summary>Marks the type for export to a named schema document.</summary>
  /// <param name="fileName">
  /// File name of the document to export to, for example <c>public-api.json</c>. A bare file name, not
  /// a path: it is resolved against the configured output directory.
  /// </param>
  public DotNetSchemaAttribute(string fileName)
  {
    FileName = fileName;
  }

  /// <summary>
  /// File name of the document this type is exported to, or <see langword="null" /> when the configured
  /// default is used.
  /// </summary>
  public string? FileName { get; }
}
