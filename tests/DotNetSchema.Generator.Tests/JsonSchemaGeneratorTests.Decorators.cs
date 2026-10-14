using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using DotNetSchema.Fixtures.Schema;
using DotNetSchema.Fixtures.Schema.Decorators;
using DotNetSchema.Fixtures.Schema.Enums;

namespace DotNetSchema.Generator.Tests;

public sealed partial class JsonSchemaGeneratorTests
{
  private const string Sampler = "DotNetSchema.Fixtures.Schema.Decorators.SampleDecoratorAttribute";
  private const string Tag = "DotNetSchema.Fixtures.Schema.Decorators.SampleTagAttribute";

  /// <summary>
  /// The defaults plus the fixtures' own sampler attributes, which sit outside the default list so that the
  /// school graph can carry them without moving the golden digests.
  /// </summary>
  private static readonly SchemaGenerationOptions WithSampler = SchemaGenerationOptions.Default with
  {
    DecoratorsAllow = ["DotNetSchema.Fixtures.Schema.Decorators.*"],
  };

  // Rendering: one test per kind of argument an attribute can carry.

  [Fact]
  public void EveryPrimitiveArgument_RendersAsJson_KeyedByItsConstructorParameterName()
  {
    var arguments = DecoratorOf(Property(Definition("Person", WithSampler, typeof(School)), "givenName"), Sampler)["arguments"];

    // The minimum of every integer width, which is where a signed or narrowing mistake would show.
    Assert.Equal(
      """{"flag":false,"tiny":-128,"octet":0,"small":-32768,"smallUnsigned":0,"whole":-2147483648,"wholeUnsigned":0,"wide":-9223372036854775808,"wideUnsigned":0,"single":"-Infinity","precise":"NaN","letter":"a","text":null}""",
      Compact(arguments));
  }

  [Fact]
  public void EveryPrimitiveArgument_KeepsItsMaximum_AndAStringNeedingEscapesSurvivesIntact()
  {
    var arguments = DecoratorOf(Property(Definition("Person", WithSampler, typeof(School)), "familyName"), Sampler)["arguments"]!;

    Assert.Equal(127, arguments["tiny"]!.GetValue<int>());
    Assert.Equal(255, arguments["octet"]!.GetValue<int>());
    Assert.Equal(32767, arguments["small"]!.GetValue<int>());
    Assert.Equal(65535, arguments["smallUnsigned"]!.GetValue<int>());
    Assert.Equal(int.MaxValue, arguments["whole"]!.GetValue<int>());
    Assert.Equal(uint.MaxValue, arguments["wholeUnsigned"]!.GetValue<uint>());
    Assert.Equal(long.MaxValue, arguments["wide"]!.GetValue<long>());
    Assert.Equal(ulong.MaxValue, arguments["wideUnsigned"]!.GetValue<ulong>());
    Assert.Equal(float.MaxValue, arguments["single"]!.GetValue<float>());
    Assert.Equal("Infinity", arguments["precise"]!.GetValue<string>());
    Assert.Equal("\"", arguments["letter"]!.GetValue<string>());
    Assert.Equal("quote \" and ✓", arguments["text"]!.GetValue<string>());
  }

