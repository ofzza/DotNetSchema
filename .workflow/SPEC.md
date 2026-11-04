# Specification

What DotNetSchema is and must do. Derived on 2026-11-04 from the documentation as of `0.1.0-alpha.4` (`README.md`, `AGENTS.md`, `CONTRIBUTE.md`, `PUBLISH.md`, `WORKFLOW.custom.md` and the project READMEs), and restated here in full so it stands on its own. Where this file and a README disagree, ask which one is right; never settle it silently.

## What it is

- A NuGet package, id **`DotNetSchema`**, MIT, [published on nuget.org](https://www.nuget.org/packages/DotNetSchema/), currently `0.1.0-alpha.4`.
- It exports `[DotNetSchema]`-marked .NET records and classes (models) to **JSON Schema Draft 2020-12** documents on every build of a host project that opts in with `DotNetSchemaGenerate=true`.
- Assemblies are read **metadata-only** (`MetadataLoadContext`): no consumer code (module initialisers, static constructors, converters) runs during an export.
- Everything targets `net10.0`, the attribute assembly too, deliberately: hosts need the .NET 10 SDK, and every project referencing the package needs `net10.0` or later (an earlier TFM restores, then fails `using DotNetSchema;` with `CS0246`).
- Three shipped parts:
  - `src/DotNetSchema`: the attribute(s), the `buildTransitive/` MSBuild integration, and packaging of the pre-built tool under `tools/net10.0/any/`. The only assembly a consumer references; it has **no dependencies**.
  - `src/DotNetSchema.Generator`: the engine. Assemblies in, `SchemaDocument`s and `SchemaDiagnostic`s out. Pure.
  - `src/DotNetSchema.Tool`: the console shell MSBuild runs with `dotnet exec`: metadata-only load, idempotent writes, manifest, diagnostics in MSBuild canonical form.
- Supporting projects: `tests/DotNetSchema.Fixtures` (the "school" model graph, a deterministic instance generator, and a live in-repo consumer of the build integration), `tests/DotNetSchema.Generator.Tests` (xUnit v3), and `samples/DotNetSchema.Sample.Models` + `samples/DotNetSchema.Sample.Host` (the model-library / host split, custom output path, type mappings and decorator rules).

## HARD RULES

1. **Golden digests are never edited to make a test pass.** If a change was meant to move the generated output, the digests are regenerated (`dotnet build tests/DotNetSchema.Fixtures`, then `sha256sum` of `tests/DotNetSchema.Fixtures/bin/Debug/net10.0/{assessment,schema,school}.json`, pasted into `JsonSchemaGeneratorTests.Goldens.cs`) and the commit message says why; if it was not meant to, the change is wrong.
2. **`DNS` diagnostic codes are permanent: never reused or repurposed.** A code that is no longer needed is retired, and its number is never assigned again. `DNS0008` is unassigned and stays a gap.

## Must-haves

### Marking and discovery

- `[DotNetSchema]` exports to `<AssemblyName>.schema.json` (the `DotNetSchemaDefaultFileName`); `[DotNetSchema("orders.json")]` to the named document. The file name must be a bare `.json` file name (`DNS0007`).
- Only entry points need marking: everything reachable from a marked type, through properties, collection elements and both halves of a dictionary, is carried into the same document. A type may be marked more than once, to publish it into several documents.
- One document per distinct file name, regenerated on every build of the host.
- The host scans its own assembly **and its whole project-reference closure** (unless `DotNetSchemaScanProjectReferences=false`). Projects without `DotNetSchemaGenerate=true` export nothing.
- A marked type must be a concrete, non-generic class, record or struct (`DNS0001`); a generic type in the closure is an error (`DNS0003`) unless it is mapped.

### Document shape

- A definition bundle: `$schema` (Draft 2020-12), `$id` `urn:dotnetschema:schema:<name>`, `title`, a `$comment` naming the exported types, and `$defs`.
- **A model is always a `$ref` into `$defs`, never a bare `object`** — unless its type is mapped. Recursion needs no special handling and has no depth limit.
- **Each document is self-contained**: a type reachable from two documents is defined in both; no cross-document `$ref`.
- Two types sharing a short name: both definitions are qualified (`DNS0005`, warning).

### The schema describes what System.Text.Json writes

- Property names camelCase; `[JsonPropertyName]`, `[JsonIgnore]`, `[JsonPropertyOrder]` and `[JsonRequired]` are honoured. Two properties serialising under one name: `DNS0006` (warning).
- The mapping table, checked against what the .NET 10 System.Text.Json converters actually write:

  | C#                                               | Schema                                                                                                         |
  | ------------------------------------------------ | -------------------------------------------------------------------------------------------------------------- |
  | `bool`                                           | `{"type":"boolean"}`                                                                                           |
  | `sbyte` `byte` `short` `ushort` `int` `uint`     | `{"type":"integer","minimum":…,"maximum":…}`                                                                   |
  | `long`                                           | `{"type":"integer"}`, no bounds                                                                                |
  | `ulong`                                          | `{"type":"integer","minimum":0}`, no upper bound                                                               |
  | `float` `double` `decimal`                       | `{"type":"number"}`                                                                                            |
  | `char`                                           | `{"type":"string","minLength":1,"maxLength":1}`                                                                |
  | `string`                                         | `{"type":"string"}`                                                                                            |
  | `Guid`                                           | `{"type":"string","format":"uuid"}`                                                                            |
  | `DateOnly`                                       | `{"type":"string","format":"date"}`                                                                            |
  | `TimeOnly`                                       | a pattern (`hh:mm:ss[.fffffff]`, no offset, so not `format: time`)                                             |
  | `TimeSpan`                                       | a pattern (`[-][d.]hh:mm:ss[.fffffff]`, not an ISO-8601 duration)                                              |
  | `DateTime`                                       | `{"type":"string","format":"date-time"}`, with a `$comment` that it holds only when `Kind` isn't `Unspecified` |
  | `DateTimeOffset`                                 | `{"type":"string","format":"date-time"}`                                                                       |
  | a mapped type                                    | its declared `type` / `format` / `pattern`, inline                                                             |
  | model                                            | `{"$ref":"#/$defs/T"}`                                                                                         |
  | model written by a `[JsonConverter]`, unmapped   | `{}`, with `DNS0019`                                                                                           |
  | enum                                             | `{"$ref":"#/$defs/E"}`, definition shaped by the enum style                                                    |
  | `IReadOnlyList<T>` and friends, `T[]`            | `{"type":"array","items":…}`                                                                                   |
  | `byte[]`, `Memory<byte>`                         | `{"type":"string","contentEncoding":"base64"}`                                                                 |
  | `IReadOnlyDictionary<K,V>` and friends           | `{"type":"object","propertyNames":…,"additionalProperties":…}`                                                 |
  | `object`, `nint`, `Int128`, `Half`, an interface | nothing, `DNS0002` (error)                                                                                     |

- Integer bounds only where exactly representable as an IEEE-754 double.
- **Nullability**: a `$ref` or anything carrying an enumeration widens via `{"anyOf":[…,{"type":"null"}]}`; an inline scalar via a second entry in its `type` array.
- **`required`** follows only the C# `required` modifier and `[JsonRequired]`, never DataAnnotations `[Required]` (which appears only under `x-decorators`). Nullability is a separate question.
- **Enums have two shapes**: as a value, a number (or a string under `JsonStringEnumConverter` on the enum or the property); as a dictionary key, always the member **name**, a plain `enum` of names. The value form's member names travel by `DotNetSchemaEnumStyle`: `OneOfConstTitle` (default, `oneOf` of `const` + `title`), `EnumWithNames` (`enum` + vendor `x-enumNames`) or `Enum` (values only). A name is always the C# member name. A property's enum converter disagreeing with the type's inlines the schema (`DNS0009`, warning). A naming policy on `JsonSerializerOptions` is invisible and must be repeated as `SchemaGenerationOptions.EnumNamingPolicy`.
- Dictionary keys System.Text.Json cannot write as property names: `DNS0004` (error); floating-point keys: `DNS0012` (warning).

### Decorators (`x-decorators`) and mapped validation keywords

- Attributes on a model, an enum or a property are listed under the vendor keyword `x-decorators`: `name`, constructor `arguments` by parameter name, named `properties` ordered by name, each left out when empty. Read through `CustomAttributeData`, never instantiated. Value rendering: strings, booleans, numbers as themselves; `char` as a one-character string; a `Type` by an assembly-version-free name (`MetadataFacts.TypeName`); an enum by member name (a number for flags combinations or undeclared values); arrays as arrays; `NaN` / `±Infinity` as strings. Entries sorted by name, then rendered text.
- Placement: on the `$defs` entry for a model or enum; on a property's outermost schema (beside a `$ref`, beside the `anyOf`, on the array rather than `items`). On a positional record, an attribute without a `property:` target is read from the primary-constructor parameter; one applied identically to both is listed once.
- **Selection** is an ordered rule list: `DefaultDecorators`, then the `DotNetSchemaDecorator` items in the order written. The **last** rule matching an attribute's full name decides; no match means left out. `*` matches any run of characters, dots included; everything else is literal and ordinal.
- Default rules (output contract): include `System.ComponentModel.DataAnnotations.*`, exclude `System.ComponentModel.DataAnnotations.*.*` (keeps EF's `DataAnnotations.Schema` out), include `System.ComponentModel.DefaultValueAttribute`, `DescriptionAttribute`, `DisplayNameAttribute`, and `System.ObsoleteAttribute`.
- Never exposed, whatever the rules: `System.Runtime.CompilerServices`, `System.Text.Json.Serialization` and `DotNetSchema` namespace attributes, and the marker — matched by **exact namespace**, never the recursive wildcard.
- One selection drives both outputs: an attribute left out yields neither its entry nor its keywords.
- Mapped keywords: `[Description]` → `description`; `[DisplayName]` / `[Display]` → `title` / `description`; `[Obsolete]` → `deprecated`; `[DefaultValue]` → `default` (scalar property); `[Range]` → `minimum` / `maximum` (or exclusive variants); `[MinLength]` / `[MaxLength]` / `[Length]` → string length or array items (not `byte[]`); `[StringLength]` → string length; `[RegularExpression]` → `pattern` anchored `^(?:…)$`; `[EmailAddress]` / `[Url]` → `format`; `[Required]` → nothing.
- A bound never loosens the type's own. A clashing keyword, or one on a schema it cannot describe, contributes nothing and raises `DNS0015` (warning); undecodable attributes raise `DNS0014` (warning) and drop that member's decorators.
- These keywords are a **deliberate exception** to "describes what System.Text.Json writes": STJ does not enforce them, and the docs must keep saying so.

### Type mappings

- A type can be described by a declared inline schema: a `DotNetSchemaType` item (`SchemaGenerationOptions.TypeMappings`) or `[DotNetSchemaType]` on the type. Fields: `Type` (`string`, `number`, `integer`, `boolean`, required), `Format`, `Pattern` (string only, a valid .NET regex, unanchored).
- **Precedence**: item, then attribute, then `ScalarTable`, then classification by shape. Of two items for the same type, the later wins.
- Emitted inline as `type`, `format`, `pattern` (plus a `$comment` naming the type and source); never a definition or `$ref`; members not walked. Nullable widens through the `type` array.
- Lookup by full name (`Outer+Inner` for nested); a generic by its definition (``Id`1``), so one mapping covers every construction, and a mapped generic is accepted as a model position.
- Dictionary keys: a `string` mapping becomes `propertyNames`; any other falls through (`DNS0004` for non-enum, non-primitive).
- Diagnostics: `DNS0016` malformed mapping (error, ignored); `DNS0017` an item matched no reached type (warning); `DNS0018` a type both marked and mapped (error); `DNS0019` a model position governed by a `[JsonConverter]` without a mapping, emitted as `{}` (warning).
- A **deliberate exception**: the consumer's unchecked assertion about their converter; the docs must keep saying so.

### Property formats

- `[DotNetSchemaFormat(format, Pattern = …)]` on a property (or a positional record's parameter) sets `format` and/or `pattern` on that property's values, never `type`.
- Applied before the DataAnnotations keywords: it replaces the type's own keyword (scalar table, mapping) for that keyword only, and a clashing `[EmailAddress]` / `[Url]` / `[RegularExpression]` gives way with `DNS0015`.
- On an array or dictionary it reaches through to elements / values at any depth, never keys.
- `DNS0020` (error): malformed (neither argument, empty format, invalid regex). `DNS0021` (warning): on models, enums or opaque values, or a pattern on a non-string.
- Never in `x-decorators`. Another unchecked assertion; the docs must keep saying so, and that unregistered formats break strict validators.

### Purity and the metadata-only rule

- **No runtime-type comparisons in `DotNetSchema.Generator`**: no `typeof(X) == t`, `IsAssignableTo`, `is X`, `Nullable.GetUnderlyingType`, `GetCustomAttribute<T>`, `Enum.GetValues`, `NullabilityInfoContext`. Everything goes through `Reflection/MetadataFacts.cs`. Enforced by `ReadingTypesForMetadataOnly_ProducesTheSameBytesAsReadingThemAtRuntime`.
- **The generator is pure**: no console, no file system, no throwing because the input was wrong. Every problem is a `SchemaDiagnostic`; an error withholds only the document it concerns.

### Determinism

- Output is byte-stable: `$defs` ordinal by name; properties by `[JsonPropertyOrder]` then `MetadataToken`; enum members by value then `MetadataToken`; `x-decorators` by name then rendered text; mapped keywords in a fixed order; `\n` newlines; `UnsafeRelaxedJsonEscaping`; one trailing newline; UTF-8 **without** BOM.
- Never ordering from `GetProperties()` or dictionary iteration, timestamps, or `Environment.NewLine`.

### Diagnostics

All printed in MSBuild canonical form, so `Exec` promotes them to build errors and warnings.

| Code      | Severity | Source  | Meaning                                                                         |
| --------- | -------- | ------- | ------------------------------------------------------------------------------- |
| `DNS0000` | error    | tool    | The tool itself failed (unhandled exception, bad argument).                     |
| `DNS0001` | error    | gen.    | A marked type is not a concrete, non-generic class, record or struct.           |
| `DNS0002` | error    | gen.    | A property's type has no JSON Schema mapping.                                   |
| `DNS0003` | error    | gen.    | A generic type appeared in the closure.                                         |
| `DNS0004` | error    | gen.    | A dictionary key System.Text.Json cannot write as a property name.              |
| `DNS0005` | warning  | gen.    | Two types shared a short name; both definitions were qualified.                 |
| `DNS0006` | warning  | gen.    | Two properties of one type serialise under the same name.                       |
| `DNS0007` | error    | gen.    | The attribute names something that is not a bare `.json` file name.             |
| `DNS0008` | —        | —       | Unassigned; leave the gap.                                                      |
| `DNS0009` | warning  | gen.    | A property's enum converter disagrees with the enum type's; schema inlined.     |
| `DNS0010` | warning  | gen.    | An assembly yielded types that could not be loaded; the rest were scanned.      |
| `DNS0011` | warning  | gen.    | Reference-typed members with no nullability metadata; treated as non-nullable.  |
| `DNS0012` | warning  | gen.    | A dictionary keyed by a floating-point type.                                    |
| `DNS0013` | error    | tool    | Nothing was marked, and `DotNetSchemaFailOnEmpty` is set.                       |
| `DNS0014` | warning  | gen.    | Attributes could not be read; decorators left out.                              |
| `DNS0015` | warning  | gen.    | A selected attribute's keyword does not fit; it stays in `x-decorators` only.   |
| `DNS0016` | error    | gen.    | A type mapping is malformed and was ignored.                                    |
| `DNS0017` | warning  | gen.    | A `DotNetSchemaType` item matched no type any exported document reached.        |
| `DNS0018` | error    | gen.    | A type is both marked for export and mapped.                                    |
| `DNS0019` | warning  | gen.    | A model position written by a `[JsonConverter]` has no mapping; described `{}`. |
| `DNS0020` | error    | gen.    | A `[DotNetSchemaFormat]` is malformed and was ignored.                          |
| `DNS0021` | warning  | gen.    | A `[DotNetSchemaFormat]` is where values take no format or pattern; ignored.    |
| `DNS1001` | error    | targets | The export tool could not be located.                                           |

### MSBuild integration

- Properties (set in the host `.csproj` or `Directory.Build.props`):

  | Property                            | Default                       | What it does                                          |
  | ----------------------------------- | ----------------------------- | ----------------------------------------------------- |
  | `DotNetSchemaGenerate`              | `false`                       | Opt in; nothing runs otherwise.                       |
  | `DotNetSchemaOutputPath`            | `$(OutDir)`                   | Output directory; relative to the project directory.  |
  | `DotNetSchemaDefaultFileName`       | `$(AssemblyName).schema.json` | Document a bare `[DotNetSchema]` targets.             |
  | `DotNetSchemaScanProjectReferences` | `true`                        | `false` exports only this project's own marked types. |
  | `DotNetSchemaIncludeInPublish`      | `true`                        | Also copy the documents to `$(PublishDir)`.           |
  | `DotNetSchemaFailOnEmpty`           | `false`                       | Fail the build (`DNS0013`) when nothing is marked.    |
  | `DotNetSchemaEnumStyle`             | `OneOfConstTitle`             | `OneOfConstTitle`, `EnumWithNames` or `Enum`.         |
  | `DotNetSchemaToolPath`              | _(empty)_                     | Escape hatch: run a specific prebuilt tool.           |

- Items (lists are items, ordered, no separator syntax): `DotNetSchemaDecorator` (`Include="true|false"`, pattern in `Attribute` metadata) and `DotNetSchemaType` (type name in `Include`, `Type` / `Format` / `Pattern` metadata). Patterns live in metadata, never in an `Include`, and reach the tool through item transforms (neither split nor globbed).
- `@(DotNetSchemaOutput)` exposes the produced files to targets ordered after `_DotNetSchemaPrepare`.
- Two delivery paths must both keep working: the **package** (auto-imported `buildTransitive/`, pre-built tool under `tools/`) and **in-repo** (`<Import>` of `DotNetSchema.targets` next to a `ProjectReference`; the tool built through an injected build-only `ProjectReference`).
- Every default in `.props` carries a `Condition`; values derived from SDK-computed properties are computed inside `_DotNetSchemaPrepare`; `_DotNetSchemaPrepare` always runs and reads the manifest, outside the incremental `DotNetSchema` target; the export hooks `AfterTargets="CopyFilesToOutputDirectory"`; design-time builds never run the tool; internal names are `_DotNetSchema`-prefixed.
- Arguments reach the tool through a response file (`--scan`, `--reference`, `--output-directory`, `--default-file-name`, `--manifest`, `--fail-on-empty`, `--enum-style`, `--decorator`, `--type-mapping`).

### Tool

- Metadata-only load; references deduplicated by assembly name (highest version wins); never scans via `@(ReferencePathWithRefAssemblies)`.
- **Idempotent writes** (unchanged bytes are not rewritten); stale documents listed in the previous manifest are **pruned**, with deletion confined to the output directory.
- Exit code `0`, or `1` when any error diagnostic was raised.
- Public types are API: `MetadataLoadContextFactory`, `SchemaFileWriter`, `ToolArguments`.

### Quality and process

- Tests: xUnit v3; one `sealed partial` `JsonSchemaGeneratorTests`, one file per contract; every tested shape lives in `DotNetSchema.Fixtures` (never declared in a test); one test per `DNS` code (`DNS0010`, `DNS0011`, `DNS0014` knowingly untested); golden digests pin the fixtures documents; the hand-rolled `JsonSchemaValidator` covers exactly the emitted keywords; a real fixture graph must validate against its generated schema; fixtures deterministic (`StableRandom`, invariant culture, path-derived seeds).
- CI (`.github/workflows/ci.yml`): pull requests to and pushes on `master` / `develop`, `ubuntu-latest` + `windows-latest`, restore → Release build → test → pack.
- Releases follow `PUBLISH.md`: checklists, consumer smoke test of the packed package, version bump, tag, fast-forward `master`, `dotnet nuget push`. Branching follows `WORKFLOW.custom.md` › "Branching".
- A schema change for unchanged models is a breaking change for consumers generating code from it (semver).

## Limits and non-goals

- Models that reach the host as **packages** (not projects) are not scanned.
- Paths containing `;` are unsupported (MSBuild splits item lists on them).
- `net10.0` only; no multi-targeting of the attribute assembly.
- Converters registered only in `JsonSerializerOptions`, and naming policies set there, are invisible to a metadata-only read; the consumer declares them (mappings, `EnumNamingPolicy`).
- Vendor keywords (`x-decorators`, `x-enumNames`) and unregistered formats are rejected by strict validators (e.g. Ajv strict mode) until registered.

## Time and complexity budget

Small, maintenance: a hobby-scale library with occasional features and fixes, working toward a stable `0.1.0`. At most one zero-context review before a feature.

## Pitfalls

- **The ordinary build never exercises the packaged path.** Only the consumer smoke test in `PUBLISH.md` does; skipping it can ship a broken package.
- **nuget.org versions are permanent** (unlist only); a bad push cannot be taken back.
- **The global NuGet cache keeps the first copy of a version**, so re-packing the same version tests a stale copy unless a private `NUGET_PACKAGES` is used.
- **`System.Reflection.MetadataLoadContext`'s major version tracks the TFM**; it must be bumped by hand with the TFM, in both the tool and the test project.
- **Runtime-type comparisons return plausible wrong answers under `MetadataLoadContext`** rather than failing; only the runtime-vs-metadata byte-equality test catches them.
- **Line endings**: without the `.gitattributes` LF pin, a Windows checkout breaks the golden literals.
- **No branch protection** on `master` / `develop`: CI does not block merges by itself.

## Open points

_None._

## Review notes

_None._
