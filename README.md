# DotNetSchema

A NuGet package that exports `[DotNetSchema]`-marked .NET records and classes to JSON Schema
(Draft 2020-12) on every build of your host project.

```csharp
[DotNetSchema]                  // -> <AssemblyName>.schema.json
public sealed record Customer(Guid Id, string Name);

[DotNetSchema("orders.json")]   // -> orders.json
public sealed record Order(Guid Id, IReadOnlyList<OrderLine> Lines);
```

```xml
<!-- wherever models live -->
<PackageReference Include="DotNetSchema" Version="0.1.0-alpha.1" />

<!-- in the host project that should produce the documents -->
<DotNetSchemaGenerate>true</DotNetSchemaGenerate>
<DotNetSchemaOutputPath>schemas</DotNetSchemaOutputPath>   <!-- optional; defaults to $(OutDir) -->
```

Requires the .NET 10 SDK for the host build, and `net10.0` or later in every project that references the
package. Published on NuGet as [`DotNetSchema`](https://www.nuget.org/packages/DotNetSchema).

Full documentation: [src/DotNetSchema/README.md](src/DotNetSchema/README.md).

| Path | |
| --- | --- |
| `src/DotNetSchema` | The package: attribute, `buildTransitive/` MSBuild integration, packaging |
| `src/DotNetSchema.Generator` | The schema engine |
| `src/DotNetSchema.Tool` | The command-line shell the build runs |
| `tests/` | Generator tests and the fixture models they run over |
| `samples/` | A model library + host pair showing the opt-in and a custom output path |
| `scripts/pack.sh` | Packs into `artifacts/packages` |
| `PUBLISH.md` | Release checklists and the steps to publish to nuget.org |
| `icon.png` | The package icon |

```bash
dotnet build DotNetSchema.slnx
dotnet test DotNetSchema.slnx
./scripts/pack.sh
```

MIT — see [LICENSE.md](LICENSE.md).
