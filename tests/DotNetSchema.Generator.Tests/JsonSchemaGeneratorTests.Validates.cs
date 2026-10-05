using System.Text.Json;
using System.Text.Json.Nodes;
using DotNetSchema.Fixtures.Generation;
using DotNetSchema.Fixtures.Schema;

namespace DotNetSchema.Generator.Tests;

public sealed partial class JsonSchemaGeneratorTests
{
  /// <summary>
  /// The naming policy the schema is written against. Anything else and the property names would not line
  /// up, which is the point: the document describes one serialisation configuration, not the type alone.
  /// </summary>
  private static readonly JsonSerializerOptions Serialisation = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
  };

  /// <summary>
  /// The test that keeps the rest honest: everything else compares the schema against what this project
  /// believes System.Text.Json does, whereas this compares it against what System.Text.Json actually did.
  /// </summary>
  [Theory]
  [InlineData(1)]
  [InlineData(7)]
  [InlineData(42)]
  public void ARealFixtureGraph_ValidatesAgainstTheSchemaGeneratedForIt(int seed)
  {
    IFixturesService fixtures = new FixturesService();
    var instance = JsonSerializer.SerializeToNode(fixtures.Generate(seed), Serialisation);
    var document = Document(typeof(School));

    var failures = JsonSchemaValidator.Validate(instance, document, "School");

    Assert.Empty(failures);
  }

  [Theory]
  [InlineData(1)]
  [InlineData(99)]
  public void TheScalarCoverageRecord_ValidatesAgainstItsSchema(int seed)
  {
    IFixturesService fixtures = new FixturesService();
    var instance = JsonSerializer.SerializeToNode(fixtures.GenerateAssessmentRecord(seed), Serialisation);
    var document = Document(typeof(AssessmentRecord));

    var failures = JsonSchemaValidator.Validate(instance, document, "AssessmentRecord");

    Assert.Empty(failures);
  }

  [Fact]
  public void TheValidator_ImplementsEveryKeywordTheGeneratorEmits()
  {
    // Without this the validating tests above could pass by ignoring a keyword they do not understand.
    var document = Document(typeof(School));

    Assert.Empty(JsonSchemaValidator.Unsupported(document).Except(JsonSchemaValidator.KnownKeywords));
  }

  [Fact]
  public void TheValidator_ActuallyRejectsSomething()
  {
    // A validator that never fails would make every assertion above vacuous.
    var document = Document(typeof(School));
    var instance = (JsonObject)JsonSerializer.SerializeToNode(
      new FixturesService().Generate(1),
      Serialisation)!;

    instance["id"] = "not-a-uuid";
    instance.Remove("name");

    var failures = JsonSchemaValidator.Validate(instance, document, "School");

    Assert.Contains(failures, f => f.Contains("not a valid uuid", StringComparison.Ordinal));
    Assert.Contains(failures, f => f.Contains("required property 'name' is absent", StringComparison.Ordinal));
  }
}
