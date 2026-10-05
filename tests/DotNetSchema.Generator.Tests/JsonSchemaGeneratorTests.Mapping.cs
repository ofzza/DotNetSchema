using DotNetSchema.Fixtures.Schema;

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
  public void AListOfBytes_IsAnArrayOfIntegers_NotBase64()
  {
    // IReadOnlyList<byte> is not byte[], so System.Text.Json writes a number array. AnswerSheetScan is
    // semantically a binary blob, but nothing in the metadata says so, and inventing contentEncoding here
    // would describe output the serialiser does not produce.
    var series = Definition("ScalarSeries", typeof(ScalarSeries));

    Assert.Equal(Compact(Property(series, "rawScores")), Compact(Property(series, "answerSheetScan")));
    Assert.Equal("array", Property(series, "answerSheetScan")["type"]!.GetValue<string>());
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
}
