using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using DotNetSchema.Fixtures.Schema;
using DotNetSchema.Generator;

namespace DotNetSchema.Generator.Tests;

/// <summary>
/// Tests for <see cref="JsonSchemaGenerator" />, split across files by the contract under test.
/// </summary>
/// <remarks>
/// Every model these tests run over comes from <c>DotNetSchema.Fixtures</c>, including the
/// deliberately awkward ones under <c>Schema/Invalid/</c>. Nothing here declares a type of its own to test
/// against: a shape worth asserting on is a shape worth adding to the fixtures, where the TypeScript side
/// will meet it too.
/// </remarks>
public sealed partial class JsonSchemaGeneratorTests
{
  /// <summary>The assembly every test reads its models out of.</summary>
  private static Assembly FixturesAssembly => typeof(School).Assembly;

  /// <summary>
  /// The marker the invalid cases carry. They cannot use the real one, or the fixtures project would fail
  /// its own build exporting them.
  /// </summary>
  private const string TestingMarker =
    "DotNetSchema.Fixtures.Schema.Invalid.ExportForTestingAttribute";

  private static SchemaGenerationResult Generate(params Type[] roots) =>
    new JsonSchemaGenerator().GenerateFor([.. roots.Select(t => new SchemaRoot(t, "test.json"))]);

  /// <summary>Runs the attribute scan over the invalid cases, which carry the testing marker.</summary>
  private static SchemaGenerationResult GenerateInvalid() =>
    new JsonSchemaGenerator().Generate(
      [FixturesAssembly],
      SchemaGenerationOptions.Default with { MarkerAttributeFullName = TestingMarker });

  /// <summary>The single document produced for the given roots, parsed.</summary>
  private static JsonObject Document(params Type[] roots)
  {
    var result = Generate(roots);
    Assert.Empty(result.Diagnostics);
    return (JsonObject)JsonNode.Parse(Assert.Single(result.Documents).Json)!;
  }

  /// <summary>One definition out of a document produced for the given roots.</summary>
  private static JsonObject Definition(string name, params Type[] roots) =>
    (JsonObject)Document(roots)["$defs"]![name]!;

  /// <summary>One property's schema out of a definition.</summary>
  private static JsonObject Property(JsonObject definition, string name) =>
    (JsonObject)definition["properties"]![name]!;

  /// <summary>A definition's <c>required</c> list.</summary>
  private static IReadOnlyList<string> Required(JsonObject definition) =>
    definition["required"] is JsonArray array ? [.. array.Select(n => n!.GetValue<string>())] : [];

  /// <summary>Compact JSON for a node, so an assertion can be written as one literal.</summary>
  private static string Compact(JsonNode? node) =>
    node?.ToJsonString(new JsonSerializerOptions { WriteIndented = false }) ?? "null";
}
