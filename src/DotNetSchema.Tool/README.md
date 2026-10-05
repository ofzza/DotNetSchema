# DotNetSchema.Tool

The command-line shell the build runs. It turns MSBuild's arguments into a metadata-only load, hands the
assemblies to [`DotNetSchema.Generator`](../DotNetSchema.Generator/README.md),
and turns the result back into files and build diagnostics.

Nobody invokes this by hand except when debugging; the build integration in
[`DotNetSchema`](../DotNetSchema/README.md) does it.

```bash
dotnet exec DotNetSchema.Tool.dll @obj/Debug/net10.0/DotNetSchema/export.rsp
```

## Arguments

Always passed in a response file. A full framework closure runs to well over a hundred paths, which
exceeds the command-line length limit on Windows; one `--key=value` per line needs no quoting and never
reaches a shell.

| Argument | Repeatable | Meaning |
| --- | --- | --- |
| `--scan=<path>` | yes | An assembly to search for marked types. |
| `--reference=<path>` | yes | An assembly needed to resolve what the scanned ones mention. |
| `--output-directory=<path>` | no | Where documents are written. |
| `--default-file-name=<name>` | no | Document a bare `[DotNetSchema]` targets. |
| `--manifest=<path>` | no | File recording what was produced. |
| `--fail-on-empty=true\|false` | no | Whether producing nothing is an error. |

A path containing `;` is not supported: MSBuild splits item lists on it long before the tool sees it.

## What it does that is not obvious

**Nothing is executed.** Assemblies are opened through a `MetadataLoadContext`, so no module initialiser,
static constructor or line of the project being built ever runs during a build that exports.

**The reference list is deduplicated by assembly name**, keeping the highest version. A
`PathAssemblyResolver` refuses a second file carrying an identity it already holds, and MSBuild can
legitimately hand over two paths for one assembly.

**Writes are idempotent.** A document whose bytes have not changed is not rewritten, so a no-op build does
not move timestamps and make everything downstream look out of date.

**Stale documents are pruned.** The manifest from the previous run is read first, and anything it lists
that this run did not produce is deleted — which is what happens when the file name in an attribute
changes. Deletion is confined to the output directory, so a tampered manifest cannot be turned into a way
of removing something else.

**Diagnostics are printed in MSBuild's canonical form**, so `Exec` promotes them to real build errors and
warnings with no logger involved. Two codes originate here rather than in the generator: `DNS0000` for an
unhandled exception, and `DNS0013` for an empty run under `--fail-on-empty=true`.

Exit code is `0`, or `1` if any error diagnostic was raised.

## Layout

```
DotNetSchema.Tool/
  Program.cs                        argument handling to exit code
  ToolArguments.cs                  parsing, including @response-file expansion
  MetadataLoadContextFactory.cs     the metadata-only load
  SchemaFileWriter.cs               idempotent writes, pruning, the manifest
  README.md
```

## Public surface

`MetadataLoadContextFactory`, `SchemaFileWriter` and `ToolArguments` are public so another front end
(a CLI, a test harness) can drive the same metadata-only load and the same idempotent writes. Treat their
signatures as API.

## Tests

No `.Tests` sibling, which departs from the repository's convention deliberately. Everything here is
plumbing around the generator, and its real contract is the behaviour of a consumer's build — incremental
skips, hand-deleted documents reappearing, stale documents pruned, publish output — which a unit test
around the argument parser would not touch.

That behaviour is covered instead by `DotNetSchema.Fixtures` and the samples, which export documents on
every build, and by the package check described in the [package README](../DotNetSchema/README.md).

## License

MIT — see [LICENSE.md](../../LICENSE.md).