  [Theory]
  [InlineData(
    "Book", "isbn",
    """{"type":"System.Collections.Generic.IReadOnlyDictionary<System.String, DotNetSchema.Fixtures.Schema.Book>","shade":"Green","boxed":42}""")]
  [InlineData(
    "Book", "name",
    """{"type":"DotNetSchema.Fixtures.Schema.Decorators.SampleDecoratorAttribute+Nested","shade":5,"boxed":"boxed"}""")]
  [InlineData(
    "Address", "street",
    """{"type":"System.Collections.Generic.List<T>","shade":64,"boxed":"System.String"}""")]
  [InlineData(
    "Address", "postalCode",
    """{"type":"System.Int32[]","shade":"None","boxed":["a","b"]}""")]
  [InlineData(
    "Address", "city",
    """{"numbers":[1,2,3],"words":["x",null],"types":["System.Int32","DotNetSchema.Fixtures.Schema.Address"],"mixed":[1,"two","DotNetSchema.Fixtures.Schema.Book","Green",[3,4],null]}""")]
  [InlineData(
    "Address", "countryCode",
    """{"numbers":[],"words":[],"types":[],"mixed":[]}""")]
  public void TypeEnumBoxedAndArrayArguments_RenderAsDocumented(string definition, string property, string expected)
  {
    // A type by a name free of assembly versions, generic or nested; an enum by member name when exactly
    // one member holds the value, and as a number for a flags combination or an undeclared value; an
    // object parameter by whatever it boxes; arrays element by element.
    var arguments = DecoratorOf(Property(Definition(definition, WithSampler, typeof(School)), property), Sampler)["arguments"];

    // Escaped as the documents are, so a generic's angle brackets and a nested type's '+' read as written.
    Assert.Equal(expected, arguments!.ToJsonString(new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
  }

  [Fact]
  public void NamedArguments_RenderUnderProperties_OrderedByName_AndAnEmptyArgumentListIsOmitted()
  {
    var entry = DecoratorOf(Definition("Campus", WithSampler, typeof(School)), Sampler);

    Assert.Null(entry["arguments"]);
    Assert.Equal(
      """{"Kind":"DotNetSchema.Fixtures.Schema.Campus","Note":"named","Numbers":[7,8],"Shade":"Blue","Weight":0.5}""",
      Compact(entry["properties"]));
  }

  [Fact]
  public void AnAttributeWithoutArguments_IsListedByNameAlone()
  {
    Assert.Equal(Entry(Sampler), Compact(DecoratorOf(Definition("School", WithSampler, typeof(School)), Sampler)));
  }

  [Fact]
  public void RepeatedApplications_AreSortedByName_ThenByTheirRenderedArguments()
  {
    // Metadata promises no order between attributes, so declaration order cannot be what decides this.
    var school = Definition("School", WithSampler, typeof(School));

    Assert.Equal(
      [
        Entry(Sampler),
        Entry(Tag, """{"tag":"alpha","weights":[3,1,2]}"""),
        Entry(Tag, """{"tag":"alpha","weights":[]}"""),
        Entry(Tag, """{"tag":"beta","weights":[]}"""),
        Entry("System.ComponentModel.DescriptionAttribute", """{"description":"A school: the root of the fixture graph."}"""),
      ],
      Decorators(school).Select(Compact));
  }

  [Fact]
  public void AnEnumType_CarriesItsDecoratorsOnItsDefinition()
  {
    var role = Definition("PersonRole", WithSampler, typeof(School));

    Assert.Equal("The capacity in which a person is attached to the school.", role["description"]!.GetValue<string>());
    Assert.Equal([Tag, "System.ComponentModel.DescriptionAttribute"], DecoratorNames(role));
  }

  [Fact]
  public void APropertyHoldingAReference_CarriesItsDecoratorsBesideTheRef()
  {
    var name = Property(Definition("Book", WithSampler, typeof(School)), "name");

    Assert.Equal("#/$defs/BookName", name["$ref"]!.GetValue<string>());
    Assert.Equal([Sampler], DecoratorNames(name));
  }

  [Fact]
  public void APositionalRecord_ReadsAttributesFromThePrimaryConstructorParameter_AndListsADuplicateOnce()
  {
    var bookmark = Definition("Bookmark", WithSampler, typeof(Bookmark));

    // [Range] was written without a target, so C# put it on the parameter rather than the property.
    Assert.Equal(
      """{"type":"integer","minimum":1,"maximum":10}""",
      Compact(WithoutDecorators(Property(bookmark, "rank"))));

    Assert.Equal(
      ["both", "parameter", "property"],
      Decorators(Property(bookmark, "tags")).Select(d => d["arguments"]!["tag"]!.GetValue<string>()));
  }

  // Keyword mapping: the well-known attributes, under the default selection.

  [Theory]
  [InlineData("Person", "givenName", """{"type":"string","minLength":2,"maxLength":32}""")]
  [InlineData("Person", "familyName", """{"type":"string","maxLength":40}""")]
  [InlineData("Person", "email", """{"type":["string","null"],"format":"email"}""")]
  [InlineData("Person", "gradePointAverage", """{"type":["number","null"],"minimum":1,"maximum":5}""")]
  [InlineData("Person", "classes", """{"type":"array","items":{"$ref":"#/$defs/Class"},"maxItems":64}""")]
  [InlineData("Address", "countryCode", """{"type":"string","pattern":"^(?:[A-Z]{2})$"}""")]
  [InlineData("Address", "latitude", """{"type":"number","minimum":-90,"maximum":90}""")]
  [InlineData("Address", "longitude", """{"type":"number","exclusiveMinimum":-180,"maximum":180}""")]
  [InlineData("Book", "isbn", """{"type":["string","null"],"title":"ISBN-13","description":"ISBN-13, formatted with separators."}""")]
  [InlineData("Book", "edition", """{"type":"integer","minimum":1,"maximum":99}""")]
  [InlineData("Book", "pageCount", """{"type":"integer","minimum":1,"maximum":5000}""")]
  [InlineData("Book", "hasDigitalLicence", """{"type":"boolean","default":false}""")]
  [InlineData("Book", "chapters", """{"type":"array","items":{"$ref":"#/$defs/Chapter"},"maxItems":64}""")]
  public void AWellKnownAttribute_MapsToItsStandardKeywords(string definition, string property, string expected)
  {
    // Bounds tighten the type's own rather than replace them blindly: Edition is a byte, PageCount a
    // ushort, and only the attribute's narrower side wins. A [RegularExpression] is anchored, because .NET
    // matches it against the whole string and a JSON Schema pattern is not.
    var schema = Property(Definition(definition, typeof(School)), property);

    Assert.Equal(expected, Compact(WithoutDecorators(schema)));
  }

  [Theory]
  [InlineData("School", "description", "A school: the root of the fixture graph.")]
  [InlineData("Campus", "title", "Campus site")]
  public void ATypeLevelAttribute_AnnotatesTheDefinition(string definition, string keyword, string expected)
  {
    Assert.Equal(expected, Definition(definition, typeof(School))[keyword]!.GetValue<string>());
  }

  [Fact]
  public void UrlObsoleteAndDescription_MapOnAPositionalRecord()
  {
    var bookmark = Definition("Bookmark", typeof(Bookmark));

    Assert.Equal("""{"type":"string","format":"uri"}""", Compact(WithoutDecorators(Property(bookmark, "href"))));
    Assert.Equal(
      """{"type":["string","null"],"description":"A free-form note.","deprecated":true}""",
      Compact(WithoutDecorators(Property(bookmark, "note"))));
  }

  [Fact]
  public void DataAnnotationsRequired_IsListed_ButNeitherRequiresNorNarrowsTheProperty()
  {
    // [Required] is a non-null, non-empty check validation runs after deserialising; JSON Schema's
    // required is a presence check, which only the C# modifier and [JsonRequired] make.
    var bookmark = Definition("Bookmark", typeof(Bookmark));
    var label = Property(bookmark, "label");

    Assert.Equal("""{"type":["string","null"]}""", Compact(WithoutDecorators(label)));
    Assert.Equal(["System.ComponentModel.DataAnnotations.RequiredAttribute"], DecoratorNames(label));
    Assert.DoesNotContain("label", Required(bookmark));
  }

  // Selection: which attributes take part, through the default list, Decorators, Allow and Deny.

  [Fact]
  public void TheDefaultSelection_LeavesTheSamplerOut()
  {
    Assert.DoesNotContain(AllDecoratorNames(Document(typeof(School))), n => n.StartsWith("DotNetSchema.", StringComparison.Ordinal));
  }

  [Fact]
  public void AllowingANamespace_AddsItsAttributes_ToTheDefaults()
  {
    var names = AllDecoratorNames(Document(WithSampler, typeof(School)));

    Assert.Contains(Sampler, names);
    Assert.Contains(Tag, names);
    Assert.Contains("System.ComponentModel.DescriptionAttribute", names);
  }

  [Fact]
  public void AllowingAnExactName_AddsThatAttributeAlone()
  {
    var options = SchemaGenerationOptions.Default with { DecoratorsAllow = [Tag] };
    var names = AllDecoratorNames(Document(options, typeof(School)));

    Assert.Contains(Tag, names);
    Assert.DoesNotContain(Sampler, names);
  }

  [Fact]
  public void ANamespaceWildcard_DoesNotReachIntoSubNamespaces()
  {
    var options = SchemaGenerationOptions.Default with { DecoratorsAllow = ["DotNetSchema.Fixtures.Schema.*"] };

    Assert.DoesNotContain(AllDecoratorNames(Document(options, typeof(School))), n => n.StartsWith("DotNetSchema.", StringComparison.Ordinal));
  }

  [Fact]
  public void DenyingAnAttribute_RemovesBothItsEntry_AndTheKeywordsItMapsTo()
  {
    var options = SchemaGenerationOptions.Default with { DecoratorsDeny = ["System.ObsoleteAttribute"] };
    var note = Property(Definition("Bookmark", options, typeof(Bookmark)), "note");

    Assert.Null(note["deprecated"]);
    Assert.Equal(["System.ComponentModel.DescriptionAttribute"], DecoratorNames(note));
  }

  [Fact]
  public void DenyingOneTypeOutOfAnAllowedNamespace_LeavesTheRestOfItAlone()
  {
    var options = SchemaGenerationOptions.Default with
    {
      DecoratorsDeny = ["System.ComponentModel.DataAnnotations.RangeAttribute"],
    };
    var book = Definition("Book", options, typeof(School));

    // Back to the byte's own bounds, with nothing listed; the [MaxLength] beside it is untouched.
    Assert.Equal("""{"type":"integer","minimum":0,"maximum":255}""", Compact(Property(book, "edition")));
    Assert.Equal(64, Property(book, "chapters")["maxItems"]!.GetValue<int>());
  }

  [Fact]
  public void SettingTheList_ReplacesTheDefaults()
  {
    var options = SchemaGenerationOptions.Default with { Decorators = ["System.ComponentModel.DescriptionAttribute"] };
    var document = Document(options, typeof(School));

    Assert.Equal(["System.ComponentModel.DescriptionAttribute"], AllDecoratorNames(document).Distinct());
    Assert.Equal("""{"type":"integer","minimum":0,"maximum":255}""", Compact(Property(Defs(document, "Book"), "edition")));
    Assert.Equal("""{"type":["string","null"]}""", Compact(Property(Defs(document, "Person"), "email")));
  }

  [Fact]
  public void TheListAllowAndDeny_Combine_DenyWinning()
  {
    var options = SchemaGenerationOptions.Default with
    {
      Decorators = ["System.ComponentModel.DataAnnotations.*"],
      DecoratorsAllow = [Tag],
      DecoratorsDeny = ["System.ComponentModel.DataAnnotations.RangeAttribute"],
    };
    var document = Document(options, typeof(School));
    var school = Defs(document, "School");

    Assert.Equal([Tag, Tag, Tag], DecoratorNames(school));
    Assert.Null(school["description"]);
    Assert.Null(Property(Defs(document, "Book"), "edition")["x-decorators"]);
    Assert.Equal("email", Property(Defs(document, "Person"), "email")["format"]!.GetValue<string>());
  }

  [Fact]
  public void DenyingEverything_SwitchesTheFeatureOff()
  {
    var off = Generate(SchemaGenerationOptions.Default with { DecoratorsDeny = ["*"] }, typeof(School), typeof(Bookmark));
    var empty = Generate(SchemaGenerationOptions.Default with { Decorators = [] }, typeof(School), typeof(Bookmark));

    var json = Assert.Single(off.Documents).Json;
    Assert.DoesNotContain("x-decorators", json, StringComparison.Ordinal);
    Assert.DoesNotContain("\"description\"", json, StringComparison.Ordinal);
    Assert.DoesNotContain("\"deprecated\"", json, StringComparison.Ordinal);
    Assert.Equal(Assert.Single(empty.Documents).Json, json);
  }

  [Fact]
  public void AllowingEverything_StillNeverExposesCompilerOrSerialiserAttributes()
  {
    // The school graph carries nullable annotations, required members and a [JsonConverter], all of which
    // the schema already reflects in its own terms, plus the marker itself.
    var names = AllDecoratorNames(Document(SchemaGenerationOptions.Default with { DecoratorsAllow = ["*"] }, typeof(School)));

    Assert.NotEmpty(names);
    Assert.DoesNotContain(names, n =>
      n.StartsWith("System.Runtime.CompilerServices.", StringComparison.Ordinal) ||
      n.StartsWith("System.Text.Json.Serialization.", StringComparison.Ordinal) ||
      n == "DotNetSchema.DotNetSchemaAttribute");
  }

  /// <summary>An <c>x-decorators</c> entry as compact JSON.</summary>
  private static string Entry(string name, string? arguments = null) =>
    arguments is null ? $"{{\"name\":\"{name}\"}}" : $"{{\"name\":\"{name}\",\"arguments\":{arguments}}}";

  private static JsonObject Defs(JsonObject document, string name) => (JsonObject)document["$defs"]![name]!;

  private static IReadOnlyList<JsonObject> Decorators(JsonObject schema) =>
    schema["x-decorators"] is JsonArray array ? [.. array.Select(n => (JsonObject)n!)] : [];

  private static IReadOnlyList<string> DecoratorNames(JsonObject schema) =>
    [.. Decorators(schema).Select(d => d["name"]!.GetValue<string>())];

  private static JsonObject DecoratorOf(JsonObject schema, string name) =>
    Assert.Single(Decorators(schema), d => d["name"]!.GetValue<string>() == name);

  /// <summary>The schema with its <c>x-decorators</c> taken off, leaving only what the mapping produced.</summary>
  private static JsonObject WithoutDecorators(JsonObject schema)
  {
    var copy = (JsonObject)schema.DeepClone();
    copy.Remove("x-decorators");
    return copy;
  }

  /// <summary>Every decorator name anywhere in a document, definitions and properties alike.</summary>
  private static IReadOnlyList<string> AllDecoratorNames(JsonObject document)
  {
    List<string> names = [];
    foreach (var (_, definition) in document["$defs"]!.AsObject())
    {
      var model = (JsonObject)definition!;
      names.AddRange(DecoratorNames(model));
      if (model["properties"] is JsonObject properties)
      {
        foreach (var (_, property) in properties)
        {
          names.AddRange(DecoratorNames((JsonObject)property!));
        }
      }
    }

    return names;
  }
}
