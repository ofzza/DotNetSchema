using System.Reflection;
using DotNetSchema.Generator.Diagnostics;
using DotNetSchema.Generator.Emit;
using DotNetSchema.Generator.Model;
using DotNetSchema.Generator.Reflection;

namespace DotNetSchema.Generator;

/// <summary>
/// The default <see cref="IJsonSchemaGenerator" />. Stateless and thread-safe; one instance can be shared.
/// </summary>
/// <remarks>
/// Nothing here writes to the console or throws for a problem with its input — everything a caller needs
/// to know comes back as a <see cref="SchemaDiagnostic" />. That is what lets the build integration render
/// diagnostics in MSBuild's canonical form and the tests assert on them as data.
/// </remarks>
public sealed class JsonSchemaGenerator : IJsonSchemaGenerator
{
  /// <inheritdoc />
  public SchemaGenerationResult Generate(
    IReadOnlyList<Assembly> assemblies,
    SchemaGenerationOptions? options = null)
  {
    var effective = options ?? SchemaGenerationOptions.Default;
    var diagnostics = new List<SchemaDiagnostic>();
    var roots = new List<SchemaRoot>();

    foreach (var assembly in assemblies)
    {
      foreach (var type in Types(assembly, diagnostics))
      {
        roots.AddRange(RootsOf(type, effective, diagnostics));
      }
    }

    return Build(roots, effective, diagnostics);
  }

  /// <inheritdoc />
  public SchemaGenerationResult GenerateFor(
    IReadOnlyList<SchemaRoot> roots,
    SchemaGenerationOptions? options = null) =>
    Build(roots, options ?? SchemaGenerationOptions.Default, []);

  private static SchemaGenerationResult Build(
    IReadOnlyList<SchemaRoot> roots,
    SchemaGenerationOptions options,
    List<SchemaDiagnostic> diagnostics)
  {
    var documents = new List<SchemaDocument>();

    var files = roots
      .GroupBy(root => root.FileName, StringComparer.Ordinal)
      .OrderBy(group => group.Key, StringComparer.Ordinal);

    foreach (var file in files)
    {
      // Diagnostics are collected per document so that an error in one file does not suppress another.
      var local = new List<SchemaDiagnostic>();

      var ordered = file
        .Select(root => root.Type)
        .DistinctBy(MetadataFacts.TypeKey)
        .OrderBy(type => type.FullName, StringComparer.Ordinal)
        .ToList();

      var walker = new ClosureWalker(options, local);
      var closure = walker.Walk(ordered);

      foreach (var assembly in walker.UnannotatedAssemblies)
      {
        local.Add(new SchemaDiagnostic(
          SchemaDiagnosticSeverity.Warning,
          SchemaDiagnosticCodes.NullabilityUnknown,
          $"Assembly '{assembly}' contributes reference-typed members the compiler recorded no " +
          "nullability for, so they are described as non-nullable. That is a guess: compile it with " +
          "nullable reference types enabled to make it a fact."));
      }

      var names = DefinitionNameResolver.Resolve(closure, local);
      var document = new SchemaDocumentWriter(options, names, local).Write(file.Key, closure, ordered);

      diagnostics.AddRange(local);

      if (!local.Any(d => d.Severity == SchemaDiagnosticSeverity.Error))
      {
        documents.Add(document);
      }
    }

    return new SchemaGenerationResult(documents, Ordered(diagnostics));
  }

  private static IEnumerable<Type> Types(Assembly assembly, List<SchemaDiagnostic> diagnostics)
  {
    try
    {
      return assembly.GetTypes();
    }
    catch (ReflectionTypeLoadException exception)
    {
      diagnostics.Add(new SchemaDiagnostic(
        SchemaDiagnosticSeverity.Warning,
        SchemaDiagnosticCodes.PartialTypeLoad,
        $"Assembly '{assembly.GetName().Name}' yielded {exception.LoaderExceptions.Length} type(s) that " +
        "could not be loaded; the rest were scanned. A marked type among them was missed."));

      return exception.Types.OfType<Type>();
    }
  }

  private static IEnumerable<SchemaRoot> RootsOf(
    Type type,
    SchemaGenerationOptions options,
    List<SchemaDiagnostic> diagnostics)
  {
    var marks = MetadataFacts.FindAttributes(type, options.MarkerAttributeFullName);
    if (marks.Count == 0)
    {
      yield break;
    }

    if (!MetadataFacts.IsModelCandidate(type) || type.IsGenericType)
    {
      diagnostics.Add(new SchemaDiagnostic(
        SchemaDiagnosticSeverity.Error,
        SchemaDiagnosticCodes.MarkedTypeNotSupported,
        $"Type '{type.FullName}' is marked for export but is not a concrete, non-generic class, record " +
        "or struct.",
        type.FullName));
      yield break;
    }

    foreach (var mark in marks)
    {
      var named = mark.ConstructorArguments is [{ Value: string value }] ? value : null;
      var fileName = named ?? options.DefaultFileName;

      if (named is not null && !IsValidFileName(named))
      {
        diagnostics.Add(new SchemaDiagnostic(
          SchemaDiagnosticSeverity.Error,
          SchemaDiagnosticCodes.InvalidFileName,
          $"Type '{type.FullName}' names '{named}' as its schema file. That has to be a bare file name " +
          "ending in .json, with no directory separator and no parent-directory segment.",
          type.FullName));
        continue;
      }

      yield return new SchemaRoot(type, fileName);
    }
  }

  private static bool IsValidFileName(string fileName) =>
    fileName.Length > 0 &&
    fileName.IndexOfAny(['/', '\\']) < 0 &&
    !fileName.Contains("..", StringComparison.Ordinal) &&
    fileName.EndsWith(".json", StringComparison.Ordinal) &&
    fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;

  private static IReadOnlyList<SchemaDiagnostic> Ordered(IEnumerable<SchemaDiagnostic> diagnostics) =>
  [
    .. diagnostics
      .DistinctBy(d => (d.Code, d.DeclaringTypeName, d.MemberName, d.Message))
      .OrderBy(d => d.Code, StringComparer.Ordinal)
      .ThenBy(d => d.DeclaringTypeName, StringComparer.Ordinal)
      .ThenBy(d => d.MemberName, StringComparer.Ordinal),
  ];
}
