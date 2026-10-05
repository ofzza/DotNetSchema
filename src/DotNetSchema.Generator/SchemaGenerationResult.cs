using System.Text;

namespace DotNetSchema.Generator;

/// <summary>Everything one generation run produced: the documents, and what went wrong along the way.</summary>
/// <param name="Documents">One per distinct target file, ordered by file name.</param>
/// <param name="Diagnostics">Ordered by code, then by the type and member they concern.</param>
public sealed record SchemaGenerationResult(
  IReadOnlyList<SchemaDocument> Documents,
  IReadOnlyList<SchemaDiagnostic> Diagnostics)
{
  /// <summary>Whether any diagnostic is an error, in which case the documents are incomplete.</summary>
  public bool HasErrors => Diagnostics.Any(d => d.Severity == SchemaDiagnosticSeverity.Error);
}

/// <summary>One emitted JSON Schema document.</summary>
/// <param name="FileName">Bare file name, as named by the attribute or defaulted.</param>
/// <param name="RootDefinitionNames">Definitions produced by an explicitly marked type, ordered.</param>
/// <param name="DefinitionNames">Every definition in the document, ordered as they are emitted.</param>
/// <param name="Json">The file content exactly, trailing newline included.</param>
public sealed record SchemaDocument(
  string FileName,
  IReadOnlyList<string> RootDefinitionNames,
  IReadOnlyList<string> DefinitionNames,
  string Json)
{
  /// <summary>The content as it is written to disk: UTF-8, no byte-order mark.</summary>
  public byte[] ToUtf8Bytes() => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(Json);
}

/// <summary>How much a diagnostic matters.</summary>
public enum SchemaDiagnosticSeverity
{
  /// <summary>Something to know about; the document was still produced.</summary>
  Warning,

  /// <summary>Something that has to be fixed; the affected document was not produced.</summary>
  Error,
}

/// <summary>One thing the generator has to say about its input.</summary>
/// <param name="Severity">Whether this stops a document being produced.</param>
/// <param name="Code">A <c>DNS….</c> code; see <see cref="Diagnostics.SchemaDiagnosticCodes" />.</param>
/// <param name="Message">One sentence, naming what is wrong and where.</param>
/// <param name="DeclaringTypeName">Full name of the type concerned, when there is one.</param>
/// <param name="MemberName">Name of the member concerned, when there is one.</param>
/// <param name="Location">Where in the source it sits, when a portable PDB made that knowable.</param>
public sealed record SchemaDiagnostic(
  SchemaDiagnosticSeverity Severity,
  string Code,
  string Message,
  string? DeclaringTypeName = null,
  string? MemberName = null,
  SourceLocation? Location = null)
{
  /// <summary>
  /// Renders the diagnostic in MSBuild's canonical form, which <c>Exec</c> recognises in tool output and
  /// promotes to a real build error or warning.
  /// </summary>
  /// <param name="toolOrigin">Origin to name when no source location is known.</param>
  public string ToCanonicalString(string toolOrigin)
  {
    var severity = Severity == SchemaDiagnosticSeverity.Error ? "error" : "warning";
    return Location is { } location
      ? $"{location.FilePath}({location.Line},{location.Column}): {severity} {Code}: {Message}"
      : $"{toolOrigin} : {severity} {Code}: {Message}";
  }
}

/// <summary>A point in a source file, recovered from a portable PDB.</summary>
/// <param name="FilePath">Absolute path as the compiler recorded it.</param>
/// <param name="Line">One-based line.</param>
/// <param name="Column">One-based column.</param>
public readonly record struct SourceLocation(string FilePath, int Line, int Column);

/// <summary>A type to export, and the document to export it to.</summary>
/// <param name="Type">The model type.</param>
/// <param name="FileName">Bare file name of the document.</param>
public readonly record struct SchemaRoot(Type Type, string FileName);
