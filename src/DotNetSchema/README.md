# DotNetSchema

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

## Getting started

Reference the package wherever you declare models:

```xml
<PackageReference Include="DotNetSchema" Version="0.1.0" />
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
| `DotNetSchemaToolPath` | *(empty)* | Escape hatch: run a specific prebuilt export tool. |

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
nothing is inlined:

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
definition in the same document, it is one. Recursion needs no special handling and there is no depth
limit — `Person.mentor` pointing back at `#/$defs/Person` is a complete answer.

**Each document is self-contained.** A type reachable from two documents is defined in both. There are no
cross-document `$ref`s, so a file can be handed to a code generator on its own.

**The schema describes what System.Text.Json writes**, not how the C# is declared. Property names are
camelCase; `[JsonPropertyName]`, `[JsonIgnore]` and `[JsonPropertyOrder]` are honoured; and where the
serialiser's output does not match the obvious JSON Schema keyword, the document says what is true rather
than what is tidy. See [the generator's README](../DotNetSchema.Generator/README.md) for
the full mapping table and the three places that bites.

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
  buildTransitive/
    DotNetSchema.props         overridable defaults; imported first on the package path
    DotNetSchema.targets       the export target; imports the .props if it has not been
  README.md
```

| Project | What it is |
| --- | --- |
| **`DotNetSchema`** | This one. The attribute, the MSBuild integration and the packaging. No dependencies; the only assembly a consumer references. |
| [`DotNetSchema.Generator`](../DotNetSchema.Generator/README.md) | The engine. Reads assemblies, produces documents. |
| [`DotNetSchema.Tool`](../DotNetSchema.Tool/README.md) | The command-line shell the build runs. |

## Tests

The engine is covered by
[`DotNetSchema.Generator.Tests`](../../tests/DotNetSchema.Generator.Tests/README.md); the build integration
is covered end to end by `DotNetSchema.Fixtures` and the samples, which export documents on every build.

## License

MIT — see [LICENSE.md](../../LICENSE.md).
