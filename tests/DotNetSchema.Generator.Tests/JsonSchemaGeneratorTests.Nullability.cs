using DotNetSchema.Fixtures.Schema;

namespace DotNetSchema.Generator.Tests;

public sealed partial class JsonSchemaGeneratorTests
{
  /// <summary>
  /// The nine nullable reference properties in the fixtures schema, which are also the only nine that are
  /// not <c>required</c>. Both halves are asserted, because they are separate questions that happen to
  /// agree here.
  /// </summary>
  public static TheoryData<string, string> NullableProperties => new()
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

  [Theory]
  [MemberData(nameof(NullableProperties))]
  public void ANullableReference_IsWrappedInAnyOf_AndIsNotRequired(string definition, string property)
  {
    var model = Definition(definition, typeof(School));
    var schema = Property(model, property);

    // anyOf rather than a sibling "type": since Draft 2019-09 a $ref no longer suppresses its siblings,
    // so {"$ref": ..., "type": "null"} would demand both at once and nothing could satisfy it.
    var alternatives = Assert.IsType<System.Text.Json.Nodes.JsonArray>(schema["anyOf"]);
    Assert.Equal(2, alternatives.Count);
    Assert.NotNull(alternatives[0]!["$ref"]);
    Assert.Equal("null", alternatives[1]!["type"]!.GetValue<string>());

    Assert.DoesNotContain(property, Required(model));
  }

  [Fact]
  public void NoOtherPropertyInTheSchema_IsNullable()
  {
    var document = Document(typeof(School));
    var expected = NullableProperties.Select(row => $"{row.Data.Item1}.{row.Data.Item2}").ToHashSet();
    var found = new List<string>();

    foreach (var (name, definition) in document["$defs"]!.AsObject())
    {
      if (definition!["properties"] is not System.Text.Json.Nodes.JsonObject properties)
      {
        continue;
      }

      found.AddRange(
        from property in properties
        where property.Value!["anyOf"] is not null || property.Value!["type"] is System.Text.Json.Nodes.JsonArray
        select $"{name}.{property.Key}");
    }

    Assert.Equal(expected.Order(), found.Order());
  }

  [Fact]
  public void EveryRequiredMember_IsListedAsRequired()
  {
    var person = Definition("Person", typeof(School));

    Assert.Equal(
      [
        "id", "givenName", "familyName", "role", "bornOn", "email", "isActive", "gradePointAverage",
        "classes", "grades", "contactMethods", "advisees",
      ],
      Required(person));
  }
}
