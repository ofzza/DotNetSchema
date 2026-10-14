# DotNetSchema

[![NuGet](https://img.shields.io/nuget/vpre/DotNetSchema)](https://www.nuget.org/packages/DotNetSchema/)

Mark a data model, build the host project, get a JSON Schema.

```csharp
using DotNetSchema;

[DotNetSchema]                       // -> <AssemblyName>.schema.json
public sealed record Customer(Guid Id, string Name, Address BillingAddress);

[DotNetSchema("orders.json")]        // -> orders.json
public sealed record Order(Guid Id, DateTimeOffset PlacedAt, IReadOnlyList<OrderLine> Lines);
```

Only entry points need marking. Everything reachable from a marked type — through properties, collection
elements, and both halves of a dictionary — is carried into the same document, so a graph is exported by
marking its root. A type may be marked more than once to publish it into several documents.

The documents are regenerated on every build of the project that opts in, one per distinct file name, in
[JSON Schema Draft 2020-12](https://json-schema.org/draft/2020-12/schema). The assemblies are read
metadata-only: no code from your project is executed to produce them.

## Requirements

- **.NET 10 SDK** for the host build. The export tool ships pre-built inside the package and runs on the
  .NET 10 runtime, whatever the host itself targets.
- **`net10.0` or later** for every project that references the package. The attribute assembly targets
  `net10.0` only, so a library on an earlier framework restores the package and then fails to compile
  `using DotNetSchema;` with `CS0246`.

## Getting started

Reference the [`DotNetSchema` package](https://www.nuget.org/packages/DotNetSchema/) wherever you declare models:

```xml
<PackageReference Include="DotNetSchema" Version="0.1.0-alpha.3" />
```

Then opt in, in the **host** project — the one that should own the documents:

```xml
<PropertyGroup>
  <DotNetSchemaGenerate>true</DotNetSchemaGenerate>
</PropertyGroup>
```

That is the whole setup. The build integration ships under `buildTransitive/`, so a host that references
your model library (by `ProjectReference`) receives it through that library without a `PackageReference`
of its own, and nothing needs an `<Import>`. On every build the host scans its own assembly **and every
project it references, directly or transitively**, and writes the documents next to its output.

Projects that do not set `DotNetSchemaGenerate` — your model libraries, typically — export nothing. They
only carry the attribute.

| Layout | What to do |
| --- | --- |
| Models and host in one project | `PackageReference` + `DotNetSchemaGenerate=true` in that project. |
| Models in a library, host references it | `PackageReference` in the library; `DotNetSchemaGenerate=true` in the host. |
| Models spread over several libraries | Same: the host scans its whole project-reference closure. |

Models that reach the host as **packages** rather than projects are not scanned.

## Configuring it

Every one of these is an ordinary MSBuild property, set in the host `.csproj` (or a `Directory.Build.props`).

| Property | Default | What it does |
| --- | --- | --- |
| `DotNetSchemaGenerate` | `false` | Set `true` in the project that should produce the documents. |
| `DotNetSchemaOutputPath` | `$(OutDir)` | Directory the documents are written to. A relative path is resolved against the project directory. |
| `DotNetSchemaDefaultFileName` | `$(AssemblyName).schema.json` | Document that a bare `[DotNetSchema]` exports to. |
| `DotNetSchemaScanProjectReferences` | `true` | Set `false` to export only this project's own marked types. |
| `DotNetSchemaIncludeInPublish` | `true` | Whether the documents are also copied to `$(PublishDir)`. |
| `DotNetSchemaFailOnEmpty` | `false` | Fail the build when nothing is marked. Worth turning on in CI. |
| `DotNetSchemaEnumStyle` | `OneOfConstTitle` | How enum values carry their member names: `OneOfConstTitle` (a `oneOf` of titled constants, standard), `EnumWithNames` (`enum` plus the `x-enumNames` vendor keyword) or `Enum` (values only). |
| `DotNetSchemaToolPath` | *(empty)* | Escape hatch: run a specific prebuilt export tool. |

Single values are properties; lists are items, one element per entry, so they need no separator syntax and keep
the order they were written in:

| Item | What it does |
| --- | --- |
| `DotNetSchemaDecorator` | An ordered rule choosing which attributes are exposed under `x-decorators`. See [Attributes](#attributes-x-decorators). |
| `DotNetSchemaType` | Describes a type inline by a schema you declare. See [Custom type mappings](#custom-type-mappings). |

For example, to keep the documents in the source tree where a front end can pick them up:

```xml
<PropertyGroup>
  <DotNetSchemaGenerate>true</DotNetSchemaGenerate>
  <DotNetSchemaOutputPath>schemas</DotNetSchemaOutputPath>                 <!-- <project>/schemas/ -->
  <DotNetSchemaDefaultFileName>api.schema.json</DotNetSchemaDefaultFileName>
</PropertyGroup>
```

Writes are idempotent — a document is only rewritten when its content changes — and a document that is no
longer produced (say, after renaming the file in an attribute) is deleted from the output directory on the
next build. If you point `DotNetSchemaOutputPath` into a project that uses the Web SDK, exclude that
folder from `Content`, or the documents will be published twice.

The produced files are exposed as `@(DotNetSchemaOutput)` for any target ordered after
`_DotNetSchemaPrepare`.

## What the documents look like

A definition bundle. Every type — the marked ones and everything they reach — is an entry in `$defs`, and
nothing is inlined except the framework's primitives and the types you [map](#custom-type-mappings):

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "urn:dotnetschema:schema:school",
  "title": "school",
  "$comment": "Definition bundle. Exported types: School. Reference one as #/$defs/<name>.",
  "$defs": {
    "Person": {
      "type": "object",
      "properties": {
        "role":   { "$ref": "#/$defs/PersonRole" },
        "mentor": { "anyOf": [ { "$ref": "#/$defs/Person" }, { "type": "null" } ] }
      },
      "required": [ "role" ]
    }
  }
}
```

Three things that shape everything else:

**A model is never described as a bare `object`.** If a property could be a reference to another
definition in the same document, it is one — unless you have mapped its type to an inline schema. Recursion needs no special handling and there is no depth
limit — `Person.mentor` pointing back at `#/$defs/Person` is a complete answer.

**Each document is self-contained.** A type reachable from two documents is defined in both. There are no
cross-document `$ref`s, so a file can be handed to a code generator on its own.

**The schema describes what System.Text.Json writes**, not how the C# is declared. Property names are
camelCase; `[JsonPropertyName]`, `[JsonIgnore]`, `[JsonPropertyOrder]` and `[JsonRequired]` are honoured;
and where the serialiser's output does not match the obvious JSON Schema keyword, the document says what
is true rather than what is tidy. See [the generator's README](https://github.com/ofzza/DotNetSchema/blob/master/src/DotNetSchema.Generator/README.md)
for the full mapping table and the three places that bites. There are two deliberate exceptions: the
keywords mapped from validation attributes, described [below](#attributes-x-decorators), and
[custom type mappings](#custom-type-mappings), where you state what your serialiser writes.

### Attributes: `x-decorators`

Attributes on a model, an enum or a property are listed under the vendor keyword `x-decorators`, with
their constructor arguments (by parameter name) and named arguments. The well-known validation and
description attributes are also translated into the standard keywords that say the same thing:

```csharp
[Range(1, 99)] public required byte Edition { get; init; }
```

```json
"edition": {
  "type": "integer", "minimum": 1, "maximum": 99,
  "x-decorators": [
    { "name": "System.ComponentModel.DataAnnotations.RangeAttribute",
      "arguments": { "minimum": 1, "maximum": 99 } }
  ]
}
```

Which attributes take part is decided by **rules applied in order**: first the default ones, then your
`DotNetSchemaDecorator` items, in the order they are written. For each attribute, **the last rule that matches
its full name decides** whether it is included; an attribute no rule matches is left out.

```xml
<ItemGroup>
  <!-- Our own attributes as well, but no length limits. -->
  <DotNetSchemaDecorator Include="true"  Attribute="MyCompany.Models.Annotations.*" />
  <DotNetSchemaDecorator Include="false" Attribute="System.ComponentModel.DataAnnotations.StringLengthAttribute" />
</ItemGroup>
```

`Include` is `true` or `false`; anything else fails the build (`DNS0000`). `Attribute` is an attribute's full
name, in which `*` matches any run of characters — **dots included**, so a namespace pattern reaches its
sub-namespaces too:

| Pattern | Matches |
| --- | --- |
| `Ns.FooAttribute` | that attribute (a nested one is `Ns.Outer+FooAttribute`) |
| `Ns.*` | everything in `Ns` and in every namespace below it |
| `Ns.*.*` | only the namespaces below `Ns` — so `Ns.*` included, then `Ns.*.*` excluded, is "directly in `Ns`" |
| `*` | everything — excluding it clears every rule before it |

The default rules are, in order: include `System.ComponentModel.DataAnnotations.*`, exclude
`System.ComponentModel.DataAnnotations.*.*` (keeping Entity Framework's `DataAnnotations.Schema` mapping
attributes out), and include `DefaultValueAttribute`, `DescriptionAttribute` and `DisplayNameAttribute` from
`System.ComponentModel`, and `System.ObsoleteAttribute`. Because a later rule always wins, any of them can be
taken back, and anything can be brought back after it was excluded — start from nothing, include a namespace,
exclude part of it, then re-include one member:

```xml
<ItemGroup>
  <DotNetSchemaDecorator Include="false" Attribute="*" />
  <DotNetSchemaDecorator Include="true"  Attribute="MyCompany.Annotations.*" />
  <DotNetSchemaDecorator Include="false" Attribute="MyCompany.Annotations.Internal.*" />
  <DotNetSchemaDecorator Include="true"  Attribute="MyCompany.Annotations.Internal.AuditedAttribute" />
</ItemGroup>
```

Items in `Directory.Build.props` come before the project's own, and items in `Directory.Build.targets` after,
so a shared baseline with per-project adjustments works as you would read it. The pattern goes in `Attribute`,
never in `Include`: MSBuild expands an `Include` containing `*` as a file pattern, and one matching no file
silently disappears.

An attribute left out yields neither its `x-decorators` entry nor its keywords. Compiler attributes, the
System.Text.Json attributes (already reflected in the schema) and this package's own are never listed,
whatever the rules say.

Two things worth knowing:

- **The mapped keywords describe validation the model declares, which System.Text.Json does not enforce.**
  A `[Range(1, 10)]` property holding `42` serialises without complaint and then fails the schema. Exclude the
  attribute if a schema consumer must accept everything the serialiser can produce.
- **DataAnnotations `[Required]` does not make a property `required`.** It is a non-null, non-empty check
  run after deserialising, while JSON Schema's `required` is a presence check. Use the C# `required`
  modifier or `[JsonRequired]` — System.Text.Json enforces both, and both are reflected.

On a positional record, an attribute written without a `property:` target lands on the constructor
parameter; it is read from there too. The full rules — value rendering, the keyword table, placement — are
in [the generator's README](https://github.com/ofzza/DotNetSchema/blob/master/src/DotNetSchema.Generator/README.md#decorators).

## Custom type mappings

A type serialised by a custom `JsonConverter` — a strongly-typed identifier, a money amount, a code — does not
look like its members on the wire, so describing it as an object definition would be wrong. Declare what it is
written as instead, and it is described inline wherever it appears (property, array element, dictionary value,
nullable or not), with no `$ref` and no `$defs` entry:

```json
"studentNumber": { "type": ["string", "null"], "pattern": "^S\\d{7}$" }
```

> [!WARNING]
> **A mapping changes the schema only — never serialisation.** It is your statement of what your serialiser
> writes, and nothing checks it. Making the type actually serialise that way is your job: register a
> `JsonConverter` (on the type, in `JsonSerializerOptions.Converters`, or wherever your application wires them)
> that writes exactly the declared `type`, `format` and `pattern`. If the converter and the declaration
> disagree — or the converter is changed later and the declaration is not — the schema is wrong, silently.
> The export reads assemblies metadata-only and never runs your converter, so it cannot tell.
>
> In particular:
>
> - A `[JsonConverter]` on a single **property** wins over the type's converter at run time, but a mapping is
>   per type: the export cannot see that this one property writes something else.
> - Converters registered only in `JsonSerializerOptions` are invisible to the export. That is exactly why a
>   mapping is needed for them, and why it must be kept in step with them by hand.
> - **`[DotNetSchemaType]` travels with the type.** A library that declares one makes that promise to every
>   host that exports its models, whatever serialiser options that host uses.

There are two ways to declare a mapping.

**On the type**, when it is yours — keeping the declaration next to the converter it describes:

```csharp
[DotNetSchemaType("string", Pattern = @"^S\d{7}$")]
[JsonConverter(typeof(StudentNumberConverter))]
public readonly record struct StudentNumber(int Value);
```

**In the exporting project**, for any type — including one from a library you do not control, and including
the framework's own primitives:

```xml
<ItemGroup>
  <DotNetSchemaType Include="MyCompany.Models.Money" Type="string" Pattern="^-?\d+\.\d{2}$" />
  <DotNetSchemaType Include="MyCompany.Models.Id`1"  Type="string" Format="uuid" />
  <!-- 64-bit integers written as strings (JsonNumberHandling.WriteAsString) for JavaScript readers. -->
  <DotNetSchemaType Include="System.Int64" Type="string" Pattern="^-?\d{1,20}$" />
</ItemGroup>
```

| Field | Required | Meaning |
| --- | --- | --- |
| `Type` | yes | `string`, `number`, `integer` or `boolean`. |
| `Format` | no | The `format` keyword: a registered one (`uuid`, `date-time`, `email`, …) or your own, which readers that do not know it treat as an annotation. |
| `Pattern` | no | The `pattern` keyword, a regular expression; `string` only. Unanchored, as JSON Schema patterns are — add `^` and `$` to match the whole value. |

**Precedence:** a `DotNetSchemaType` item, then `[DotNetSchemaType]` on the type, then the built-in mapping of
the framework's primitives, then the type's shape. A project can therefore correct a library's declaration;
it cannot remove one and go back to an object definition. Of two items for the same type, the later wins, so
a project can override one set in a shared `Directory.Build.props` — or drop it with `Remove`.

**Naming the type.** `Include` is the type's full name as the runtime spells it, without the assembly:

- a nested type is `Outer+Inner`, not `Outer.Inner`;
- a generic type is named by its definition, with a backtick and its arity: `MyCompany.Models.Id`1`;
- in MSBuild, a literal `%` in a pattern is written `%25` (as anywhere in MSBuild); `;`, `*` and `|` need no
  escaping.

A misspelt name matches nothing and would leave the type described by its shape, so a configured mapping no
exported document reached is reported (`DNS0017`).

### Generic types

A mapping on a generic type — by item or by attribute on its definition — covers **every construction** of it:
`Id<Order>`, `Id<Customer>` and `Id<Invoice>` are all described by the one declaration, and there is no way to
map one construction differently. A generic type that would otherwise be refused as a model (`DNS0003`) is
accepted once it is mapped.

That fits the common case, and works best when you keep it that way:

- **Keep the wire shape independent of the type argument.** `Id<T>` should write the same kind of value
  whatever `T` is; if `Id<Order>` and `Id<Legacy>` are written differently, they should be different types.
- **Write one converter per generic definition**, a `JsonConverterFactory` that creates the converter for each
  construction, so the constructions cannot drift apart.
- **Prefer the attribute on the generic definition** to an item: it sits next to that factory, and every
  project using the type gets it.

### Warnings and errors

| Code | Severity | What it means | What to do |
| --- | --- | --- | --- |
| `DNS0016` | error | A mapping is malformed — a `Type` that is not one of the four, a `Pattern` on a non-string type or one that is not a valid regular expression, or an item with no type name. It is ignored. | Fix the item or the attribute named in the message. |
| `DNS0017` | warning | A `DotNetSchemaType` item matched no type any exported document reached. | Check the name: namespace, `+` for nested types, `` `N `` for generics. If the item is shared deliberately (say, from `Directory.Build.props`) and this project simply does not use the type, suppress it with `<MSBuildWarningsAsMessages>$(MSBuildWarningsAsMessages);DNS0017</MSBuildWarningsAsMessages>`. |
| `DNS0018` | error | A type marked `[DotNetSchema]` is also mapped. A document definition and an inline scalar cannot both be true. | Remove the mark or the mapping. |
| `DNS0019` | warning | A type that would be an object definition is written by a `[JsonConverter]` (on the type, or on the property) and has no mapping. Its shape is unknown, so it is described by the empty schema `{}`, which accepts anything. | Declare what the converter writes, with `[DotNetSchemaType]` or a `DotNetSchemaType` item. |
| `DNS0004` | error | A type of your own with a mapping that is not a `string` one is used as a dictionary key. Every JSON property name is a string, so only a string mapping can describe a key. (An enum or a framework primitive keeps its usual key form instead.) | Map the type as a `string` (with the pattern its keys match), or key the dictionary differently. |

`DNS0019` only sees converters named by `[JsonConverter]`. A converter registered in `JsonSerializerOptions`
leaves no trace in the assembly, so a type relying on one is described by its members, without a warning —
map it.

## Diagnostics

Reported in MSBuild's canonical form, so they arrive as ordinary build errors and warnings.

| Code | Severity | Meaning |
| --- | --- | --- |
| `DNS0001` | error | A marked type is not a concrete, non-generic class, record or struct. |
| `DNS0002` | error | A property's type has no JSON Schema mapping. |
| `DNS0003` | error | A generic type appeared in the closure. |
| `DNS0004` | error | A dictionary key System.Text.Json cannot write as a property name. |
| `DNS0005` | warning | Two types shared a short name; both definitions were qualified. |
| `DNS0006` | warning | Two properties of one type serialise under the same name. |
| `DNS0007` | error | The attribute names something that is not a bare `.json` file name. |
| `DNS0009` | warning | A property's enum converter disagrees with the enum type's; its schema is inlined. |
| `DNS0010` | warning | An assembly yielded types that could not be loaded; the rest were scanned. |
| `DNS0011` | warning | Reference-typed members with no nullability metadata; treated as non-nullable. |
| `DNS0012` | warning | A dictionary keyed by a floating-point type. |
| `DNS0014` | warning | A type's or property's attributes could not be read; its decorators were left out. |
| `DNS0015` | warning | A selected attribute's standard keyword does not fit the property (say, `[Range]` on a string); it stays in `x-decorators` only. |
| `DNS0016` | error | A type mapping is malformed and was ignored. See [Custom type mappings](#warnings-and-errors). |
| `DNS0017` | warning | A `DotNetSchemaType` item matched no type any exported document reached. |
| `DNS0018` | error | A type is both marked for export and mapped. |
| `DNS0019` | warning | A model position is written by a `[JsonConverter]` and has no mapping; it is described by `{}`. |
| `DNS0000` | error | The tool itself failed; the message carries the exception. |
| `DNS0013` | error | Nothing was marked, and `DotNetSchemaFailOnEmpty` is set. |
| `DNS1001` | error | The export tool could not be located. Raised by the targets, not the tool. |

An error withholds the document it concerns and leaves the other documents alone.

## Developing in this repository

MSBuild only auto-imports a package's build targets for a `PackageReference`, never for a
`ProjectReference`, so projects inside this repository import them by hand:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\src\DotNetSchema\DotNetSchema.csproj" />
</ItemGroup>

<Import Project="..\..\src\DotNetSchema\buildTransitive\DotNetSchema.targets" />
```

On that path there is no pre-built tool, so the targets add a build-only `ProjectReference` to
`DotNetSchema.Tool` and build it as part of the consumer's build. A clean clone builds with a plain
`dotnet build DotNetSchema.slnx`. The samples (`samples/DotNetSchema.Sample.Models` and
`samples/DotNetSchema.Sample.Host`) are the worked example of the library/host split.

## Releasing the package

`./scripts/pack.sh` packs it into `artifacts/packages` (extra arguments go to `dotnet pack`, e.g.
`-p:Version=0.2.0`). The package carries the attribute under `lib/`, the build integration under
`buildTransitive/`, and the export tool, pre-published, under `tools/net10.0/any/`.

The ordinary build never exercises the packaged path, so check it by hand before releasing:

1. `./scripts/pack.sh`
2. In a scratch directory outside the repository, add a `nuget.config` pointing at `artifacts/packages`,
   a `net10.0` library with a `PackageReference` to `DotNetSchema` and one `[DotNetSchema]` record, and
   a host project that references the library and sets `DotNetSchemaGenerate` to `true`.
3. `dotnet build` the host. `<Host>.schema.json` must appear in the host's `bin/`, nothing in the
   library's, and there must be no `<Import>` line anywhere.

## Layout

```
DotNetSchema/
  DotNetSchema.csproj          the attribute, and the packaging that ships the tool
  DotNetSchemaAttribute.cs
  DotNetSchemaTypeAttribute.cs [DotNetSchemaType]: a type's declared inline schema
  buildTransitive/
    DotNetSchema.props         overridable defaults; imported first on the package path
    DotNetSchema.targets       the export target; imports the .props if it has not been
  README.md
```

| Project | What it is |
| --- | --- |
| **`DotNetSchema`** | This one. The attribute, the MSBuild integration and the packaging. No dependencies; the only assembly a consumer references. |
| [`DotNetSchema.Generator`](https://github.com/ofzza/DotNetSchema/blob/master/src/DotNetSchema.Generator/README.md) | The engine. Reads assemblies, produces documents. |
| [`DotNetSchema.Tool`](https://github.com/ofzza/DotNetSchema/blob/master/src/DotNetSchema.Tool/README.md) | The command-line shell the build runs. |

## Tests

The engine is covered by
[`DotNetSchema.Generator.Tests`](https://github.com/ofzza/DotNetSchema/blob/master/tests/DotNetSchema.Generator.Tests/README.md); the build integration
is covered end to end by `DotNetSchema.Fixtures` and the samples, which export documents on every build.

## License

MIT — see [LICENSE.md](https://github.com/ofzza/DotNetSchema/blob/master/LICENSE.md).
