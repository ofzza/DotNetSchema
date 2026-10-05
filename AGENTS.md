# AGENTS.md

Guidance for coding agents (Claude Code and others) working in this repository.

## Keeping this file and the READMEs current

**`AGENTS.md` and the `README.md` files are part of the deliverable of every task, not separate chores.** Whenever a change makes something here or in a README inaccurate, update it in the same change — never leave it for later, and never finish a task reporting only the code change.

The detailed documentation lives in the per-project READMEs (see [Project](#project)); this file summarises the rules and points at them. Do not copy README content in here — copies drift. When a fact belongs in a README, put it there and link to it.

Concrete triggers:

| A change that ...                                                              | ... requires updating                                                                                                                                   |
| ------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------- |
| adds, renames or changes the default of an MSBuild property (`buildTransitive/`) | `src/DotNetSchema/README.md` "Configuring it", the root `README.md` snippet if the property appears there, and the sample csproj comments if they mention it |
| adds or retires a `DNS` diagnostic code                                        | `SchemaDiagnosticCodes.cs` (or `Program.cs` / `.targets` for tool / targets codes), the Diagnostics table in `src/DotNetSchema/README.md`, a test in `JsonSchemaGeneratorTests.Diagnostics.cs`, and [Architecture rules](#architecture-rules-that-must-not-be-broken) below |
| changes how a C# type maps to a schema                                         | the mapping table in `src/DotNetSchema.Generator/README.md`, the golden digests (see [Testing](#testing)), and `JsonSchemaValidator` if a new keyword is emitted |
| adds or changes an export tool argument                                        | the Arguments table in `src/DotNetSchema.Tool/README.md`, and the `_DotNetSchemaArgument` items in `DotNetSchema.targets`                                |
| changes the public surface of `DotNetSchema.Generator` or `DotNetSchema.Tool`  | that project's README (the Tool's "Public surface" section treats its signatures as API)                                                                |
| adds, removes or renames a file, directory or project                          | the Layout block of that project's README, [Layout](#layout) below, the root `README.md` path table, and `DotNetSchema.slnx` for a project               |
| adds or changes a fixture model                                                | `tests/DotNetSchema.Fixtures/README.md` (Coverage, Pseudocode, Graph, Schema export tables), and the golden digests                                     |
| changes a build, test or pack command, the CI workflow, the TFM or the SDK      | [Commands](#commands) / [Continuous integration](#continuous-integration) below, and the root `README.md`                                               |
| bumps the package version                                                      | `<Version>` in `src/DotNetSchema/DotNetSchema.csproj` **and** every `Version="0.1.0"` snippet: root `README.md`, `src/DotNetSchema/README.md`, the comment in `samples/DotNetSchema.Sample.Models/DotNetSchema.Sample.Models.csproj` |
| changes packaging, the package contents, the release process or the publish target | `PUBLISH.md` (checklists, content list, smoke test, steps)                                                                                             |
| establishes a new convention, or hits a new gotcha                             | [Code style](#code-style) / [Gotchas and known issues](#gotchas-and-known-issues)                                                                       |
| resolves one of the known issues listed below                                  | remove it from [Gotchas and known issues](#gotchas-and-known-issues)                                                                                    |

Any C# sample added to a README should compile against the current API — check it in a scratch project (or a throwaway test) before committing.

## Project

DotNetSchema — a NuGet package that exports `[DotNetSchema]`-marked .NET records and classes to JSON Schema (Draft 2020-12) on every build of a host project that opts in with `DotNetSchemaGenerate=true`. MIT, package id **`DotNetSchema`**, version `0.1.0`, everything targets `net10.0`. Assemblies are read metadata-only (`MetadataLoadContext`), so no consumer code runs during export.

| Project                                                            | Role                                                                                                   | Detailed docs                                       |
| ------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------ | --------------------------------------------------- |
| `src/DotNetSchema`                                                 | The package: the attribute, `buildTransitive/` MSBuild integration, packaging of the pre-built tool. The only assembly a consumer references. | `src/DotNetSchema/README.md` (user-facing)          |
| `src/DotNetSchema.Generator`                                       | The engine: assemblies in, `SchemaDocument`s + `SchemaDiagnostic`s out. Pure.                          | `src/DotNetSchema.Generator/README.md`              |
| `src/DotNetSchema.Tool`                                            | The console shell MSBuild runs (`dotnet exec`): metadata-only load, idempotent writes, manifest, diagnostics in MSBuild format. | `src/DotNetSchema.Tool/README.md`                   |
| `tests/DotNetSchema.Fixtures`                                      | The "school" model graph every test runs over, a deterministic instance generator, and a live in-repo consumer of the build integration. | `tests/DotNetSchema.Fixtures/README.md`             |
| `tests/DotNetSchema.Generator.Tests`                               | xUnit v3 tests for the generator.                                                                      | `tests/DotNetSchema.Generator.Tests/README.md`      |
| `samples/DotNetSchema.Sample.Models` + `samples/DotNetSchema.Sample.Host` | The worked example of the model-library / host split, with a custom `DotNetSchemaOutputPath`.         | comments in their csproj files                      |

Read the relevant README before changing a project — they record *why* things are the way they are, and most non-obvious choices are explained there or in comments in the MSBuild files.

Toolchain last verified against: .NET SDK 10.0.112, xunit.v3 3.2.2, Microsoft.NET.Test.Sdk 18.9.0, System.Reflection.MetadataLoadContext 10.0.1.

## Branching

- **`develop` is the work-in-progress trunk.** Create every feature, fix or dependency branch from `develop`, and open every pull request against `develop`.
- **`master` holds only the latest stable release.** It is never committed to directly and never receives feature pull requests — it is only updated by merging `develop` into it when a stable version is released.
- Pull request titles follow `.github/pull_request_template.md`: `Issue #NN, Copied issue title`, with Changes / Testing / Impact sections.

## Layout

```
DotNetSchema.slnx          Solution (slnx format): /src/, /tests/, /samples/ folders
src/
  DotNetSchema/            Attribute + buildTransitive/DotNetSchema.{props,targets} + packing of the tool under tools/net10.0/any/
  DotNetSchema.Generator/  Reflection/ (metadata-only layer), Model/ (C# types to schema shapes), Emit/ (writer), Diagnostics/
  DotNetSchema.Tool/       Program, ToolArguments, MetadataLoadContextFactory, SchemaFileWriter
tests/
  DotNetSchema.Fixtures/   Schema/ (records, Enums/, Invalid/), Generation/ (FixturesService, StableRandom, ...)
  DotNetSchema.Generator.Tests/  JsonSchemaGeneratorTests.<Contract>.cs partials, JsonSchemaValidator, SchemaDigest
samples/                   Models library + Host app; Host writes into samples/DotNetSchema.Sample.Host/schemas/ (gitignored)
scripts/pack.sh            Packs the package into artifacts/packages
PUBLISH.md                 Release checklists and the steps to pack and push a version to nuget.org
artifacts/                 Pack output, gitignored
.github/                   CI workflow, dependabot, pull request template
```

Each project's README has the file-level layout; keep it, not this tree, as the detailed one.

## Commands

- `dotnet build DotNetSchema.slnx` — builds everything. Building `tests/DotNetSchema.Fixtures` and `samples/DotNetSchema.Sample.Host` also runs the in-repo export integration end to end (the tool is built through a build-only `ProjectReference` the targets inject).
- `dotnet test DotNetSchema.slnx` — runs the generator tests (about a second). A single project: `dotnet test tests/DotNetSchema.Generator.Tests`.
- `./scripts/pack.sh [extra dotnet pack args, e.g. -p:Version=0.2.0]` — packs `src/DotNetSchema` in Release into `artifacts/packages`.
- Regenerate golden digests (only after an *intended* output change): `dotnet build tests/DotNetSchema.Fixtures` then `sha256sum tests/DotNetSchema.Fixtures/bin/Debug/net10.0/{assessment,schema,school}.json`, and paste into `JsonSchemaGeneratorTests.Goldens.cs`.
- Debug the tool by hand: `dotnet exec DotNetSchema.Tool.dll @obj/Debug/net10.0/DotNetSchema/export.rsp` from a consumer project directory.
- **The ordinary build never exercises the packaged path.** Before a release, or after touching packaging or `buildTransitive/`, run the consumer smoke test in `PUBLISH.md` (scratch library + host outside the repo, `nuget.config` pointing at `artifacts/packages`, private `NUGET_PACKAGES`).
- **Releasing:** follow `PUBLISH.md` — checklists before the first and every publish, then version bump, tag, fast-forward `master`, pack and `dotnet nuget push`.

## Architecture rules that must not be broken

Summaries — the reasoning is in the READMEs named.

- **No runtime-type comparisons in `DotNetSchema.Generator`** (Generator README, "The one rule"). No `typeof(X) == t`, `IsAssignableTo`, `is X`, `Nullable.GetUnderlyingType`, `GetCustomAttribute<T>`, `Enum.GetValues`, `NullabilityInfoContext`. They silently return wrong answers for `MetadataLoadContext` types. Ask through `Reflection/MetadataFacts.cs` (`FullName` comparisons, `CustomAttributeData`, `GetRawConstantValue()`). Enforced by `ReadingTypesForMetadataOnly_ProducesTheSameBytesAsReadingThemAtRuntime`.
- **The generator is pure.** No console, no file system, no throwing because the input was wrong — every problem is a `SchemaDiagnostic`. An error withholds only the document it concerns.
- **Output is byte-deterministic** (Generator README, "Determinism"): `$defs` ordinal by name; properties by `[JsonPropertyOrder]` then `MetadataToken`; enum members by value then `MetadataToken`; `\n` newlines; `UnsafeRelaxedJsonEscaping`; one trailing newline; UTF-8 **without** BOM. Never introduce ordering from `GetProperties()` / dictionary iteration, timestamps or `Environment.NewLine`.
- **The schema describes what System.Text.Json actually writes**, not what the C# declaration suggests (camelCase, `[JsonPropertyName]` / `[JsonIgnore]` / `[JsonPropertyOrder]`, the `TimeOnly` / `TimeSpan` / `DateTime` caveats, enums numeric as values but names as dictionary keys). A model is always a `$ref` into `$defs`, never a bare `object`; each document is self-contained (no cross-document `$ref`). Nullable `$ref`/enum widens via `anyOf` with `{"type":"null"}`; nullable inline scalars via the `type` array. `required` follows only the C# `required` modifier.
- **Diagnostic codes are permanent** — retire a code, never reuse one. `DNS0008` is unassigned; leave the gap. `DNS0001`–`DNS0012` come from the generator (`SchemaDiagnosticCodes.cs`), `DNS0000` and `DNS0013` from the tool (`Program.cs`), `DNS1001` from `DotNetSchema.targets`. All are printed in MSBuild canonical form so `Exec` promotes them.
- **MSBuild integration** (comments in `DotNetSchema.props` / `.targets`):
  - Two delivery paths must both keep working: the **package** (auto-imported, pre-built tool under `tools/`) and **in-repo** (`<Import>` by hand next to a `ProjectReference`, tool built via an injected build-only `ProjectReference`).
  - Every default in `.props` carries a `Condition` — it is imported *after* the csproj body on the in-repo path, so an unconditional default would override the user.
  - Anything derived from SDK-computed values (`$(OutDir)`, `$(IntermediateOutputPath)`, `$(AssemblyName)`, `$(DOTNET_HOST_PATH)`) is computed inside `_DotNetSchemaPrepare`, not at import time.
  - `_DotNetSchemaPrepare` always runs and reads the manifest; keep it out of the incremental `DotNetSchema` target, or `IncrementalClean` deletes the documents on the second build.
  - The export target hooks `AfterTargets="CopyFilesToOutputDirectory"` (not `Build`) so its `FileWrites` land before `IncrementalClean`.
  - Nothing runs unless `DotNetSchemaGenerate` is `true`; design-time builds never run the tool.
  - Internal properties, items and targets are `_DotNetSchema`-prefixed; public ones are `DotNetSchema*`.
- **The tool's public types are API**: `MetadataLoadContextFactory`, `SchemaFileWriter`, `ToolArguments`. Writes are idempotent (unchanged bytes are not rewritten), stale documents listed in the previous manifest are pruned, and deletion stays confined to the output directory.
- **`src/DotNetSchema` has no dependencies.** It is the only assembly consumers reference; the generator references it only to take the attribute's name from `typeof(...).FullName`.

## Testing

xUnit v3 (`using Xunit` is a global `<Using>` in the test csproj). Conventions, from `tests/DotNetSchema.Generator.Tests/README.md`:

- **`JsonSchemaGeneratorTests` is one `sealed partial class`, one file per contract** (Discovery, Mapping, Nullability, Recursion, Determinism, Goldens, Diagnostics, Validates). A new test goes in the file whose contract it tests; shared helpers live in `JsonSchemaGeneratorTests.cs`.
- Test names are `[Fact]` methods named as a sentence split by underscores: `Subject_Behaviour` / `Subject_Behaviour_AndMore`, e.g. `AnUnmarkedAssembly_ProducesNothingRatherThanAnEmptyDocument`.
- **Never declare model types inside a test.** Every shape tested lives in `DotNetSchema.Fixtures` — valid ones in the school graph, invalid ones in `Schema/Invalid/` marked `[ExportForTesting]` (not `[DotNetSchema]`, which would export them and break the Fixtures build). Tests reach them by swapping `SchemaGenerationOptions.MarkerAttributeFullName`.
- **One test per `DNS` code** in `.Diagnostics.cs`. `DNS0010` and `DNS0011` are knowingly untested (see that README).
- **Goldens are never edited to make a test pass.** If the change was meant to move the output, regenerate (see [Commands](#commands)) and say why in the commit message; if not, the change is wrong. Any change to the fixtures schema moves the digests too.
- `JsonSchemaValidator` is hand-rolled and covers exactly the keywords the generator emits; `TheValidator_ImplementsEveryKeywordTheGeneratorEmits` fails when the emitter grows a keyword — extend the validator rather than reaching for a third-party one.
- Fixtures must stay deterministic across machines and runtimes: draw from `StableRandom` (SplitMix64), never `System.Random`; use `CultureInfo.InvariantCulture`; derive seeds by path (`FixtureSeed`). Adding marking attributes to fixtures must not change any generated value.
- `DotNetSchema.Tool` has no test project, deliberately — it is covered by the Fixtures / samples builds and the manual package check.

## Code style

There is no `.editorconfig` in this repo; follow the surrounding code.

- 2-space indentation in C# and MSBuild/XML. File-scoped namespaces matching the folder (`DotNetSchema.Generator.Model`). `ImplicitUsings` and `Nullable` enabled in every project.
- Classes `sealed` unless designed for inheritance; `record`s for data; `static` for stateless helpers. Collection expressions (`[]`, `[a, b]`), `with` expressions, expression-bodied members where short.
- Always pass `StringComparer.Ordinal` / `StringComparison.Ordinal` explicitly.
- XML doc comments (`<summary>`, `<remarks>`, `<see cref>`, `<inheritdoc />`) on public members and on non-obvious private ones. Comments are full sentences explaining *why*, not *what*; tests carry such comments too.
- MSBuild files: every non-obvious choice gets an explanatory `<!-- -->` comment in the same voice as the existing ones (what it does, why the obvious alternative is wrong).
- Encoding: `.cs` / `.csproj` files in `DotNetSchema.Generator`, `DotNetSchema.Tool` and both `tests/` projects start with a UTF-8 BOM; `src/DotNetSchema`, the samples, Markdown and scripts do not. Match the file you edit, and keep the BOM on new files in the BOM-carrying projects. (Generated schema documents never carry a BOM.)
- Prose in READMEs and comments uses British spelling (serialise, initialiser, behaviour).
- Commit messages: imperative summary line; explain *why* when a golden digest moves.

## Gotchas and known issues

- **Stale paths in two READMEs**, left over from before the code was extracted into this repo: `tests/DotNetSchema.Generator.Tests/README.md` gives `dotnet test src/dotnet/Core/ExportJsonSchema/DotNetSchema.Generator.Tests` (correct: `dotnet test tests/DotNetSchema.Generator.Tests`), and `src/DotNetSchema.Generator/README.md` "Determinism" refers to `src/dotnet/.editorconfig`, which does not exist here.
- **MSBuild only auto-imports a package's targets for a `PackageReference`, never a `ProjectReference`.** Every in-repo consumer (`tests/DotNetSchema.Fixtures`, both samples) must `<Import>` `src/DotNetSchema/buildTransitive/DotNetSchema.targets` by hand.
- **`tests/DotNetSchema.Fixtures` pins `DotNetSchemaDefaultFileName` to `schema.json`** so its `bin/` output equals `SchemaGenerationOptions.Default` and the golden digests. Don't change one without the other.
- **Packing ships the tool's *publish* output**, not its build output — `tools/` is not a restore target, so `System.Reflection.MetadataLoadContext.dll` must be physically present. `PackagePath`s for `buildTransitive/` files are full file paths, not directories (a trailing separator nests them as `buildTransitive/buildTransitive/`). Use `None Update`, not `Include`, for files the SDK glob already matches (NETSDK1022).
- **Never scan via `@(ReferencePathWithRefAssemblies)`** — it offers a ref assembly with the same identity as the implementation assembly and the resolver refuses it. The tool also deduplicates references by assembly name (highest version wins).
- **Paths containing `;` are unsupported** — MSBuild splits item lists on them.
- **A Web SDK host with an in-tree `DotNetSchemaOutputPath` must exclude that folder from `Content`**, or the documents are published twice.
- **`System.Reflection.MetadataLoadContext`'s major version tracks the TFM.** Dependabot ignores its semver-major updates; bump it by hand together with the TFM, in both `DotNetSchema.Tool.csproj` and `DotNetSchema.Generator.Tests.csproj`.
- **Models that reach the host as packages (not projects) are not scanned** — only the host's own assembly and its project-reference closure.
- **`*.gitignore.*` files and `*.gitignore/` directories are local-only** (e.g. `TODO.gitignore.md`) and ignored by `.gitignore` — never commit them. An empty stray `DotNetSchema/` directory sits at the repo root (git ignores empty directories).

## Continuous integration

`.github/workflows/ci.yml` runs on every pull request targeting `master` or `develop` and on every push to either, on an `[ubuntu-latest, windows-latest]` matrix with `fail-fast: false` (both path flavours, because the integration resolves paths and starts the tool from MSBuild). Steps: `dotnet restore DotNetSchema.slnx` → `dotnet build -c Release --no-restore` → `dotnet test -c Release --no-build` → `./scripts/pack.sh`; the `.nupkg` is uploaded as the `packages` artifact from the ubuntu leg. In-progress runs are cancelled only for pull requests.

`.github/dependabot.yml`: weekly NuGet updates grouped into one pull request (MetadataLoadContext majors ignored), weekly github-actions updates. **It sets no `target-branch`**, so Dependabot opens pull requests against the default branch rather than `develop`.

**A workflow alone does not block merges** — that needs branch protection on `master` / `develop` in the GitHub repo settings with the `CI / ubuntu-latest` and `CI / windows-latest` checks required.
