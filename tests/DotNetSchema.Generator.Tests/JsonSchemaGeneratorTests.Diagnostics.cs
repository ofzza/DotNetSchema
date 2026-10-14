using System.Text.Json.Nodes;
using DotNetSchema.Generator.Diagnostics;

namespace DotNetSchema.Generator.Tests;

public sealed partial class JsonSchemaGeneratorTests
{
  [Theory]
  [InlineData(SchemaDiagnosticCodes.MarkedTypeNotSupported, SchemaDiagnosticSeverity.Error)]
  [InlineData(SchemaDiagnosticCodes.UnmappedType, SchemaDiagnosticSeverity.Error)]
  [InlineData(SchemaDiagnosticCodes.GenericModel, SchemaDiagnosticSeverity.Error)]
  [InlineData(SchemaDiagnosticCodes.UnsupportedDictionaryKey, SchemaDiagnosticSeverity.Error)]
  [InlineData(SchemaDiagnosticCodes.InvalidFileName, SchemaDiagnosticSeverity.Error)]
  [InlineData(SchemaDiagnosticCodes.DefinitionNameCollision, SchemaDiagnosticSeverity.Warning)]
  [InlineData(SchemaDiagnosticCodes.PropertyNameCollision, SchemaDiagnosticSeverity.Warning)]
  [InlineData(SchemaDiagnosticCodes.FloatingPointDictionaryKey, SchemaDiagnosticSeverity.Warning)]
  [InlineData(SchemaDiagnosticCodes.DecoratorNotApplicable, SchemaDiagnosticSeverity.Warning)]
  public void EachInvalidCase_RaisesItsCode(string code, SchemaDiagnosticSeverity severity)
  {
    var diagnostic = Assert.Single(GenerateInvalid().Diagnostics, d => d.Code == code);

    Assert.Equal(severity, diagnostic.Severity);
  }

  [Fact]
  public void AnErrorSuppressesItsOwnDocument_AndLeavesTheOthersAlone()
  {
    var result = GenerateInvalid();

    Assert.True(result.HasErrors);

    // Every document whose closure held an error is withheld; the merely-odd ones still come out.
    Assert.Equal(
      ["clashing-names.json", "colliding.json", "floating-key.json", "misapplied-decorator.json"],
      result.Documents.Select(d => d.FileName));
  }

  [Fact]
  public void ACollision_QualifiesBothTypes_NotOnlyTheNewcomer()
  {
    var colliding = GenerateInvalid().Documents.Single(d => d.FileName == "colliding.json");

    Assert.Contains("Left.Duplicate", colliding.DefinitionNames);
    Assert.Contains("Right.Duplicate", colliding.DefinitionNames);
    Assert.DoesNotContain("Duplicate", colliding.DefinitionNames);
  }

  [Fact]
  public void AMisappliedDecorator_KeepsItsEntry_ButContributesNoKeyword()
  {
    var document = GenerateInvalid().Documents.Single(d => d.FileName == "misapplied-decorator.json");
    var model = (JsonObject)JsonNode.Parse(document.Json)!["$defs"]!["MisappliedDecorator"]!;

    Assert.Equal(
      """{"type":"string","x-decorators":[{"name":"System.ComponentModel.DataAnnotations.RangeAttribute","arguments":{"minimum":1,"maximum":10}}]}""",
      Compact(Property(model, "code")));
  }

  [Fact]
  public void ADiagnostic_RendersInMsBuildsCanonicalForm()
  {
    var diagnostic = Assert.Single(
      GenerateInvalid().Diagnostics,
      d => d.Code == SchemaDiagnosticCodes.UnmappedType);

    Assert.StartsWith("tool : error DNS0002: ", diagnostic.ToCanonicalString("tool"), StringComparison.Ordinal);
  }
}
