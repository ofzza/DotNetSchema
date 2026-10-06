# DotNetSchema.Generator

The engine behind [`DotNetSchema`](../DotNetSchema/README.md). Give it
assemblies, get JSON Schema Draft 2020-12 documents.

```csharp
var result = new JsonSchemaGenerator().Generate([typeof(School).Assembly]);

foreach (var document in result.Documents)
{
  File.WriteAllBytes(document.FileName, document.ToUtf8Bytes());
}
```

Nothing here writes to a console, touches the file system, or throws because its input was wrong.
Problems come back as `SchemaDiagnostic` values, which is what lets the tool render them in MSBuild's
canonical form and the tests assert on them as data.

## The one rule

**No type in this project may be compared against a runtime type.**

The generator is used two ways: against the running assemblies, which is how the tests reach it, and
against assemblies opened for metadata only, which is how the build does. A type read through a
`MetadataLoadContext` is never reference-equal to a runtime type, so every one of these answers the wrong
question on the second path:

- `typeof(X) == t`, `t.IsAssignableTo(...)`, `t is X`
- `Nullable.GetUnderlyingType(t)` — a bare `ReferenceEquals(genericType, typeof(Nullable<>))`
- `Attribute.GetCustomAttribute<T>(...)`, `Enum.GetValues(t)`
- `NullabilityInfoContext`

None of them throw. They return a plausible wrong answer, and `NullabilityInfoContext` goes further and
desynchronises its own byte-array walk, corrupting the result for unrelated members. So everything goes
through `Reflection/MetadataFacts.cs`, which asks the same questions using `FullName` comparisons,
`CustomAttributeData` and `GetRawConstantValue()`.

`ReadingTypesForMetadataOnly_ProducesTheSameBytesAsReadingThemAtRuntime` is what enforces this: it runs
the generator both ways over the fixtures and asserts the documents are byte-identical. If a `typeof`
creeps in, that test is where it surfaces.

## Layout

```
DotNetSchema.Generator/
  IJsonSchemaGenerator.cs        the contract
  JsonSchemaGenerator.cs         attribute scan, grouping by document, ordering
  SchemaGenerationOptions.cs     every choice, with its default
  SchemaGenerationResult.cs      documents, diagnostics, source locations
  Reflection/                    the metadata-only layer
    MetadataFacts.cs             the firewall described above
    NullableFlagsReader.cs       decodes [Nullable] / [NullableContext] by hand
    JsonMemberReader.cs          a property as System.Text.Json sees it
    JsonMember.cs
    EnumFacts.cs                 members, ordering, string-converter detection
  Model/                         C# types to schema shapes
    ScalarTable.cs               the mapping table, in one place
    TypeClassifier.cs            scalar / enum / model / array / map / binary / unmapped
    ClosureWalker.cs             what a document has to define
    DefinitionNameResolver.cs    $defs names, and collisions
    SchemaTypeRef.cs
  Emit/
    SchemaDocumentWriter.cs      the document, key by key
    JsonWriterDefaults.cs        the writer settings that make output byte-stable
  Diagnostics/
    SchemaDiagnosticCodes.cs
```

## The mapping table

Checked against what the .NET 10 `System.Text.Json` converters actually write, not against what the
keyword names suggest. Three rows marked ⚠ are places where the obvious answer would be false.

| C# | Schema |
| --- | --- |
| `bool` | `{"type":"boolean"}` |
| `sbyte` `byte` `short` `ushort` `int` `uint` | `{"type":"integer","minimum":…,"maximum":…}` |
| `long` | `{"type":"integer"}` — no bounds |
| `ulong` | `{"type":"integer","minimum":0}` — no upper bound |
| `float` `double` `decimal` | `{"type":"number"}` |
| `char` | `{"type":"string","minLength":1,"maxLength":1}` |
| `string` | `{"type":"string"}` |
| `Guid` | `{"type":"string","format":"uuid"}` |
| `DateOnly` | `{"type":"string","format":"date"}` |
| ⚠ `TimeOnly` | a pattern. Written as `hh:mm:ss[.fffffff]` with **no offset**, so it does not satisfy RFC 3339 `full-time` and `format: "time"` would be false. |
| ⚠ `TimeSpan` | a pattern. Written as `[-][d.]hh:mm:ss[.fffffff]`, which is **not** an ISO-8601 duration, so `format: "duration"` would be false. |
| ⚠ `DateTime` | `{"type":"string","format":"date-time"}` — true only when `Kind` is not `Unspecified`, which the type does not guarantee. Noted in a `$comment`. |
| `DateTimeOffset` | `{"type":"string","format":"date-time"}` |
| model | `{"$ref":"#/$defs/T"}`, always |
| `IReadOnlyList<T>` and friends, `T[]` | `{"type":"array","items":…}` |
| `byte[]`, `Memory<byte>` | `{"type":"string","contentEncoding":"base64"}` |
| `IReadOnlyDictionary<K,V>` and friends | `{"type":"object","propertyNames":…,"additionalProperties":…}` |
| `Nullable<T>`, `T?` | see below |
| `object`, `nint`, `Int128`, `Half`, an interface | nothing — `DNS0002` |

