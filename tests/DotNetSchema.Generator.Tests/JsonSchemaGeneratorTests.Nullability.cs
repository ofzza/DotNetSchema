using System.Text.Json.Nodes;
using DotNetSchema.Fixtures.Schema;

namespace DotNetSchema.Generator.Tests;

/// <remarks>
/// Whether a property is <c>required</c> and whether it accepts <c>null</c> are separate questions, and the
/// fixtures answer them in all four combinations so that neither can quietly be derived from the other.
/// Every property not listed below is required and never null.
/// </remarks>
public sealed partial class JsonSchemaGeneratorTests
{
  /// <summary>Nullable model references that are not <c>required</c>: <c>x?: T | null</c>.</summary>
  public static TheoryData<string, string> OptionalNullableReferences => new()
  {
    { "School", "library" },
    { "Department", "head" },
    { "Person", "mentor" },
    { "Person", "office" },
    { "Class", "professor" },
    { "Class", "room" },
    { "ClassSession", "room" },
    { "Book", "class" },
    { "Assessment", "class" },
  };

  /// <summary>Nullable inline scalars that are not <c>required</c>: <c>x?: T | null</c>.</summary>
  public static TheoryData<string, string> OptionalNullableScalars => new()
  {
    { "School", "motto" },
    { "Address", "postalCode" },
    { "Person", "email" },
    { "Class", "synopsis" },
    { "Chapter", "estimatedReadingTime" },
    { "Exercise", "difficulty" },
  };

  /// <summary>Nullable inline scalars that are <c>required</c>: <c>x: T | null</c>.</summary>
  public static TheoryData<string, string> RequiredNullableScalars => new()
  {
    { "Campus", "openedOn" },
    { "Building", "builtInYear" },
    { "Department", "annualBudget" },
    { "Person", "gradePointAverage" },
    { "Library", "closesAt" },
    { "Book", "isbn" },
    { "Section", "heading" },
    { "Assessment", "dueAt" },
  };

  /// <summary>Non-nullable properties that are not <c>required</c>: <c>x?: T</c>.</summary>
  public static TheoryData<string, string> OptionalNonNullableProperties => new()
  {
    { "School", "enrollmentCap" },
    { "Campus", "areaHectares" },
    { "Building", "isAccessible" },
    { "Room", "seats" },
    { "Department", "costCentre" },
    { "Person", "advisees" },
    { "Class", "prerequisites" },
    { "ClassSession", "isRemote" },
    { "Library", "volumeCount" },
    { "Book", "hasDigitalLicence" },
    { "Chapter", "subChapters" },
    { "Section", "wordCount" },
    { "Exercise", "hasWorkedSolution" },
    { "Assessment", "isOpenBook" },
  };

  [Theory]
  [MemberData(nameof(OptionalNullableReferences))]
  public void ANullableReference_IsWrappedInAnyOf_AndIsNotRequired(string definition, string property)
  {
    var model = Definition(definition, typeof(School));
    var schema = Property(model, property);

    // anyOf rather than a sibling "type": since Draft 2019-09 a $ref no longer suppresses its siblings,
    // so {"$ref": ..., "type": "null"} would demand both at once and nothing could satisfy it.
    var alternatives = Assert.IsType<JsonArray>(schema["anyOf"]);
    Assert.Equal(2, alternatives.Count);
    Assert.NotNull(alternatives[0]!["$ref"]);
    Assert.Equal("null", alternatives[1]!["type"]!.GetValue<string>());

    Assert.DoesNotContain(property, Required(model));
  }

  [Theory]
  [MemberData(nameof(OptionalNullableScalars))]
  public void AnOptionalNullableScalar_WidensItsTypeWithNull_AndIsNotRequired(string definition, string property)
  {
    var model = Definition(definition, typeof(School));

    AssertTypeWidenedWithNull(Property(model, property));
    Assert.DoesNotContain(property, Required(model));
  }

  [Theory]
  [MemberData(nameof(RequiredNullableScalars))]
  public void ARequiredNullableScalar_WidensItsTypeWithNull_AndIsStillRequired(string definition, string property)
  {
    var model = Definition(definition, typeof(School));

    // The key must be present, but its value may be null: the C# required modifier says nothing about null.
    AssertTypeWidenedWithNull(Property(model, property));
    Assert.Contains(property, Required(model));
  }

  [Theory]
  [MemberData(nameof(OptionalNonNullableProperties))]
  public void AnOptionalNonNullableProperty_IsNotRequired_AndDoesNotAcceptNull(string definition, string property)
  {
    var model = Definition(definition, typeof(School));
    var schema = Property(model, property);

    // An initialised property may be left out of the input, but when it is there it is never null.
    Assert.Null(schema["anyOf"]);
    Assert.IsNotType<JsonArray>(schema["type"]);
    Assert.DoesNotContain(property, Required(model));
  }

  [Fact]
  public void NoOtherPropertyInTheSchema_IsNullable()
  {
    var expected = Rows(OptionalNullableReferences, OptionalNullableScalars, RequiredNullableScalars);

    var found = AllProperties(Document(typeof(School)))
      .Where(p => p.Schema["anyOf"] is not null || p.Schema["type"] is JsonArray)
      .Select(p => p.Path);

    Assert.Equal(expected.Order(StringComparer.Ordinal), found.Order(StringComparer.Ordinal));
  }

  [Fact]
  public void NoOtherPropertyInTheSchema_IsOptional()
  {
    var expected = Rows(OptionalNullableReferences, OptionalNullableScalars, OptionalNonNullableProperties);

    var found = AllProperties(Document(typeof(School)))
      .Where(p => !p.Required)
      .Select(p => p.Path);

    Assert.Equal(expected.Order(StringComparer.Ordinal), found.Order(StringComparer.Ordinal));
  }

  [Fact]
  public void EveryRequiredMember_IsListedAsRequired()
  {
    var person = Definition("Person", typeof(School));

    Assert.Equal(
      [
        "id", "givenName", "familyName", "role", "bornOn", "isActive", "gradePointAverage", "classes", "grades",
        "contactMethods",
      ],
      Required(person));
  }

  /// <summary>An inline scalar's nullability is a two-element <c>type</c> array ending in <c>"null"</c>.</summary>
  private static void AssertTypeWidenedWithNull(JsonObject schema)
  {
    var types = Assert.IsType<JsonArray>(schema["type"]);
    Assert.Equal(2, types.Count);
    Assert.NotEqual("null", types[0]!.GetValue<string>());
    Assert.Equal("null", types[1]!.GetValue<string>());
  }

  private static IEnumerable<string> Rows(params TheoryData<string, string>[] tables) =>
    tables.SelectMany(table => table.Select(row => $"{row.Data.Item1}.{row.Data.Item2}"));

  /// <summary>Every property of every model in the document, by <c>Definition.property</c>.</summary>
  private static IEnumerable<(string Path, JsonObject Schema, bool Required)> AllProperties(JsonObject document)
  {
    foreach (var (name, definition) in document["$defs"]!.AsObject())
    {
      if (definition!["properties"] is not JsonObject properties)
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
