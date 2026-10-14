using System.Text.Json.Nodes;
using DotNetSchema.Fixtures.Schema;
using DotNetSchema.Generator.Diagnostics;

namespace DotNetSchema.Generator.Tests;

public sealed partial class JsonSchemaGeneratorTests
{
  /// <summary>
  /// The whole scalar table, one case per portable primitive, read off <see cref="ScalarBattery" /> —
  /// which exists precisely because it carries exactly one property of each.
  /// </summary>
  [Theory]
  [InlineData("isProctored", """{"type":"boolean"}""")]
  [InlineData("curveAdjustment", """{"type":"integer","minimum":-128,"maximum":127}""")]
  [InlineData("rawScore", """{"type":"integer","minimum":0,"maximum":255}""")]
  [InlineData("seatNumber", """{"type":"integer","minimum":-32768,"maximum":32767}""")]
  [InlineData("itemCount", """{"type":"integer","minimum":0,"maximum":65535}""")]
  [InlineData("totalPoints", """{"type":"integer","minimum":-2147483648,"maximum":2147483647}""")]
  [InlineData("submissionSequence", """{"type":"integer","minimum":0,"maximum":4294967295}""")]
  [InlineData("percentileRank", """{"type":"number"}""")]
  [InlineData("scaledScore", """{"type":"number"}""")]
  [InlineData("letterGrade", """{"type":"string","minLength":1,"maxLength":1}""")]
  [InlineData("remarks", """{"type":"string"}""")]
  [InlineData("responseId", """{"type":"string","format":"uuid"}""")]
  [InlineData("administeredOn", """{"type":"string","format":"date"}""")]
  [InlineData("submittedAt", """{"type":"string","format":"date-time"}""")]
  public void EveryPortableScalar_MapsToItsDocumentedSchema(string property, string expected)
  {
    var battery = Definition("ScalarBattery", typeof(ScalarBattery));

    Assert.Equal(expected, Compact(Property(battery, property)));
  }

  [Fact]
  public void Int64_OmitsItsBounds_BecauseTheyAreNotExactlyRepresentableAsADouble()
  {
    var battery = Definition("ScalarBattery", typeof(ScalarBattery));
    var elapsed = Property(battery, "elapsedTicks");

    Assert.Equal("integer", elapsed["type"]!.GetValue<string>());
    Assert.Null(elapsed["minimum"]);
    Assert.Null(elapsed["maximum"]);
    Assert.Contains("2^53", elapsed["$comment"]!.GetValue<string>());
  }

  [Fact]
  public void UInt64_KeepsItsZeroLowerBound_ButNotItsUpperOne()
  {
    var checksum = Property(Definition("ScalarBattery", typeof(ScalarBattery)), "integrityChecksum");

    Assert.Equal(0, checksum["minimum"]!.GetValue<int>());
    Assert.Null(checksum["maximum"]);
  }

  [Fact]
  public void TimeSpan_IsAPattern_BecauseItIsNotAnIsoDuration()
  {
    var duration = Property(Definition("ScalarBattery", typeof(ScalarBattery)), "allowedDuration");

    Assert.Equal("string", duration["type"]!.GetValue<string>());
    Assert.Null(duration["format"]);
    Assert.Equal(@"^-?(\d+\.)?\d{2}:\d{2}:\d{2}(\.\d{1,7})?$", duration["pattern"]!.GetValue<string>());
  }

  [Fact]
  public void TimeOnly_IsAPattern_BecauseItCarriesNoUtcOffset()
  {
    var startedAt = Property(Definition("ScalarBattery", typeof(ScalarBattery)), "startedAt");

    Assert.Equal("string", startedAt["type"]!.GetValue<string>());
    Assert.Null(startedAt["format"]);
    Assert.Contains("full-time", startedAt["$comment"]!.GetValue<string>());
  }

  [Fact]
  public void Decimal_IsANumber_AndSaysWhatThatCosts()
  {
    var weighted = Property(Definition("ScalarBattery", typeof(ScalarBattery)), "weightedAverage");

    Assert.Equal("number", weighted["type"]!.GetValue<string>());
    Assert.Contains("scale preserved", weighted["$comment"]!.GetValue<string>());
  }

