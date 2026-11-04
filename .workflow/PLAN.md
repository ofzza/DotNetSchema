# Plan

## Goals

Implement and maintain the specification (`./.workflow/SPEC.md`). The project is already implemented and released up to `0.1.0-alpha.4`; this plan records that baseline and the issues known about it. New segments are added when the next piece of work is named.

## Segments

### Segment 1: Baseline (implemented, `0.1.0-alpha.4`)

- **Status:** done. Recorded on 2026-11-04 from the existing code and documentation; it was not planned under this workflow.
- **Covers:** every section of the specification's "Must-haves": marking and discovery, document shape, the System.Text.Json mapping table, enums and enum styles, decorators, type mappings, property formats, determinism, diagnostics, MSBuild integration, the tool, tests, CI and the release process.
- **History** (`git log`):
  - `0.1.0-alpha.1` (2026-10-06): initial implementation, agent guidance, GitHub configuration, release guide.
  - `0.1.0-alpha.2` (2026-10-13): fixtures cover the required × nullable combinations, nullable refs, arrays, maps, enums.
  - `0.1.0-alpha.3` (2026-10-14): `[JsonRequired]`, `DotNetSchemaEnumStyle`, `x-decorators`.
  - `0.1.0-alpha.4` (2026-10-14): custom type mappings (`DotNetSchemaType`), ordered `DotNetSchemaDecorator` rules, `[DotNetSchemaFormat]`.
  - After it: alignment with the dotnet template and adoption of the repo skill workflow.
- **Open points:** _none._

## Known open issues

- [ ] **`DNS0010`, `DNS0011` and `DNS0014` have no tests.** Each needs an input the fixtures can't express (a partially loadable assembly, an assembly without nullable reference types, an attribute argument naming an unresolvable type). Deferred as knowingly untested (`tests/DotNetSchema.Generator.Tests/README.md` › "Where coverage stops").
- [ ] **No branch protection on `master` / `develop`.** CI runs but doesn't block merges. It's a GitHub setting, outside the repository (`AGENTS.md` › "Continuous integration").
- [ ] **Models delivered as packages aren't scanned**, only the host's project-reference closure. Documented as a limit (SPEC › "Limits and non-goals").
- [ ] **Paths containing `;` are unsupported** (MSBuild item-list splitting). Documented as a limit.
- [ ] **Open dependabot branches on `origin`**: three `dependabot/nuget/src/DotNetSchema.Tool/dependencies-*` branches waiting for review.
- [ ] **The local `TODO.gitignore.md` note on non-enum `[JsonConverter]` types looks resolved** by `DNS0019`, type mappings and `[DotNetSchemaFormat]` (`0.1.0-alpha.4`). It needs your confirmation; the file is local-only and yours to delete.
