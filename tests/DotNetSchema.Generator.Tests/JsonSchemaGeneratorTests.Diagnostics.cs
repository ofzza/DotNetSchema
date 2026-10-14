using System.Text.Json.Nodes;
using DotNetSchema.Fixtures.Schema;
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
  [InlineData(SchemaDiagnosticCodes.InvalidTypeMapping, SchemaDiagnosticSeverity.Error)]
  [InlineData(SchemaDiagnosticCodes.MappedRoot, SchemaDiagnosticSeverity.Error)]
  [InlineData(SchemaDiagnosticCodes.ConverterWithoutMapping, SchemaDiagnosticSeverity.Warning)]
  [InlineData(SchemaDiagnosticCodes.InvalidPropertyFormat, SchemaDiagnosticSeverity.Error)]
  [InlineData(SchemaDiagnosticCodes.PropertyFormatNotApplicable, SchemaDiagnosticSeverity.Warning)]
  public void EachInvalidCase_RaisesItsCode(string code, SchemaDiagnosticSeverity severity)
  {
    // Some codes have more than one case (DNS0021 is raised for a model and for a misplaced pattern), but a
    // code never changes severity between them.
    var diagnostics = GenerateInvalid().Diagnostics.Where(d => d.Code == code).ToList();

    Assert.NotEmpty(diagnostics);
    Assert.All(diagnostics, d => Assert.Equal(severity, d.Severity));
  }

  [Fact]
  public void AnErrorSuppressesItsOwnDocument_AndLeavesTheOthersAlone()
  {
    var result = GenerateInvalid();

    Assert.True(result.HasErrors);

    // Every document whose closure held an error is withheld; the merely-odd ones still come out.
    Assert.Equal(
      [
        "clashing-names.json", "colliding.json", "converted.json", "floating-key.json", "misapplied-decorator.json",
        "misapplied-format.json",
      ],
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

  [Fact]
  public void AConvertedTypeWithoutAMapping_IsAnEmptySchema_NotItsMembers()
  {
    var document = GenerateInvalid().Documents.Single(d => d.FileName == "converted.json");
    var model = (JsonObject)JsonNode.Parse(document.Json)!["$defs"]!["ConvertedWithoutMapping"]!;

    Assert.Equal("{}", Compact(Property(model, "value")));
    Assert.DoesNotContain("Converted", document.DefinitionNames);
  }

  [Fact]
  public void AConfiguredMappingThatMatchesNothing_Warns_AndChangesNothingElse()
  {
    // DNS0017 has no fixture: a misspelt name is a configuration problem, not a model one.
    var options = SchemaGenerationOptions.Default with
    {
      TypeMappings = [new TypeMapping("DotNetSchema.Fixtures.Schema.StudentNumbr", "string")],
    };

    var result = Generate(options, typeof(School));

    var diagnostic = Assert.Single(result.Diagnostics);
    Assert.Equal(SchemaDiagnosticCodes.TypeMappingUnused, diagnostic.Code);
    Assert.Equal(SchemaDiagnosticSeverity.Warning, diagnostic.Severity);
    Assert.Equal(Document(typeof(School)).ToJsonString(), JsonNode.Parse(Assert.Single(result.Documents).Json)!.ToJsonString());
  }

  [Theory]
  [InlineData("DotNetSchema.Fixtures.Schema.StudentNumber", "text", null)]
  [InlineData("DotNetSchema.Fixtures.Schema.StudentNumber", "integer", "^\\d+$")]
  [InlineData("DotNetSchema.Fixtures.Schema.StudentNumber", "string", "([")]
  [InlineData(" ", "string", null)]
  public void AMalformedConfiguredMapping_IsAnError_AndIsIgnored(string name, string type, string? pattern)
  {
    var options = SchemaGenerationOptions.Default with
    {
      EmitComments = false,
      TypeMappings = [new TypeMapping(name, type, Pattern: pattern)],
    };

    var result = Generate(options, typeof(School));

    var diagnostic = Assert.Single(result.Diagnostics);
    Assert.Equal(SchemaDiagnosticCodes.InvalidTypeMapping, diagnostic.Code);
    Assert.Equal(SchemaDiagnosticSeverity.Error, diagnostic.Severity);

    // Ignored, so the type's own declaration still stands.
    var person = (JsonObject)JsonNode.Parse(Assert.Single(result.Documents).Json)!["$defs"]!["Person"]!;
    Assert.Equal("""{"type":["string","null"],"pattern":"^S\\d{7}$"}""", Compact(Property(person, "studentNumber")));
  }

  [Fact]
  public void AMalformedPropertyFormat_IsAnError_AndIsIgnored()
  {
    var diagnostic = Assert.Single(GenerateInvalid().Diagnostics, d => d.Code == SchemaDiagnosticCodes.InvalidPropertyFormat);

    Assert.Equal("Code", diagnostic.MemberName);
    Assert.Contains("not a valid regular expression", diagnostic.Message, StringComparison.Ordinal);
  }

  [Fact]
  public void AMisappliedPropertyFormat_Warns_AndContributesNothing()
  {
    var result = GenerateInvalid();
    var document = result.Documents.Single(d => d.FileName == "misapplied-format.json");
    var model = (JsonObject)JsonNode.Parse(document.Json)!["$defs"]!["MisappliedFormat"]!;

    Assert.Equal(
      ["Count", "Target"],
      result.Diagnostics
        .Where(d => d.Code == SchemaDiagnosticCodes.PropertyFormatNotApplicable)
        .Select(d => d.MemberName)
        .Order(StringComparer.Ordinal));

    // Nothing half-applied: a pattern that cannot go on a number takes the attribute's format with it too.
    Assert.Equal("""{"$ref":"#/$defs/FormatTarget"}""", Compact(Property(model, "target")));
    var count = Property(model, "count");
    Assert.False(count.ContainsKey("format"));
    Assert.False(count.ContainsKey("pattern"));
  }

  [Fact]
  public void APropertyFormat_BeatsAClashingDataAnnotationsFormat()
  {
    var result = GenerateInvalid();
    var document = result.Documents.Single(d => d.FileName == "misapplied-format.json");
    var model = (JsonObject)JsonNode.Parse(document.Json)!["$defs"]!["MisappliedFormat"]!;

    // [EmailAddress] gives way, as it does to a Guid's own format: the explicit declaration is kept.
    Assert.Equal("""{"type":"string","format":"idn-email"}""", Compact(WithoutDecorators(Property(model, "contact"))));
    Assert.Contains(
      result.Diagnostics,
      d => d.Code == SchemaDiagnosticCodes.DecoratorNotApplicable && d.MemberName == "Contact");
  }
}