  [Fact]
  public void ACollectionOfScalars_BecomesAnArrayOfThem()
  {
    var series = Definition("ScalarSeries", typeof(ScalarSeries));

    Assert.Equal(
      """{"type":"array","items":{"type":"string","format":"uuid"}}""",
      Compact(Property(series, "responseIds")));
  }

  [Fact]
  public void AByteArray_IsBase64_ButAListOfBytesIsAnArrayOfIntegers()
  {
    // System.Text.Json writes byte[] as a base64 string, but IReadOnlyList<byte> as a number array. The two
    // must not be confused either way: contentEncoding on a list would describe output the serialiser does
    // not produce, and an array schema for byte[] would reject what it does.
    var series = Definition("ScalarSeries", typeof(ScalarSeries));

    Assert.Equal(
      """{"type":"string","contentEncoding":"base64"}""",
      Compact(Property(series, "answerSheetScan")));
    Assert.Equal(
      """{"type":"array","items":{"type":"integer","minimum":0,"maximum":255}}""",
      Compact(Property(series, "rawScores")));
  }

  [Fact]
  public void AStringKeyedMap_OmitsPropertyNames_BecauseItConstrainsNothing()
  {
    var register = Definition("ScalarRegister", typeof(ScalarRegister));
    var remarks = Property(register, "remarkByItem");

    Assert.Null(remarks["propertyNames"]);
    Assert.Equal("""{"type":"object","additionalProperties":{"type":"string"}}""", Compact(remarks));
  }

  [Theory]
  [InlineData("remarkByItemNumber", """{"type":"string","pattern":"^-?(0|[1-9]\\d*)$"}""")]
  [InlineData("scoreByResponseId", """{"type":"string","format":"uuid"}""")]
  [InlineData("attendanceByDate", """{"type":"string","format":"date"}""")]
  public void ANonStringKey_ConstrainsThePropertyNamesAsAString(string property, string expected)
  {
    var register = Definition("ScalarRegister", typeof(ScalarRegister));

    Assert.Equal(expected, Compact(Property(register, property)["propertyNames"]));
  }

  /// <summary>Options without comments, so a mapped schema can be asserted as one short literal.</summary>
  private static SchemaGenerationOptions Uncommented => SchemaGenerationOptions.Default with { EmitComments = false };

  private const string StudentNumberName = "DotNetSchema.Fixtures.Schema.StudentNumber";
  private const string ClassNameName = "DotNetSchema.Fixtures.Schema.Enums.ClassName";

  [Fact]
  public void ADeclaredType_IsDescribedInline_AndGetsNoDefinition()
  {
    var document = Document(Uncommented, typeof(School));
    var person = (JsonObject)document["$defs"]!["Person"]!;

    // Nullable through the type array, like an inline scalar, not through the anyOf a $ref would need.
    Assert.Equal("""{"type":["string","null"],"pattern":"^S\\d{7}$"}""", Compact(Property(person, "studentNumber")));
    Assert.Null(document["$defs"]!["StudentNumber"]);
  }

  [Fact]
  public void AGenericDeclaration_CoversEveryConstruction_AndRescuesItFromBeingAGenericModel()
  {
    var result = Generate(Uncommented, typeof(School));
    var document = (JsonObject)JsonNode.Parse(Assert.Single(result.Documents).Json)!;

    Assert.DoesNotContain(result.Diagnostics, d => d.Code == SchemaDiagnosticCodes.GenericModel);
    Assert.Equal(
      """{"type":"string","format":"uuid"}""",
      Compact(Property((JsonObject)document["$defs"]!["Book"]!, "catalogueId")));
    Assert.DoesNotContain(Assert.Single(result.Documents).DefinitionNames, n => n.Contains("Catalogue", StringComparison.Ordinal));
  }

  [Fact]
  public void AConfiguredMapping_BeatsTheTypesOwnDeclaration()
  {
    var options = Uncommented with { TypeMappings = [new TypeMapping(StudentNumberName, "integer")] };

    var person = Definition("Person", options, typeof(School));

    Assert.Equal("""{"type":["integer","null"]}""", Compact(Property(person, "studentNumber")));
  }

