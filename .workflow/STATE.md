# Session state

## Next session: start here

- **Stage:** planning. The specification and the plan's Baseline segment are approved.
- **Working on:** nothing; the next piece of work hasn't been named yet.
- **Next step:** name the next work (e.g. one of the plan's known open issues, or a new feature through the specification) and plan it as a new segment.

## Open questions

_None._

## Waiting on me

- Confirm the pre-Prettier copies in `./.workflow/.temp/showroom-format/` can be deleted.

- Confirm the `TODO.gitignore.md` note is resolved (`PLAN.md` › Known open issues).
- Confirm the repo-skill reports in `./.workflow/.temp/` are applied, so they can be deleted.

## Running

_Nothing._

## History

- 2026-11-04: `SPEC.md`, `PLAN.md` and `STATE.md` drafted from the existing documentation; budget "small, maintenance"; HARD RULES 1–2 added at your request (golden digests, permanent `DNS` codes).
- 2026-11-04: specification approved ("Approve").
- 2026-11-04: plan Segment 1 (Baseline) approved ("Approve").
- 2026-11-04: Glossary terms "Property format" and "Enum style" added to `CONTRIBUTE.md`.
- 2026-11-04: documentation clean-up (plan approved in plan mode): `AGENTS.md` points into `SPEC.md` instead of restating it; stale facts fixed in `AGENTS.md`; duplicates trimmed from `README.md`, `CONTRIBUTE.md` and `src/DotNetSchema/README.md`. A stop (Prettier on `AGENTS.md` without a pre-format copy) was reported and cleared ("go on").
- 2026-11-16: showroom updated to `@ofzza/showroom` 0.1.0-alpha.2 (`tsshowcase.json` → `showroom.json`, `dotnet` stack adapter), documented in `README.md`, `PUBLISH.md`, `AGENTS.md` and `SPEC.md`, rebuilt into the gitignored `showroom/`, and published to GitHub Pages by the new `showroom.yml` workflow on every push to `master`; `ci.yml` runs `showroom check` on ubuntu. Checkpoint: "Approve and commit".
- 2026-11-16: release `0.1.0-alpha.5` cut on `master` (your choice, instead of `develop`; the only packaged change since `0.1.0-alpha.4` is the trimmed package README); pre-publish checks and consumer smoke test passed.
- 2026-11-16: fast path: each `README.md` links to its own page in the showroom (the root one to the showroom), as `../tsschema` does; Prettier also aligned the tables of three READMEs it hadn't formatted before.
