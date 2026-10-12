using System.Text.Json.Nodes;
using DotNetSchema.Fixtures.Schema;

namespace DotNetSchema.Generator.Tests;

/// <remarks>
/// Whether a property is <c>required</c> and whether it accepts <c>null</c> are separate questions, and the
/// fixtures answer them in all four combinations — for scalars, arrays, maps, enums and model references
/// alike — so that neither can quietly be derived from the other. Every property of the school graph not
/// listed below is required and never null. The three nullable scalar twins are checked by rule instead of by
/// list, because they are laid out by rule.
/// </remarks>
public sealed partial class JsonSchemaGeneratorTests
{
  /// <summary>Required properties that accept <c>null</c>: <c>x: T | null</c>.</summary>
  public static TheoryData<string, string> RequiredNullableProperties => new()
  {
    // scalars
    { "Campus", "openedOn" },
    { "Building", "builtInYear" },
    { "Department", "annualBudget" },
    { "Person", "gradePointAverage" },
    { "Library", "closesAt" },
    { "Book", "isbn" },
    { "Section", "heading" },
    { "Assessment", "dueAt" },

    // model references, an array, a map and an enum
    { "Department", "head" },
    { "Class", "professor" },
    { "Book", "authors" },
    { "Library", "catalogue" },
    { "Room", "kind" },
  };

  /// <summary>Optional properties that accept <c>null</c>: <c>x?: T | null</c>.</summary>
  public static TheoryData<string, string> OptionalNullableProperties => new()
  {
    // scalars
    { "School", "motto" },
    { "Address", "postalCode" },
    { "Person", "email" },
    { "Class", "synopsis" },
    { "Chapter", "estimatedReadingTime" },
    { "Exercise", "difficulty" },

    // model references
    { "School", "library" },
    { "Person", "mentor" },
    { "Person", "office" },
    { "Class", "room" },
    { "ClassSession", "room" },
    { "Book", "class" },
    { "Assessment", "class" },

    // an array, a map and an enum
    { "Class", "schedule" },
    { "Person", "contactMethods" },
    { "Class", "term" },
  };

  /// <summary>
  /// Optional properties that never accept <c>null</c>: <c>x?: T</c>. There is no model reference among
  /// them: a record with required members cannot be default-initialised, so it has no such C# form.
  /// </summary>
  public static TheoryData<string, string> OptionalNonNullableProperties => new()
  {
    // scalars
    { "School", "enrollmentCap" },
    { "Campus", "areaHectares" },
    { "Building", "isAccessible" },
    { "Room", "seats" },
    { "Department", "costCentre" },
    { "ClassSession", "isRemote" },
    { "Library", "volumeCount" },
    { "Book", "hasDigitalLicence" },
    { "Section", "wordCount" },
    { "Exercise", "hasWorkedSolution" },
    { "Assessment", "isOpenBook" },

    // arrays, a map and an enum
    { "Person", "advisees" },
    { "Class", "prerequisites" },
    { "Chapter", "subChapters" },
    { "Person", "grades" },
    { "Assessment", "kind" },
  };

  /// <summary>The nullable twins, with the plain bundle each mirrors and the keyword its elements sit under.</summary>
  private static readonly (string Twin, string Plain, string? Elements)[] NullableTwins =
  [
    ("NullableScalarBattery", "ScalarBattery", null),
    ("NullableScalarSeries", "ScalarSeries", "items"),
    ("NullableScalarRegister", "ScalarRegister", "additionalProperties"),
  ];

  [Theory]
  [MemberData(nameof(RequiredNullableProperties))]
  public void ARequiredNullableProperty_AcceptsNull_AndIsStillRequired(string definition, string property)
  {
    var model = Definition(definition, typeof(School));

    // The key must be present, but its value may be null: the C# required modifier says nothing about null.
    AssertAcceptsNull(Property(model, property));
    Assert.Contains(property, Required(model));
  }

  [Theory]
  [MemberData(nameof(OptionalNullableProperties))]
  public void AnOptionalNullableProperty_AcceptsNull_AndIsNotRequired(string definition, string property)
  {
    var model = Definition(definition, typeof(School));

    AssertAcceptsNull(Property(model, property));
    Assert.DoesNotContain(property, Required(model));
  }

  [Theory]
  [MemberData(nameof(OptionalNonNullableProperties))]
  public void AnOptionalNonNullableProperty_IsNotRequired_AndDoesNotAcceptNull(string definition, string property)
  {
    var model = Definition(definition, typeof(School));

    // An initialised property may be left out of the input, but when it is there it is never null.
    Assert.False(AcceptsNull(Property(model, property)));
    Assert.DoesNotContain(property, Required(model));
  }

  [Fact]
  public void NoOtherPropertyInTheSchema_IsNullable()
  {
    var expected = Rows(RequiredNullableProperties, OptionalNullableProperties);

    var found = SchoolGraphProperties()
      .Where(p => AcceptsNull(p.Schema))
      .Select(p => p.Path);

    Assert.Equal(expected.Order(StringComparer.Ordinal), found.Order(StringComparer.Ordinal));
  }

