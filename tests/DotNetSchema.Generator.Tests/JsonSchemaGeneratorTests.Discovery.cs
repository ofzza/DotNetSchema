using DotNetSchema.Fixtures.Schema;

namespace DotNetSchema.Generator.Tests;

public sealed partial class JsonSchemaGeneratorTests
{
  [Fact]
  public void Scanning_FindsEveryMarkedType_AndTheDocumentItBelongsTo()
  {
    var result = new JsonSchemaGenerator().Generate([FixturesAssembly]);

    Assert.Empty(result.Diagnostics);
    Assert.Equal(
      ["assessment.json", "schema.json", "school.json"],
      result.Documents.Select(d => d.FileName));
  }

  [Fact]
  public void ADocumentWithSeveralMarkedTypes_NamesThemAllAsRoots()
  {
    var result = new JsonSchemaGenerator().Generate([FixturesAssembly]);
    var assessment = result.Documents.Single(d => d.FileName == "assessment.json");

    Assert.Equal(["Assessment", "AssessmentRecord"], assessment.RootDefinitionNames);
  }

  [Fact]
  public void AnUnmarkedAssembly_ProducesNothingRatherThanAnEmptyDocument()
  {
    var result = new JsonSchemaGenerator().Generate([typeof(JsonSchemaGenerator).Assembly]);

    Assert.Empty(result.Documents);
    Assert.Empty(result.Diagnostics);
  }

  [Fact]
  public void ATypeReachableFromTwoDocuments_IsDefinedIdenticallyInBoth()
  {
    // ScalarRegister is marked into schema.json and also sits under AssessmentRecord in assessment.json.
    // Each document is self-contained, so the definition is duplicated - but it must be the same bytes,
    // because a definition is a function of the type and the options and of nothing else.
    var result = new JsonSchemaGenerator().Generate([FixturesAssembly]);

    var inSchema = Definition("ScalarRegister", typeof(ScalarRegister));
    var inAssessment = Definition("ScalarRegister", typeof(AssessmentRecord));

    Assert.Contains("ScalarRegister", result.Documents.Single(d => d.FileName == "schema.json").DefinitionNames);
    Assert.Contains("ScalarRegister", result.Documents.Single(d => d.FileName == "assessment.json").DefinitionNames);
    Assert.Equal(Compact(inSchema), Compact(inAssessment));
  }
}
