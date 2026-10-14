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
  TypeMapping.cs                 a configured type mapping
  Reflection/                    the metadata-only layer
    MetadataFacts.cs             the firewall described above
    NullableFlagsReader.cs       decodes [Nullable] / [NullableContext] by hand
    JsonMemberReader.cs          a property as System.Text.Json sees it
    JsonMember.cs
    EnumFacts.cs                 members, ordering, string-converter detection
    DecoratorReader.cs           selected attributes, rendered for x-decorators
  Model/                         C# types to schema shapes
    ScalarTable.cs               the mapping table, in one place
    TypeClassifier.cs            mapped / scalar / enum / model / array / map / binary / opaque / unmapped
    TypeMappingResolver.cs       configured and [DotNetSchemaType] mappings, validated and tracked
    ClosureWalker.cs             what a document has to define
    DefinitionNameResolver.cs    $defs names, and collisions
    SchemaTypeRef.cs
    DecoratorKeywords.cs         well-known attributes to standard keywords
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
| a mapped type | its declared `{"type":…,"format":…,"pattern":…}`, inline — see [Type mappings](#type-mappings) |
| model | `{"$ref":"#/$defs/T"}`, always |
| model written by a `[JsonConverter]`, unmapped | `{}` — `DNS0019` |
| enum | `{"$ref":"#/$defs/E"}`; the definition's shape is set by `EnumStyle` — see [Enums](#enums-have-two-shapes-and-they-disagree) |
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

**`required`** follows the C# `required` modifier and `[JsonRequired]` — the two things System.Text.Json
itself refuses a payload for lacking — and nothing else. DataAnnotations `[Required]` is not one of them:
it is a non-null, non-empty check run by validation after deserialising, so it appears only under
[`x-decorators`](#decorators). Nullability is a separate question: a required member may still accept
null, and an optional one may not.

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

`[JsonConverter(typeof(JsonStringEnumConverter))]` on the enum or the property switches the value form
to strings. Either way the value keeps its `type` (`integer` or `string`), and
`SchemaGenerationOptions.EnumStyle` (`DotNetSchemaEnumStyle` in the build) picks how the member names
travel with it:

| Style | Shape | |
| --- | --- | --- |
| `OneOfConstTitle` (default) | `"oneOf": [{"const": 0, "title": "Student"}, …]` | Standard JSON Schema. |
| `EnumWithNames` | `"enum": [0, …], "x-enumNames": ["Student", …]` | The shape NSwag popularised and several code generators look for. `x-enumNames` is a vendor keyword: Draft 2020-12 treats it as an annotation, but a strict validator (Ajv's default strict mode) rejects the schema unless it is registered. |
| `Enum` | `"enum": [0, …]` | Terser; the names are lost. |

A name is always the **C# member name**, never the value again. That matters for a string enum renamed
by `[JsonStringEnumMemberName]` or a naming policy: it is written `"spiral-bound"`, and only the name says
it is `SpiralBound`. The style does not reach a dictionary key, which is always a plain `enum` of the names
it is written under. A naming policy set on `JsonSerializerOptions` is invisible to a metadata-only
read, so an application that configures one must repeat it as `SchemaGenerationOptions.EnumNamingPolicy`.

## Decorators

Attributes on a model, an enum or a property are exposed under the vendor keyword **`x-decorators`** —
named for the general idea rather than for .NET, so that schemas from other languages' generators can use
the same keyword. Each selected attribute application becomes one entry:

```json
"edition": {
  "type": "integer", "minimum": 1, "maximum": 99,
  "x-decorators": [
    { "name": "System.ComponentModel.DataAnnotations.RangeAttribute",
      "arguments": { "minimum": 1, "maximum": 99 } }
  ]
}
```

- `arguments` are the constructor arguments, keyed by parameter name; `properties` are the named
  arguments, ordered by name. Either is left out when empty. Only what the source wrote is visible: the
  attribute is read through `CustomAttributeData`, never instantiated.
- Values: strings, booleans and numbers as themselves; a `char` as a one-character string; a `Type` by a
  name free of assembly versions (`System.Collections.Generic.List<System.Int32>`, `List<T>` when open,
  `Outer+Nested`); an enum by member name, or as its number for a flags combination or an undeclared
  value; arrays as arrays; `NaN` and `±Infinity` as the strings `"NaN"`, `"Infinity"`, `"-Infinity"`.
- Entries are sorted by name, then by their rendered text — metadata does not promise an order.
- On a model or enum the list sits on its `$defs` entry; on a property, on the outermost schema — beside a
  `$ref`, beside the `anyOf` of a nullable reference, on the array rather than its `items`.
- On a positional record an attribute written without a `property:` target lands on the
  primary-constructor parameter. It is read from there too (a positional record is recognised by its
  compiler-generated `Deconstruct`); one applied identically to both is listed once.

**Selection.** `(Decorators ?? DefaultDecorators) ∪ DecoratorsAllow − DecoratorsDeny`, deny winning. Each
entry is an attribute's full name, a namespace wildcard `Ns.*` — types directly in `Ns`, so
`System.ComponentModel.DataAnnotations.*` does not reach Entity Framework's `.Schema` attributes — or `*`.
The defaults are `System.ComponentModel.DataAnnotations.*`, `DescriptionAttribute`,
`DefaultValueAttribute` and `DisplayNameAttribute` from `System.ComponentModel`, and
`System.ObsoleteAttribute`; they are part of the output contract. Whatever the selection says,
`System.Runtime.CompilerServices.*`, `System.Text.Json.Serialization.*` (already reflected in the schema),
`DotNetSchema.*` and the marker attribute are never exposed. `DecoratorsDeny = ["*"]` switches the
feature off.

**Standard keywords.** A selected well-known attribute is also translated, and only a selected one — denying
`[Range]` removes its bounds along with its entry:

| Attribute | Keywords | Applies to |
| --- | --- | --- |
| `[Description]` | `description` | model, enum, property |
| `[DisplayName]`, `[Display(Name, Description)]` | `title`, `description` | model, enum, property |
| `[Obsolete]` | `deprecated: true` | model, enum, property |
| `[DefaultValue(value)]` | `default` | a scalar property; not the `(Type, string)` overload, nor an enum value |
| `[Range]` (`int`, `double`, or `(Type, string, string)` for a numeric type) | `minimum` / `maximum`, or `exclusiveMinimum` / `exclusiveMaximum` under `MinimumIsExclusive` / `MaximumIsExclusive` | a numeric scalar |
| `[MinLength]`, `[MaxLength]`, `[Length]` | `minLength` / `maxLength`; `minItems` / `maxItems` on an array | a string or an array, not `byte[]` (its base64 length differs) |
| `[StringLength(max, MinimumLength)]` | `maxLength` / `minLength` | a string |
| `[RegularExpression]` | `pattern`, anchored as `^(?:…)$` — .NET matches the whole string, JSON Schema does not | a string |
| `[EmailAddress]`, `[Url]` | `format: email`, `format: uri` | a string |
| `[Required]` | nothing — see `required` above | |

A bound never loosens the type's own: `[Range(1, 99)]` on a `byte` gives `minimum: 1, maximum: 99`, and
`[Range(-1000, 1000)]` on it keeps `0` and `255`. A `pattern` or `format` that would clash with the type's
own (a `Guid`'s `uuid`), or any keyword on a schema it cannot describe (`[Range]` on a string), contributes
nothing and raises `DNS0015`; the entry stays in `x-decorators`. Attributes that cannot be decoded at all —
an argument naming a type from an assembly the load cannot resolve — raise `DNS0014`, and that member's
decorators are left out.

These keywords describe validation the model *declares*. System.Text.Json enforces none of it — it writes
a `[Range(1, 10)]` property holding `42` without complaint — so they are one of the two places the schema
says more than "what System.Text.Json writes" (the other is [type mappings](#type-mappings)). Deny the attribute if a schema consumer must accept everything the
serialiser can produce.

`x-decorators` is a vendor keyword: Draft 2020-12 treats it as an annotation, but a strict validator (Ajv's
default strict mode) rejects the schema unless it is registered.

## Type mappings

A type can be described by a declared inline schema instead of by its shape: a `SchemaGenerationOptions.TypeMappings`
entry (the build's `DotNetSchemaType` item) or `[DotNetSchemaType]` on the type. The package README,
["Custom type mappings"](../DotNetSchema/README.md#custom-type-mappings), is the user-facing account; the rules
the generator implements are:

- **Precedence:** configuration, then the attribute, then `ScalarTable`, then classification by shape. The
  mapping check is the first thing `TypeClassifier.Classify` does, so it beats enums and collection-shaped
  types too.
- **Lookup** is by `FullName`, or by the generic *definition's* `FullName` for a constructed generic — one
  mapping covers every construction. The attribute is read from the definition through `CustomAttributeData`,
  by name; the attribute type's name is taken from `typeof(DotNetSchemaTypeAttribute).FullName`, like the
  marker's.
- **Emission:** `{"type":…,"format":…,"pattern":…}` in that order, plus a `$comment` naming the type and the
  source of the declaration. Never a definition, never a `$ref`; the type's members are not walked and its
  model-level `x-decorators` are not emitted. Nullable widens it through the `type` array like an inline
  scalar. Property decorators apply to it as to a scalar of the declared type.
- **Dictionary keys:** a `string` mapping becomes `propertyNames`. Any other mapping speaks only for the value
  form, so the key falls through to what it would have been — fine for an enum or a primitive, `DNS0004` for
  anything else.
- **Validation** (`DNS0016`, error): `Type` one of `string`, `number`, `integer`, `boolean` (case-insensitive,
  emitted lower-case); `Pattern` only on `string`, and it must compile as a .NET `Regex`. A malformed mapping
  is ignored. A bad configured one is reported once for the run; a bad attribute every time its type is
  reached, so each document depending on it is withheld.
- **`DNS0017`:** one `TypeMappingResolver` serves a whole `Generate` call, recording which configured names
  were looked up, so a configured mapping no document reached can be reported at the end.
- **`DNS0018`:** a root that is also mapped withholds its document.
- **`DNS0019`:** a position about to classify as a model, governed by a converter — named on the property, or
  by `[JsonConverter]` on the type (its generic definition, for a construction) — and not mapped, becomes
  `SchemaTypeKind.Opaque`, emitted as `{}`, and its members are not walked.

This is the second deliberate step beyond "the schema describes what System.Text.Json writes", after the
[decorator keywords](#decorators): the generator cannot see a converter's output, so a mapping is the
consumer's assertion about it, taken on trust.

## Determinism

Output is byte-stable, because the goldens depend on it and so will the TypeScript side.

- `$defs` sorted ordinally by definition name.
- Properties in wire order: `[JsonPropertyOrder]`, then declaration order via `MetadataToken` — the one
  ordering key provably identical on the runtime and metadata-only paths, since reflection promises
  nothing about the order `GetProperties` returns.
- Enum members by ascending value, then `MetadataToken`, matching `Enum.GetNames`.
- `x-decorators` entries by name, then rendered text; mapped keywords in one fixed order; a `Type` argument
  by `MetadataFacts.TypeName`, never `FullName`, which embeds a different core assembly on each path.
- A type mapping's keywords as `type`, `format`, `pattern`, `$comment`; the comment names the type by
  `MetadataFacts.TypeName` for the same reason.
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
