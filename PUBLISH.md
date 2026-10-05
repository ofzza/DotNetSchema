# Publishing

How to release a new version of the `DotNetSchema` package to NuGet. Releases are cut from `develop` and published from `master`, following the branching model in [AGENTS.md](./AGENTS.md#branching).

A version published to nuget.org can never be overwritten or deleted — only unlisted, and an unlisted version can still be restored by anyone who names it. Treat every push as permanent.

## Before the first publish

One-time work, found by packing `0.1.0` and installing it into a scratch consumer. Tick each item off (or decide deliberately not to) before the first release, then delete this section.

- [ ] **Decide which frameworks the attribute supports.** `lib/` currently contains only `net10.0`, so a model library targeting `net8.0` (the current LTS) or `netstandard2.0` cannot reference the package — it restores, then fails with `CS0246` on `using DotNetSchema;`. The attribute assembly is tiny and the generator matches it by full name, so multi-targeting `src/DotNetSchema` (e.g. `<TargetFrameworks>netstandard2.0;net10.0</TargetFrameworks>`) should be enough; re-run the [consumer smoke test](#consumer-smoke-test) with a lower-framework library to confirm. The export tool can stay `net10.0`.
- [ ] **Document the SDK requirement.** Whatever the attribute targets, the export tool under `tools/net10.0/any/` runs on the .NET 10 runtime, so the host build needs the .NET 10 SDK. Neither README says so yet.
- [ ] **Complete the package metadata** in `src/DotNetSchema/DotNetSchema.csproj`. The nuspec currently carries a repository commit but no URL. Add `RepositoryUrl` (`https://github.com/ofzza/DotNetSchema`), `RepositoryType` (`git`), `PackageProjectUrl`, `PackageTags` (e.g. `json-schema;msbuild;codegen;system-text-json`) and, optionally, `PackageIcon`.
- [ ] **Make the packaged README's links absolute.** `src/DotNetSchema/README.md` is what nuget.org renders, and its relative links (`../DotNetSchema.Generator/README.md`, `../DotNetSchema.Tool/README.md`, `../../tests/DotNetSchema.Generator.Tests/README.md`, `../../LICENSE.md`) are dead there. Point them at `https://github.com/ofzza/DotNetSchema/blob/master/...`.
- [ ] **Decide whether the first release is a preview.** `0.1.0-preview.1` (or `-alpha.1`) leaves `0.1.0` free for when the package has met real consumers.
- [ ] **Decide how packages reach nuget.org.** Either push by hand with an API key (the steps below assume this), or add a workflow that packs and pushes on a `v*` tag on `master`, using a `NUGET_API_KEY` secret or nuget.org trusted publishing. If you add one, update this file and [Continuous integration](./AGENTS.md#continuous-integration) in `AGENTS.md`.
- [ ] **Optionally mark release builds as CI builds and ship symbols**: `ContinuousIntegrationBuild=true` (normalises source paths in the PDBs — conditioned on `GITHUB_ACTIONS`, or passed to `pack.sh` as below), and `IncludeSymbols` + `SymbolPackageFormat=snupkg` for a symbol package. Today the only PDBs are the tool's, inside `tools/`.
- [ ] **Fix the stale `src/dotnet/...` paths** in `tests/DotNetSchema.Generator.Tests/README.md` and `src/DotNetSchema.Generator/README.md` (see [Gotchas and known issues](./AGENTS.md#gotchas-and-known-issues)).
- [ ] **Optionally protect `master`** in the GitHub repo settings, requiring the `CI / ubuntu-latest` and `CI / windows-latest` checks.

## Before each publish

- [ ] **`develop` is clean and in sync with `origin`** — `git status` shows nothing to commit, and `git pull` brings in nothing new.
- [ ] **CI is green** for the head of `develop` on GitHub, on both `CI / ubuntu-latest` and `CI / windows-latest`.
- [ ] **The Release build and tests pass locally**, exactly as CI runs them:

  ```sh
  $ dotnet restore DotNetSchema.slnx
  $ dotnet build DotNetSchema.slnx -c Release --no-restore
  $ dotnet test DotNetSchema.slnx -c Release --no-build
  ```

- [ ] **Every golden digest change since the last release was intended**, and its commit message says why — `git log v<previous>..develop -- tests/DotNetSchema.Generator.Tests/JsonSchemaGeneratorTests.Goldens.cs`.
- [ ] **The documentation matches the code** — per [Keeping this file and the READMEs current](./AGENTS.md#keeping-this-file-and-the-readmes-current): the property table ("Configuring it") and the Diagnostics table in `src/DotNetSchema/README.md`, the mapping table in the Generator README, and the Arguments table in the Tool README.
- [ ] **The new version has been chosen** following semver, and is not in the list nuget.org already has — including unlisted versions:

  ```sh
  $ curl -s https://api.nuget.org/v3-flatcontainer/dotnetschema/index.json
  ```

  (A `404` means nothing has been published yet.) A change to the schema the package produces for unchanged models is a breaking change for anyone generating code from it.
- [ ] **The package contents are right** — after packing (step 2 below), `unzip -l artifacts/packages/DotNetSchema.<version>.nupkg` lists `lib/<tfm>/DotNetSchema.dll`, `buildTransitive/DotNetSchema.{props,targets}`, `tools/net10.0/any/` with `DotNetSchema.Tool.dll`, `DotNetSchema.Generator.dll`, `System.Reflection.MetadataLoadContext.dll`, `DotNetSchema.Tool.runtimeconfig.json` and `.deps.json`, plus `README.md` and `LICENSE.md` — and nothing from `tests/` or `samples/`. `unzip -p … DotNetSchema.nuspec` shows the right version, license, repository URL and readme.
- [ ] **The packed package works for a consumer** — see [Consumer smoke test](#consumer-smoke-test) below. The ordinary build only ever exercises the in-repository path, so this is the only check of the packaged one.
- [ ] **You have a valid nuget.org API key** with push rights to `DotNetSchema` (nuget.org → API Keys; scope it to the `DotNetSchema` glob). Keys expire — check the date.
- [ ] **`master` can be fast-forwarded to `develop`** — `git log develop..master` prints nothing. If it prints commits (a hotfix, for example), merge `master` into `develop` first and start this checklist again.

### Consumer smoke test

Restore the packed package into throwaway projects outside the repository: a model library that references the package, and a host that references the library and opts in. Use a private `NUGET_PACKAGES` folder — the global package cache keeps the first copy of a version it sees, so re-packing the same version would otherwise test a stale package.

```sh
$ ./scripts/pack.sh -p:Version=<version>
$ mkdir -p /tmp/dotnetschema-smoke/{Lib,Host} && cd /tmp/dotnetschema-smoke
$ export NUGET_PACKAGES=$PWD/.nuget
$ cat > nuget.config <<EOF
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="<repo>/artifacts/packages" />
    <add key="nuget" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
EOF
$ cat > Lib/Lib.csproj <<'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="DotNetSchema" Version="<version>" />
  </ItemGroup>
</Project>
EOF
$ cat > Lib/Customer.cs <<'EOF'
using DotNetSchema;
namespace Lib;
[DotNetSchema] public sealed record Customer(Guid Id, string Name, Customer? Referrer);
EOF
$ cat > Host/Host.csproj <<'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <DotNetSchemaGenerate>true</DotNetSchemaGenerate>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../Lib/Lib.csproj" />
  </ItemGroup>
</Project>
EOF
$ echo 'System.Console.WriteLine();' > Host/Program.cs
$ dotnet build Host
$ dotnet build Host
$ dotnet publish Host -o pub
```

It passes when:

- both builds succeed with no `DNS` warnings or errors;
- `Host/bin/Debug/net10.0/Host.schema.json` exists, defines `Customer` in `$defs`, and is **not** rewritten by the second build (its timestamp does not move);
- nothing `*.schema.json` appears under `Lib/bin/`;
- `pub/Host.schema.json` exists;
- neither csproj contains an `<Import>`.

Repeat with `Lib` targeting every other framework the attribute claims to support (e.g. `net8.0`), changing only its `TargetFramework`.

## Packaging and publishing

1. **Bump the version on `develop`.** Set `<Version>` in `src/DotNetSchema/DotNetSchema.csproj`, and every `Version="<old>"` snippet with it — `grep -rn 'Version="<old>"' --include=*.md --include=*.csproj .` finds them (root `README.md`, `src/DotNetSchema/README.md`, the comment in `samples/DotNetSchema.Sample.Models/DotNetSchema.Sample.Models.csproj`). Then commit and tag:

   ```sh
   $ git switch develop
   $ git commit -am "Release v<version>"
   $ git tag v<version>
   ```

2. **Pack it from a clean output folder**, so that a stale `.nupkg` can never be the one pushed, and run the content checks and the [consumer smoke test](#consumer-smoke-test) against it:

   ```sh
   $ rm -rf artifacts/packages
   $ ./scripts/pack.sh -p:ContinuousIntegrationBuild=true
   ```

3. **Push `develop` with the tag**, and wait for CI to pass on the version commit:

   ```sh
   $ git push --follow-tags origin develop
   ```

4. **Fast-forward `master` to `develop`** and push it, so that `master` points at exactly the tagged commit:

   ```sh
   $ git switch master
   $ git pull --ff-only
   $ git merge --ff-only develop
   $ git push origin master
   ```

5. **Publish from `master`.** Re-pack on `master` (it is the same commit, but this guarantees the package's embedded commit is the tagged one), then push. Never paste the key into a committed file or a shell history you keep:

   ```sh
   $ rm -rf artifacts/packages
   $ ./scripts/pack.sh -p:ContinuousIntegrationBuild=true
   $ dotnet nuget push artifacts/packages/DotNetSchema.<version>.nupkg \
       --api-key "$NUGET_API_KEY" \
       --source https://api.nuget.org/v3/index.json
   ```

   If symbol packages are enabled, `dotnet nuget push` picks up the `.snupkg` next to the `.nupkg` automatically.

6. **Verify the release.** nuget.org validates and indexes a new version over several minutes. Once `curl -s https://api.nuget.org/v3-flatcontainer/dotnetschema/index.json` lists it, re-run the [consumer smoke test](#consumer-smoke-test) with the `local` source removed from `nuget.config`, so the package comes from nuget.org. Check that the README renders on `https://www.nuget.org/packages/DotNetSchema/<version>` with working links.

7. **Switch back to `develop`** to continue work:

   ```sh
   $ git switch develop
   ```
