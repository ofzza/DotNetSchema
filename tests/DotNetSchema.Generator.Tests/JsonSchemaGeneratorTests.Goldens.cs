using DotNetSchema.Fixtures.Schema;

namespace DotNetSchema.Generator.Tests;

public sealed partial class JsonSchemaGeneratorTests
{
  /// <summary>
  /// Whole documents, pinned by digest. A digest tells you something moved; the literal definitions below
  /// tell you what. Regenerate both together, and only ever against a change you meant to make — see the
  /// project README for how.
  /// </summary>
  [Theory]
  [InlineData("assessment.json", "b48db81b6acc43a61909b3f6e6ca5c8c3ff16b4fe1b232c9d4739e23b968dedb")]
  [InlineData("schema.json", "f81e546f34fd24b8063db573618f11627642a86a022b372eb1da75ad1b58b4c0")]
  [InlineData("school.json", "29b17c4d2264b44b00f4a1bb43a3700e4305637be8ce7de6c929e6da16e64609")]
  public void EachDocument_ReproducesItsGoldenDigest(string fileName, string digest)
  {
    var document = new JsonSchemaGenerator()
      .Generate([FixturesAssembly])
      .Documents
      .Single(d => d.FileName == fileName);

    Assert.Equal(digest, SchemaDigest.Of(document));
  }

  /// <summary>
  /// The mapping table's cleanest witness: <see cref="ScalarRegister" /> is pure scalars and maps, so its
  /// definition is the scalar and key tables written out with nothing else in the way. Trimmed to the
  /// first seven properties so the literal stays reviewable; the digest above covers the other seventeen.
  /// </summary>
  [Fact]
  public void ScalarRegister_ReproducesItsGoldenDefinition()
  {
    var register = Definition("ScalarRegister", typeof(ScalarRegister));

    // The literal takes its line endings from the checkout of this file, which .gitattributes pins to LF;
    // normalising here as well keeps the test honest on a checkout that ignores it (core.autocrlf=true).
    Assert.Equal(
      """
      {
        "type": "object",
        "properties": {
          "answeredByItem": {
            "type": "object",
            "additionalProperties": {
              "type": "boolean"
            }
          },
          "curveByItem": {
            "type": "object",
            "additionalProperties": {
              "type": "integer",
              "minimum": -128,
              "maximum": 127
            }
          },
          "rawScoreByItem": {
            "type": "object",
            "additionalProperties": {
              "type": "integer",
              "minimum": 0,
              "maximum": 255
            }
          },
          "seatByItem": {
            "type": "object",
            "additionalProperties": {
              "type": "integer",
              "minimum": -32768,
              "maximum": 32767
            }
          },
          "attemptsByItem": {
            "type": "object",
            "additionalProperties": {
              "type": "integer",
              "minimum": 0,
              "maximum": 65535
            }
          },
          "pointsByItem": {
            "type": "object",
            "additionalProperties": {
              "type": "integer",
              "minimum": -2147483648,
              "maximum": 2147483647
            }
          },
          "sequenceByItem": {
            "type": "object",
            "additionalProperties": {
              "type": "integer",
              "minimum": 0,
              "maximum": 4294967295
            }
          }
        }
      }
      """.ReplaceLineEndings("\n"),
      Pretty(Trim(register, 7)));
  }

  /// <summary>
  /// <see cref="Person" /> is where the interesting cases meet: a recursive reference, a nullable one, an
  /// enum as a value and the same kind of enum as a map key.
  /// </summary>
  [Fact]
  public void Person_ReproducesItsGoldenSalientProperties()
  {
    var person = Definition("Person", typeof(School));

    Assert.Equal("""{"$ref":"#/$defs/PersonRole"}""", Compact(Property(person, "role")));
    Assert.Equal(
      """{"anyOf":[{"$ref":"#/$defs/Person"},{"type":"null"}]}""",
      Compact(Property(person, "mentor")));
    Assert.Equal(
      """{"type":"array","items":{"$ref":"#/$defs/Person"}}""",
      Compact(Property(person, "advisees")));
    Assert.Equal(
      """{"type":["object","null"],"additionalProperties":{"type":"string"}}""",
      Compact(Property(person, "contactMethods")));
  }

  [Fact]
  public void ANumericEnum_CarriesItsMemberNamesAsAnnotations()
  {
    var role = Definition("PersonRole", typeof(School));

    Assert.Equal("integer", role["type"]!.GetValue<string>());
    Assert.Equal(
      """[{"const":0,"title":"Student"},{"const":1,"title":"Professor"}]""",
      Compact(Trim(role, 2, "oneOf")));
  }

  [Fact]
  public void AStringEnum_CarriesItsCSharpNamesAsTitles_EvenWhereItsValueIsRenamed()
  {
    // The title is the C# name, not the value again: only the title says "spiral-bound" is SpiralBound.
    Assert.Equal(
      """{"type":"string","oneOf":[{"const":"Hardcover","title":"Hardcover"},{"const":"Paperback","title":"Paperback"},{"const":"spiral-bound","title":"SpiralBound"}]}""",
      Compact(Definition("BookBinding", typeof(School))));
  }

  [Fact]
  public void UnderEnumWithNames_AnEnumListsItsCSharpNamesAlongsideItsValues()
  {
    var options = SchemaGenerationOptions.Default with { EnumStyle = EnumStyle.EnumWithNames };
    var role = Definition("PersonRole", options, typeof(School));

    Assert.Equal("integer", role["type"]!.GetValue<string>());
    Assert.Equal("[0,1]", Compact(Trim(role, 2, "enum")));
    Assert.Equal("""["Student","Professor"]""", Compact(Trim(role, 2, "x-enumNames")));
    Assert.False(role.ContainsKey("oneOf"));

    Assert.Equal(
      """{"type":"string","enum":["Hardcover","Paperback","spiral-bound"],"x-enumNames":["Hardcover","Paperback","SpiralBound"]}""",
      Compact(Definition("BookBinding", options, typeof(School))));
  }

  [Fact]
  public void UnderEnum_AnEnumLosesItsNames()
  {
    var options = SchemaGenerationOptions.Default with { EnumStyle = EnumStyle.Enum };

    Assert.Equal(
      """{"type":"string","enum":["Hardcover","Paperback","spiral-bound"]}""",
      Compact(Definition("BookBinding", options, typeof(School))));
    Assert.Equal("[0,1]", Compact(Trim(Definition("PersonRole", options, typeof(School)), 2, "enum")));
  }

  /// <summary>Indented JSON, matching the raw string literals the goldens are written as.</summary>
  private static string Pretty(System.Text.Json.Nodes.JsonNode node) =>
    node.ToJsonString(new System.Text.Json.JsonSerializerOptions
    {
      WriteIndented = true,
      IndentSize = 2,
      NewLine = "\n",
    });

  /// <summary>Keeps a golden literal to a reviewable length by taking only the first few entries.</summary>
  private static System.Text.Json.Nodes.JsonNode Trim(
    System.Text.Json.Nodes.JsonObject definition,
    int take,
    string? array = null)
  {
    if (array is not null)
    {
      return new System.Text.Json.Nodes.JsonArray(
        [.. definition[array]!.AsArray().Take(take).Select(n => n!.DeepClone())]);
    }

    var properties = new System.Text.Json.Nodes.JsonObject();
    foreach (var (name, schema) in definition["properties"]!.AsObject().Take(take))
    {
      properties[name] = schema!.DeepClone();
    }

    return new System.Text.Json.Nodes.JsonObject
    {
      ["type"] = definition["type"]!.DeepClone(),
      ["properties"] = properties,
    };
  }
}
