# AGENTS.md

Guidance for coding agents (Claude Code and others) working in this repository.

**Read [`.workflow/SPEC.md`](./.workflow/SPEC.md) before changing code.** It is the canonical statement of what DotNetSchema is and must do: its HARD RULES, the output contract (mapping table, nullability, enums, decorators, type mappings, property formats, determinism), the diagnostics, the MSBuild integration and the tool, the limits and the pitfalls. It isn't loaded automatically; this file doesn't repeat it.

## Keeping this file and the READMEs current

Documentation and tests are part of the deliverable of every task (see [WORKFLOW.md](./WORKFLOW.md) › "Documentation and tests"). This section says which ones a change touches: `.workflow/SPEC.md`, `AGENTS.md`, `CONTRIBUTE.md`, the `README.md` files and the tests (in the matching test project under `tests/`, with the shapes in `DotNetSchema.Fixtures`, see [Testing](#testing)).

The detailed documentation lives in the per-project READMEs (see [Project](#project)); this file points at them and at the specification. Do not copy their content in here — copies drift. When a fact belongs in a README or the specification, put it there and link to it.

Concrete triggers:

| A change that ...                                                                                                                         | ... requires updating                                                                                                                                                                                                                                                                               |
| ----------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| changes behaviour or code                                                                                                                 | the tests covering it, and `.workflow/SPEC.md` if it states the behaviour                                                                                                                                                                                                                           |
| adds, renames or changes the default of an MSBuild property or item (`buildTransitive/`)                                                  | `src/DotNetSchema/README.md` "Configuring it", `.workflow/SPEC.md` › "MSBuild integration", the root `README.md` snippet if the property appears there, and the sample csproj comments if they mention it                                                                                           |
| adds or retires a `DNS` diagnostic code                                                                                                   | `SchemaDiagnosticCodes.cs` (or `Program.cs` / `.targets` for tool / targets codes), the Diagnostics table in `src/DotNetSchema/README.md`, the Diagnostics table in `.workflow/SPEC.md`, and a test in `JsonSchemaGeneratorTests.Diagnostics.cs`                                                    |
| changes how a C# type maps to a schema                                                                                                    | the mapping table in `src/DotNetSchema.Generator/README.md`, the mapping table in `.workflow/SPEC.md`, the golden digests (see [Testing](#testing)), and `JsonSchemaValidator` if a new keyword is emitted                                                                                          |
| adds or changes an export tool argument                                                                                                   | the Arguments table in `src/DotNetSchema.Tool/README.md`, the `_DotNetSchemaArgument` items in `DotNetSchema.targets`, and the argument list in `.workflow/SPEC.md` › "MSBuild integration"                                                                                                         |
| changes the public surface of `DotNetSchema.Generator` or `DotNetSchema.Tool`                                                             | that project's README (the Tool's "Public surface" section treats its signatures as API)                                                                                                                                                                                                            |
| adds, removes or renames a file, directory or project                                                                                     | the Layout block of that project's README, [Layout](#layout) below, the root `README.md` path table, and `DotNetSchema.slnx` for a project                                                                                                                                                          |
| adds or changes a fixture model                                                                                                           | `tests/DotNetSchema.Fixtures/README.md` (Coverage, Pseudocode, Graph, Decorators, Schema export tables), and the golden digests                                                                                                                                                                     |
| changes a build, test or pack command, the CI workflow, the TFM or the SDK                                                                | [Commands](#commands) / [Continuous integration](#continuous-integration) below, the root `README.md`, and `.workflow/SPEC.md` if it states it                                                                                                                                                      |
| bumps the package version                                                                                                                 | `<Version>` in `src/DotNetSchema/DotNetSchema.csproj` **and** every `Version="0.1.0-alpha.4"` snippet: root `README.md`, `src/DotNetSchema/README.md`, the comment in `samples/DotNetSchema.Sample.Models/DotNetSchema.Sample.Models.csproj`; and the version in `.workflow/SPEC.md` › "What it is" |
| changes packaging, the package contents, the release process or the publish target                                                        | `PUBLISH.md` (checklists, content list, smoke test, steps)                                                                                                                                                                                                                                          |
| introduces, renames or retires a concept or term                                                                                          | the glossary in `CONTRIBUTE.md` (proposed, or on request, as [WORKFLOW.md](./WORKFLOW.md) › "Glossary" says), and every identifier, comment and document using the term                                                                                                                             |
| changes the branching model or contribution workflow (only on request, as [WORKFLOW.md](./WORKFLOW.md) › "Precedence and conflicts" says) | `WORKFLOW.custom.md`, and `CONTRIBUTE.md`                                                                                                                                                                                                                                                           |
| finds a new limit of what the package can do, or a new pitfall                                                                            | `.workflow/SPEC.md` › "Limits and non-goals" / "Pitfalls" (and the user-facing README if consumers hit it)                                                                                                                                                                                          |
| establishes a new convention, or hits a new gotcha                                                                                        | [Code style](#code-style) / [Gotchas and known issues](#gotchas-and-known-issues)                                                                                                                                                                                                                   |
| resolves one of the known issues listed below                                                                                             | remove it from [Gotchas and known issues](#gotchas-and-known-issues)                                                                                                                                                                                                                                |

Any C# sample added to a README should compile against the current API — check it in a scratch project in the session scratch directory (or a throwaway test) before committing.

## Project

DotNetSchema — a NuGet package that exports `[DotNetSchema]`-marked .NET records and classes to JSON Schema (Draft 2020-12) on every build of a host project that opts in. What it is, its version, its target framework and its parts: `.workflow/SPEC.md` › "What it is".

| Project                                                                   | Role                                                                                                                                            | Detailed docs                                  |
| ------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------- |
| `src/DotNetSchema`                                                        | The package: the attribute, `buildTransitive/` MSBuild integration, packaging of the pre-built tool. The only assembly a consumer references.   | `src/DotNetSchema/README.md` (user-facing)     |
| `src/DotNetSchema.Generator`                                              | The engine: assemblies in, `SchemaDocument`s + `SchemaDiagnostic`s out. Pure.                                                                   | `src/DotNetSchema.Generator/README.md`         |
| `src/DotNetSchema.Tool`                                                   | The console shell MSBuild runs (`dotnet exec`): metadata-only load, idempotent writes, manifest, diagnostics in MSBuild format.                 | `src/DotNetSchema.Tool/README.md`              |
| `tests/DotNetSchema.Fixtures`                                             | The "school" model graph every test runs over, a deterministic instance generator, and a live in-repo consumer of the build integration.        | `tests/DotNetSchema.Fixtures/README.md`        |
| `tests/DotNetSchema.Generator.Tests`                                      | xUnit v3 tests for the generator.                                                                                                               | `tests/DotNetSchema.Generator.Tests/README.md` |
| `samples/DotNetSchema.Sample.Models` + `samples/DotNetSchema.Sample.Host` | The worked example of the model-library / host split, with a custom `DotNetSchemaOutputPath`, `DotNetSchemaDecorator` rules and a type mapping. | comments in their csproj files                 |

Read the relevant README before changing a project — they record _why_ things are the way they are, and most non-obvious choices are explained there or in comments in the MSBuild files.

Toolchain last verified against: .NET SDK 10.0.114, xunit.v3 3.2.2, Microsoft.NET.Test.Sdk 18.9.0, System.Reflection.MetadataLoadContext 10.0.1.

## Branching

The branching model is a workflow rule: see [WORKFLOW.custom.md](./WORKFLOW.custom.md) › "Branching".

## Layout

```
DotNetSchema.slnx          Solution (slnx format): /src/, /tests/, /samples/ folders
src/
  DotNetSchema/            Attribute + buildTransitive/DotNetSchema.{props,targets} + packing of the tool under tools/net10.0/any/
  DotNetSchema.Generator/  Reflection/ (metadata-only layer), Model/ (C# types to schema shapes), Emit/ (writer), Diagnostics/
  DotNetSchema.Tool/       Program, ToolArguments, MetadataLoadContextFactory, SchemaFileWriter
tests/
  DotNetSchema.Fixtures/   Schema/ (records, Enums/, Decorators/, Invalid/), Generation/ (FixturesService, StableRandom, ...)
  DotNetSchema.Generator.Tests/  JsonSchemaGeneratorTests.<Contract>.cs partials, JsonSchemaValidator, SchemaDigest
samples/                   Models library + Host app; Host writes into samples/DotNetSchema.Sample.Host/schemas/ (gitignored)
scripts/pack.sh            Packs the package into artifacts/packages
CONTRIBUTE.md              Contributor guide: reporting issues, branching, glossary and workflow
WORKFLOW.md                The agent workflow (generated by the repo skill; kept as generated)
WORKFLOW.custom.md         This project's workflow rules (branching), read after WORKFLOW.md
.workflow/                 Workflow state: specification, plan, session state; .temp/ is gitignored
PUBLISH.md                 Release checklists and the steps to pack and push a version to nuget.org
icon.png                   The package icon (packed from the repo root by src/DotNetSchema)
.gitattributes             Pins text files to LF on every platform
artifacts/                 Pack output, gitignored
.github/                   CI workflow, dependabot, pull request template
.vscode/extensions.json    Recommended VSCode extensions: C# Dev Kit, TODO Highlight
```

Each project's README has the file-level layout; keep it, not this tree, as the detailed one.

## Commands

- `dotnet build DotNetSchema.slnx` — builds everything. Building `tests/DotNetSchema.Fixtures` and `samples/DotNetSchema.Sample.Host` also runs the in-repo export integration end to end (the tool is built through a build-only `ProjectReference` the targets inject).
- `dotnet test DotNetSchema.slnx` — runs the generator tests (about a second). A single project: `dotnet test tests/DotNetSchema.Generator.Tests`.
- `./scripts/pack.sh [extra dotnet pack args, e.g. -p:Version=0.2.0]` — packs `src/DotNetSchema` in Release into `artifacts/packages`.
- Regenerate golden digests (only after an _intended_ output change, see SPEC HARD RULE 1): `dotnet build tests/DotNetSchema.Fixtures` then `sha256sum tests/DotNetSchema.Fixtures/bin/Debug/net10.0/{assessment,schema,school}.json`, and paste into `JsonSchemaGeneratorTests.Goldens.cs`.
- Debug the tool by hand: `dotnet exec DotNetSchema.Tool.dll @obj/Debug/net10.0/DotNetSchema/export.rsp` from a consumer project directory.
- **Consumer smoke test**: before a release, or after touching packaging or `buildTransitive/`, run the one in `PUBLISH.md` (scratch library + host outside the repo, in the session scratch directory, `nuget.config` pointing at `artifacts/packages`, private `NUGET_PACKAGES`). The ordinary build never exercises the packaged path (SPEC › "Pitfalls").
- **Releasing:** follow `PUBLISH.md` — checklists before the first and every publish, then version bump, tag, fast-forward `master`, pack and `dotnet nuget push`.

## Architecture rules

The rules that must not be broken are the specification's HARD RULES and Must-haves (`.workflow/SPEC.md`); this file doesn't restate them. The reasoning behind them lives in:

- `src/DotNetSchema.Generator/README.md` — "The one rule" (no runtime-type comparisons; ask through `Reflection/MetadataFacts.cs`), "Determinism", "Decorators", "Type mappings", "Property formats";
- `src/DotNetSchema/README.md` — the user-facing side of the same contracts ("Custom type mappings", "Property formats", "Diagnostics"), including the unchecked-assertion caveats the docs must keep stating;
- the comments in `src/DotNetSchema/buildTransitive/DotNetSchema.props` / `.targets` — why each MSBuild integration rule is the way it is;
- `src/DotNetSchema.Tool/README.md` — the tool's public types, idempotent writes and pruning.

## Testing

xUnit v3 (`using Xunit` is a global `<Using>` in the test csproj). Conventions, from `tests/DotNetSchema.Generator.Tests/README.md`:

- **`JsonSchemaGeneratorTests` is one `sealed partial class`, one file per contract** (the list is in that README › "Structure"). A new test goes in the file whose contract it tests; shared helpers live in `JsonSchemaGeneratorTests.cs`.
- Test names are `[Fact]` methods named as a sentence split by underscores: `Subject_Behaviour` / `Subject_Behaviour_AndMore`, e.g. `AnUnmarkedAssembly_ProducesNothingRatherThanAnEmptyDocument`.
- **Never declare model types inside a test.** Every shape tested lives in `DotNetSchema.Fixtures` — valid ones in the school graph (or, for decorator and `[DotNetSchemaFormat]` cases the graph cannot carry, `Schema/Decorators/Bookmark`), invalid ones in `Schema/Invalid/` marked `[ExportForTesting]` (not `[DotNetSchema]`, which would export them and break the Fixtures build). Tests reach them by swapping `SchemaGenerationOptions.MarkerAttributeFullName`.
- **One test per `DNS` code** in `.Diagnostics.cs`. `DNS0010`, `DNS0011` and `DNS0014` are knowingly untested (see that README).
- **Golden digests**: SPEC HARD RULE 1. If the change was meant to move the output, regenerate (see [Commands](#commands)) and say why in the commit message. Any change to the fixtures schema moves the digests too.
- `JsonSchemaValidator` is hand-rolled and covers exactly the keywords the generator emits; `TheValidator_ImplementsEveryKeywordTheGeneratorEmits` fails when the emitter grows a keyword — extend the validator rather than reaching for a third-party one.
- Fixtures and tests must stay deterministic across machines and runtimes: draw from `StableRandom` (SplitMix64), never `System.Random`; use `CultureInfo.InvariantCulture`; derive seeds by path (`FixtureSeed`); never depend on the current time, the time zone or `Environment.NewLine`. Adding marking or decorator attributes to fixtures must not change any generated value — a constraint attribute must already hold for what `FixturesService` generates, or `ARealFixtureGraph_ValidatesAgainstTheSchemaGeneratedForIt` fails.
- Tests carry comments explaining _why_ they exist when the name does not: a regression, an edge case, or a test guarding a gotcha (link it: `See AGENTS.md › <gotcha title>`).
- `DotNetSchema.Tool` has no test project, deliberately — it is covered by the Fixtures / samples builds and the manual package check.

## Code style

There is no `.editorconfig` in this repo; follow the surrounding code.

- 2-space indentation in C# and MSBuild/XML. File-scoped namespaces matching the folder (`DotNetSchema.Generator.Model`). `ImplicitUsings` and `Nullable` enabled in every project.
- Classes `sealed` unless designed for inheritance; `record`s for data; `static` for stateless helpers. Collection expressions (`[]`, `[a, b]`), `with` expressions, expression-bodied members where short.
- Always pass `StringComparer.Ordinal` / `StringComparison.Ordinal` explicitly.
- XML doc comments (`<summary>`, `<remarks>`, `<see cref>`, `<inheritdoc />`) on public members and on non-obvious private ones. Comments are full sentences explaining _why_, not _what_; tests carry such comments too.
- MSBuild files: every non-obvious choice gets an explanatory `<!-- -->` comment in the same voice as the existing ones (what it does, why the obvious alternative is wrong).
- Encoding: `.cs` / `.csproj` files in `DotNetSchema.Generator`, `DotNetSchema.Tool` and both `tests/` projects start with a UTF-8 BOM; `src/DotNetSchema`, the samples, Markdown and scripts do not. Match the file you edit, and keep the BOM on new files in the BOM-carrying projects. (Generated schema documents never carry a BOM.)
- Prose in READMEs and comments uses British spelling (serialise, initialiser, behaviour).
- Commit messages: imperative summary line; explain _why_ when a golden digest moves (SPEC HARD RULE 1). No AI attribution (see [WORKFLOW.md](./WORKFLOW.md) › "No AI attribution").

## Gotchas and known issues

The project-level pitfalls (packaged path untested by the build, the global NuGet cache, the `MetadataLoadContext` major version, line endings, no branch protection) and limits (models delivered as packages, paths containing `;`) are in `.workflow/SPEC.md` › "Pitfalls" / "Limits and non-goals". The implementation gotchas:

- **Multi-line raw-string goldens call `.ReplaceLineEndings("\n")`**, on top of the `.gitattributes` LF pin (SPEC › "Pitfalls"): the golden literals take their line endings from the checkout, and the generator always writes `\n`.
- **MSBuild only auto-imports a package's targets for a `PackageReference`, never a `ProjectReference`.** Every in-repo consumer (`tests/DotNetSchema.Fixtures`, both samples) must `<Import>` `src/DotNetSchema/buildTransitive/DotNetSchema.targets` by hand.
- **`tests/DotNetSchema.Fixtures` pins `DotNetSchemaDefaultFileName` to `schema.json`** so its `bin/` output equals `SchemaGenerationOptions.Default` and the golden digests. Don't change one without the other — nor add `DotNetSchemaDecorator` items there; the sample host is where the MSBuild decorator plumbing is exercised.
- **Lists are items, and a pattern never goes in an `Include`** — an `Include` containing `*` is a file glob and, matching nothing, silently drops the item; `;` splits it. Patterns live in metadata (`DotNetSchemaDecorator`'s `Attribute`, `DotNetSchemaType`'s `Pattern`) and reach the tool through an item transform, which neither splits nor globs. Order-sensitive rules must be one item type: MSBuild keeps order only within an item type.
- **On a positional record, an attribute without a `property:` target lands on the constructor parameter.** `DecoratorReader` also reads the primary-constructor parameter (recognised through the compiler-generated `Deconstruct`); anything else reading property attributes must do the same.
- **A `Type` attribute argument must be rendered with `MetadataFacts.TypeName`, never `FullName`** — a constructed generic's `FullName` embeds assembly-qualified arguments that differ between the runtime and metadata-only reads.
- **Packing ships the tool's _publish_ output**, not its build output — `tools/` is not a restore target, so `System.Reflection.MetadataLoadContext.dll` must be physically present. `PackagePath`s for `buildTransitive/` files are full file paths, not directories (a trailing separator nests them as `buildTransitive/buildTransitive/`). Use `None Update`, not `Include`, for files the SDK glob already matches (NETSDK1022).
- **Never scan via `@(ReferencePathWithRefAssemblies)`** — it offers a ref assembly with the same identity as the implementation assembly and the resolver refuses it. The tool also deduplicates references by assembly name (highest version wins).
- **`DotNetSchemaType` metadata is MSBuild-unescaped**: a `%XX` in a pattern decodes, so a literal `%` is `%25`. `;`, `*` and `|` survive, because the targets build each `--type-mapping` line with an item transform (not re-split, not globbed) and the tool splits it into at most four parts, pattern last. Response-file lines are trimmed, so a pattern cannot end in whitespace.
- **A Web SDK host with an in-tree `DotNetSchemaOutputPath` must exclude that folder from `Content`**, or the documents are published twice.
- **Bump `System.Reflection.MetadataLoadContext` in both `DotNetSchema.Tool.csproj` and `DotNetSchema.Generator.Tests.csproj`**, by hand, together with the TFM; Dependabot ignores its semver-major updates (SPEC › "Pitfalls").
- **A project not listed in `DotNetSchema.slnx` is invisible to CI.** `dotnet build` / `test` run against the solution, so add every new project to it (`dotnet sln DotNetSchema.slnx add <path> --solution-folder <src|tests|samples>`).
- `.gitignore` ignores `.vscode/` but re-includes `settings.json`, `tasks.json`, `launch.json` and `extensions.json`. Any other `.vscode/` file must be re-included there before it can be committed.
- **`*.gitignore.*` files and `*.gitignore/` directories are local-only** (e.g. `TODO.gitignore.md`) and ignored by `.gitignore` — never commit them. They are for your own local notes: an agent's working files still go under `./.workflow/.temp/<topic>/` (see [WORKFLOW.md](./WORKFLOW.md) › "Working files").

## Continuous integration

`.github/workflows/ci.yml` runs on every pull request targeting `master` or `develop` and on every push to either, on an `[ubuntu-latest, windows-latest]` matrix with `fail-fast: false` (both path flavours, because the integration resolves paths and starts the tool from MSBuild). Steps: `dotnet restore DotNetSchema.slnx` → `dotnet build -c Release --no-restore` → `dotnet test -c Release --no-build` → `./scripts/pack.sh`; the `.nupkg` and `.snupkg` are uploaded as the `packages` artifact from the ubuntu leg. In-progress runs are cancelled only for pull requests.

`.github/dependabot.yml`: weekly NuGet updates grouped into one pull request (MetadataLoadContext majors ignored), weekly github-actions updates. It targets `develop` explicitly (`target-branch`), so it doesn't depend on which branch is the repository's default.

CI does not block merges by itself; that needs branch protection with the `CI / ubuntu-latest` and `CI / windows-latest` checks required (`.workflow/PLAN.md` › "Known open issues").

---

## Workflow

Follow the workflow defined in [WORKFLOW.md](./WORKFLOW.md).

@WORKFLOW.md