**Integer bounds** are emitted only where the value is exactly representable as an IEEE-754 double.
Most validators parse JSON numbers into doubles, where `"maximum": 18446744073709551615` reads back one
larger than it was written; a constraint whose meaning depends on the reader is worse than no constraint.

**Nullability** widens a schema one of two ways. A `$ref` or anything carrying an enumeration goes into
`{"anyOf":[…,{"type":"null"}]}` — it has to, because since Draft 2019-09 a `$ref` no longer suppresses
its siblings, so `{"$ref":…,"type":"null"}` demands both at once and nothing satisfies it. A plain inline
scalar instead takes a second entry in its `type` array, which is safe because `format`, `pattern` and
the numeric bounds are all type-conditional.

**`required`** follows the C# `required` modifier and nothing else. Nullability is a separate question:
a required member may still accept null, and an optional one may not.

### Enums have two shapes, and they disagree

As a **value** an enum is a number. As a **dictionary key** it is the member **name** — because a JSON
property name is a string, and `System.Text.Json` fills its write-name cache for every member whether or
not the string converter is in force. So in one document:

```json
"kind":  { "$ref": "#/$defs/AssessmentKind" },
"weightByAssessmentKind": {
  "type": "object",
  "propertyNames": { "type": "string", "enum": ["Quiz", "Essay", …] }
}
```

`Assessment.Kind` serialises as `3`; the keys of `IReadOnlyDictionary<AssessmentKind, decimal>` serialise
as `"Essay"`. Same CLR type, no attribute anywhere hinting at it. The document makes it visible, which is
most of the value in generating one.

The numeric form carries its member names as `oneOf` of annotated constants, so code generation still
gets them. `[JsonConverter(typeof(JsonStringEnumConverter))]` on the enum or the property switches the
value form to strings. A naming policy set on `JsonSerializerOptions` is invisible to a metadata-only
read, so an application that configures one must repeat it as `SchemaGenerationOptions.EnumNamingPolicy`.

## Determinism

Output is byte-stable, because the goldens depend on it and so will the TypeScript side.

- `$defs` sorted ordinally by definition name.
- Properties in wire order: `[JsonPropertyOrder]`, then declaration order via `MetadataToken` — the one
  ordering key provably identical on the runtime and metadata-only paths, since reflection promises
  nothing about the order `GetProperties` returns.
- Enum members by ascending value, then `MetadataToken`, matching `Enum.GetNames`.
- `JsonWriterOptions.NewLine` pinned to `"\n"`. It defaults to `Environment.NewLine`, so leaving it unset
  would emit CRLF on Windows and put a golden out of reach.
- `JavaScriptEncoder.UnsafeRelaxedJsonEscaping`, so the `TimeSpan` pattern's `\d+` stays readable rather
  than becoming `\d+`. "Unsafe" means unsafe to interpolate into HTML, which a schema file is not.
- One trailing newline; UTF-8 with **no** BOM.

That last one departs from this repository's own C# sources, which in this project, the tool and the tests
start with a BOM. A BOM in a schema file breaks readers outside .NET, and the documents land in `bin/`
rather than under source control, so the departure costs nothing.

## Tests

[`DotNetSchema.Generator.Tests`](../../tests/DotNetSchema.Generator.Tests/README.md).

## License

MIT — see [LICENSE.md](../../LICENSE.md).
