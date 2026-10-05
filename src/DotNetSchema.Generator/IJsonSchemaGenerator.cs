using System.Reflection;

namespace DotNetSchema.Generator;

/// <summary>Turns marked C# models into JSON Schema Draft 2020-12 documents.</summary>
/// <remarks>
/// Every method takes ordinary <see cref="Assembly" /> and <see cref="Type" /> instances and asks nothing
/// of them that a metadata-only load cannot answer, so the same call works against a
/// <c>MetadataLoadContext</c> — which is how the build integration uses it — and against the running
/// assemblies, which is how the tests use it. The two are required to agree byte for byte.
/// </remarks>
public interface IJsonSchemaGenerator
{
  /// <summary>Scans assemblies for marked types and emits one document per distinct target file.</summary>
  /// <param name="assemblies">The assemblies to scan. Referenced assemblies are read but not scanned.</param>
  /// <param name="options">Generation options, or <see langword="null" /> for the defaults.</param>
  SchemaGenerationResult Generate(IReadOnlyList<Assembly> assemblies, SchemaGenerationOptions? options = null);

  /// <summary>Emits documents for explicitly named roots, bypassing the attribute scan.</summary>
  /// <param name="roots">The types to export, each with the document it belongs to.</param>
  /// <param name="options">Generation options, or <see langword="null" /> for the defaults.</param>
  SchemaGenerationResult GenerateFor(IReadOnlyList<SchemaRoot> roots, SchemaGenerationOptions? options = null);
}