  [Fact]
  public void AConfiguredMapping_CanReplaceABuiltInScalar()
  {
    // The case of an application writing 64-bit integers as strings for JavaScript readers.
    var options = Uncommented with { TypeMappings = [new TypeMapping("System.Int64", "string", Pattern: "^-?\\d{1,20}$")] };

    var battery = Definition("ScalarBattery", options, typeof(ScalarBattery));

    Assert.Equal("""{"type":"string","pattern":"^-?\\d{1,20}$"}""", Compact(Property(battery, "elapsedTicks")));
  }

  [Fact]
  public void AMappedType_SaysInItsCommentWhereTheDeclarationCameFrom()
  {
    var declared = Property(Definition("Person", typeof(School)), "studentNumber")["$comment"]!.GetValue<string>();
    var configured = Property(
        Definition(
          "Person",
          SchemaGenerationOptions.Default with { TypeMappings = [new TypeMapping(StudentNumberName, "string")] },
          typeof(School)),
        "studentNumber")["$comment"]!.GetValue<string>();

    Assert.Contains("[DotNetSchemaType]", declared, StringComparison.Ordinal);
    Assert.Contains("a DotNetSchemaType item", configured, StringComparison.Ordinal);
  }

  [Fact]
  public void AStringMappedKey_ConstrainsThePropertyNamesWithItsDeclaration()
  {
    var options = Uncommented with { TypeMappings = [new TypeMapping(ClassNameName, "string", Pattern: "^[A-Z]")] };

    var grades = Property(Definition("Person", options, typeof(School)), "grades");

    Assert.Equal("""{"type":"string","pattern":"^[A-Z]"}""", Compact(grades["propertyNames"]));
  }

  [Fact]
  public void ANonStringMappedKey_LeavesTheKeyAsItWouldHaveBeen()
  {
    // Every key is written as a string, so an integer mapping speaks only for the value form.
    var options = Uncommented with { TypeMappings = [new TypeMapping(ClassNameName, "integer")] };
    var person = Definition("Person", options, typeof(School));

    Assert.Equal(
      Compact(Property(Definition("Person", Uncommented, typeof(School)), "grades")["propertyNames"]),
      Compact(Property(person, "grades")["propertyNames"]));
  }

  // [DotNetSchemaFormat]: a format and/or pattern declared on one property, never changing its type.

  [Theory]
  [InlineData("Book", "isbn", """{"type":["string","null"],"format":"isbn","pattern":"^978-\\d-\\d{3}-\\d{5}-\\d$"}""")]
  [InlineData("Book", "price", """{"type":"number","format":"decimal"}""")]
  [InlineData("Department", "costCentre", """{"type":"string","pattern":"^(?:CC-\\d{3})?$"}""")]
  public void APropertyFormat_AddsItsKeywords_BesideTheTypeItLeavesAlone(string definition, string property, string expected)
  {
    // Isbn is nullable: the keywords are type-conditional, so they sit beside the type array, not in an anyOf.
    var schema = Property(Definition(definition, Uncommented, typeof(School)), property);
    schema.Remove("title");
    schema.Remove("description");
    schema.Remove("x-decorators");

    Assert.Equal(expected, Compact(schema));
  }

  [Fact]
  public void APropertyFormat_ReplacesTheFormatTheTypeHasOfItsOwn()
  {
    var anchor = Property(Definition("Bookmark", typeof(DotNetSchema.Fixtures.Schema.Decorators.Bookmark)), "anchor");

    Assert.Equal("""{"type":"string","format":"uuid-v7"}""", Compact(anchor));
  }

  [Fact]
  public void APropertyFormatOnACollection_ConstrainsItsElements_AndIsReadFromAPositionalParameter()
  {
    // Written without a property: target, so C# put it on the primary-constructor parameter.
    var tags = Property(Definition("Bookmark", typeof(DotNetSchema.Fixtures.Schema.Decorators.Bookmark)), "tags");

    Assert.Equal("""{"type":"string","pattern":"^[a-z][a-z0-9-]*$"}""", Compact(tags["items"]));
    Assert.False(tags.ContainsKey("pattern"));
  }
}
