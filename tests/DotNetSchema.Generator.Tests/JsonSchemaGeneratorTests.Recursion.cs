using DotNetSchema.Fixtures.Schema;

namespace DotNetSchema.Generator.Tests;

public sealed partial class JsonSchemaGeneratorTests
{
  [Fact]
  public void TheClosureOfSchool_IsEveryRecordEveryEnumAndTheOneBclEnum()
  {
    var document = Document(typeof(School));
    var definitions = document["$defs"]!.AsObject().Select(d => d.Key).ToList();

    Assert.Equal(
      [
        "Address", "Assessment", "AssessmentKind", "AssessmentRecord", "Book", "BookName", "Building",
        "Campus", "Chapter", "Class", "ClassName", "ClassSession", "DayOfWeek", "Department",
        "DepartmentName", "Exercise", "Library", "Person", "PersonRole", "Room", "RoomKind",
        "ScalarBattery", "ScalarRegister", "ScalarSeries", "School", "Section", "Term",
      ],
      definitions);
  }

  [Fact]
  public void ADirectlyRecursiveProperty_RefersToItsOwnDefinition()
  {
    var person = Definition("Person", typeof(School));

    Assert.Equal("#/$defs/Person", Property(person, "advisees")["items"]!["$ref"]!.GetValue<string>());
    Assert.Equal("#/$defs/Person", Property(person, "mentor")["anyOf"]![0]!["$ref"]!.GetValue<string>());
  }

  [Fact]
  public void AnIndirectlyRecursiveCycle_ClosesThroughDefinitions()
  {
    var document = Document(typeof(School));
    var book = (System.Text.Json.Nodes.JsonObject)document["$defs"]!["Book"]!;
    var @class = (System.Text.Json.Nodes.JsonObject)document["$defs"]!["Class"]!;

    // Class -> Book -> Class, with neither side inlined and no depth budget anywhere.
    Assert.Equal("#/$defs/Class", Property(book, "class")["anyOf"]![0]!["$ref"]!.GetValue<string>());
    Assert.Equal("#/$defs/Book", Property(@class, "materials")["items"]!["$ref"]!.GetValue<string>());
  }

  [Fact]
  public void NoPropertyAnywhere_IsDescribedAsABareObject()
  {
    // The whole point of the exercise: if a property could be a reference to another definition, it is one.
    var document = Document(typeof(School));

    foreach (var (name, definition) in document["$defs"]!.AsObject())
    {
      if (definition!["properties"] is not System.Text.Json.Nodes.JsonObject properties)
      {
        continue;
      }

      foreach (var (property, schema) in properties)
      {
        var isBareObject =
          schema!["type"]?.ToString() == "object" &&
          schema["additionalProperties"] is null &&
          schema["properties"] is null;

        Assert.False(isBareObject, $"{name}.{property} is an untyped object.");
      }
    }
  }
}
