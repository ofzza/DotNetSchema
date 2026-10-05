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
      ["clashing-names.json", "colliding.json", "floating-key.json"],
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
  public void ADiagnostic_RendersInMsBuildsCanonicalForm()
  {
    var diagnostic = Assert.Single(
      GenerateInvalid().Diagnostics,
      d => d.Code == SchemaDiagnosticCodes.UnmappedType);

    Assert.StartsWith("tool : error DNS0002: ", diagnostic.ToCanonicalString("tool"), StringComparison.Ordinal);
  }
}