  [Fact]
  public void NoOtherPropertyInTheSchema_IsOptional()
  {
    var expected = Rows(OptionalNullableProperties, OptionalNonNullableProperties);

    var found = SchoolGraphProperties()
      .Where(p => !p.Required)
      .Select(p => p.Path);

    Assert.Equal(expected.Order(StringComparer.Ordinal), found.Order(StringComparer.Ordinal));
  }

  [Fact]
  public void EveryRequiredMember_IsListedAsRequired()
  {
    var person = Definition("Person", typeof(School));

    Assert.Equal(
      ["id", "givenName", "familyName", "role", "bornOn", "isActive", "gradePointAverage", "classes"],
      Required(person));
  }

  [Fact]
  public void EachNullableTwin_HasExactlyThePropertiesOfThePlainBundleItMirrors()
  {
    var definitions = Document(typeof(School))["$defs"]!;

    foreach (var (twin, plain, _) in NullableTwins)
    {
      Assert.Equal(
        definitions[plain]!["properties"]!.AsObject().Select(p => p.Key),
        definitions[twin]!["properties"]!.AsObject().Select(p => p.Key));
    }
  }

  [Fact]
  public void EveryNullableScalarBatteryProperty_AcceptsNull_AndEveryOtherOneIsRequired()
  {
    var battery = Definition("NullableScalarBattery", typeof(School));
    var properties = battery["properties"]!.AsObject().ToList();

    Assert.All(properties, p => AssertAcceptsNull(p.Value!.AsObject()));
    Assert.Equal(properties.Where((_, i) => i % 2 == 0).Select(p => p.Key), Required(battery));
  }

  [Fact]
  public void EveryNullableScalarSeriesAndRegister_HasNullableElements_AndCyclesThroughTheFourCombinations()
  {
    foreach (var (twin, _, elements) in NullableTwins.Where(t => t.Elements is not null))
    {
      var definition = Definition(twin, typeof(School));
      var required = Required(definition);

      // By ordinal: required, optional, required but nullable, optional and nullable — then round again.
      foreach (var (property, i) in definition["properties"]!.AsObject().Select((p, i) => (p, i)))
      {
        var schema = property.Value!.AsObject();
        AssertAcceptsNull(schema[elements!]!.AsObject());
        Assert.Equal(i % 4 is 0 or 2, required.Contains(property.Key));
        Assert.Equal(i % 4 is 2 or 3, AcceptsNull(schema));
      }
    }
  }

  /// <summary>
  /// Whether a schema admits <c>null</c> at all, in either of the two encodings the generator uses: an
  /// <c>anyOf</c> with a null branch, or a <c>type</c> array containing <c>"null"</c>.
  /// </summary>
  private static bool AcceptsNull(JsonObject schema) =>
    schema["anyOf"] is not null ||
    (schema["type"] is JsonArray types && types.Any(t => t!.GetValue<string>() == "null"));

  /// <summary>
  /// Asserts that a schema admits <c>null</c>, and that it does so in the right encoding: a <c>$ref</c>
  /// (model or enum) through <c>anyOf</c>, since Draft 2019-09 a <c>$ref</c> no longer suppresses its
  /// siblings and <c>{"$ref": ..., "type": "null"}</c> would demand both at once; anything else through a
  /// two-element <c>type</c> array ending in <c>"null"</c>.
  /// </summary>
  private static void AssertAcceptsNull(JsonObject schema)
  {
    if (schema["anyOf"] is JsonArray alternatives)
    {
      Assert.Equal(2, alternatives.Count);
      Assert.NotNull(alternatives[0]!["$ref"]);
      Assert.Equal("""{"type":"null"}""", Compact(alternatives[1]));
      return;
    }

    Assert.Null(schema["$ref"]);
    var types = Assert.IsType<JsonArray>(schema["type"]);
    Assert.Equal(2, types.Count);
    Assert.NotEqual("null", types[0]!.GetValue<string>());
    Assert.Equal("null", types[1]!.GetValue<string>());
  }

  private static IEnumerable<string> Rows(params TheoryData<string, string>[] tables) =>
    tables.SelectMany(table => table.Select(row => $"{row.Data.Item1}.{row.Data.Item2}"));

  /// <summary>
  /// Every property of every model in the school document, by <c>Definition.property</c>, leaving out the
  /// nullable twins, which are checked by rule.
  /// </summary>
  private static IEnumerable<(string Path, JsonObject Schema, bool Required)> SchoolGraphProperties()
  {
    foreach (var (name, definition) in Document(typeof(School))["$defs"]!.AsObject())
    {
      if (definition!["properties"] is not JsonObject properties || NullableTwins.Any(t => t.Twin == name))
      {
        continue;
      }

      var required = Required(definition.AsObject());
      foreach (var (key, schema) in properties)
      {
        yield return ($"{name}.{key}", schema!.AsObject(), required.Contains(key));
      }
    }
  }
}
