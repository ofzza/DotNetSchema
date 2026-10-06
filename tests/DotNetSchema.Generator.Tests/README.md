# DotNetSchema.Generator.Tests

Tests for [`DotNetSchema.Generator`](../../src/DotNetSchema.Generator/README.md).

```bash
dotnet test tests/DotNetSchema.Generator.Tests   # from the repository root
```

xUnit v3. Pure computation apart from one metadata-only assembly load, so the suite runs in about a
second and can be left on a watch.

Every model these tests run over comes from
[`DotNetSchema.Fixtures`](../DotNetSchema.Fixtures/README.md) — the school graph for the
ordinary cases, and `Schema/Invalid/` for the diagnostics. Nothing here declares a type to test against:
a shape worth asserting on is a shape worth adding to the fixtures, where every test can
meet it.

## Structure

`JsonSchemaGeneratorTests` is one partial class split by the contract under test. A new test goes in the
file its contract already occupies.

```
DotNetSchema.Generator.Tests/
  JsonSchemaGeneratorTests.cs               shared helpers, and the testing marker
  JsonSchemaGeneratorTests.Discovery.cs     which types are found, and which document each lands in
  JsonSchemaGeneratorTests.Mapping.cs       the scalar table and the dictionary key table
  JsonSchemaGeneratorTests.Nullability.cs   the nine nullable members, and that nothing else is
  JsonSchemaGeneratorTests.Recursion.cs     cycles, the full closure, and no bare objects
  JsonSchemaGeneratorTests.Determinism.cs   metadata-only vs runtime; newlines; no BOM
  JsonSchemaGeneratorTests.Goldens.cs       whole documents by digest, salient definitions verbatim
  JsonSchemaGeneratorTests.Diagnostics.cs   one case per DNS code
  JsonSchemaGeneratorTests.Validates.cs     real fixture graphs against the generated schema
  JsonSchemaValidator.cs                    a validator for exactly the keywords the generator emits
  SchemaDigest.cs                           SHA-256 of a document
  README.md
```

## The two that carry the weight

**`ReadingTypesForMetadataOnly_ProducesTheSameBytesAsReadingThemAtRuntime`.** The generator is reached
both ways — the build opens assemblies for metadata only, these tests use the running ones — and a type
read those two ways is not interchangeable. A single `typeof(X) == t`, `Nullable.GetUnderlyingType` or
`NullabilityInfoContext` in the generator would answer differently on the metadata path, and none of them
throw when they do. This test runs both and compares bytes, so the whole no-runtime-types rule rests on
it. Look here first if it fails.

**`ARealFixtureGraph_ValidatesAgainstTheSchemaGeneratedForIt`.** Everything else compares the schema
against what this project *believes* System.Text.Json does. This serialises an actual fixture graph and
validates it against the document, so it compares the schema against what System.Text.Json actually did.

`JsonSchemaValidator` is hand-rolled and deliberately narrow — it implements the keyword subset the
generator emits and nothing more, for the same reason `FixtureDigest` is hand-rolled in the fixtures
suite: a third-party validator would be testing itself as much as the schema.
`TheValidator_ImplementsEveryKeywordTheGeneratorEmits` fails if the emitter grows a keyword the validator
does not know, so the coverage cannot quietly fall behind. `TheValidator_ActuallyRejectsSomething` keeps
the passing assertions from being vacuous.

## Golden values

Two kinds, and they answer different questions. `EachDocument_ReproducesItsGoldenDigest` pins each whole
document by SHA-256, so drift anywhere lands somewhere. `ScalarRegister_ReproducesItsGoldenDefinition`
and `Person_ReproducesItsGoldenSalientProperties` pin definitions verbatim, so drift in the parts that
matter most says *what* moved.

The digests were produced by this generator over `DotNetSchema.Fixtures` as it stands, with
`SchemaGenerationOptions.Default`. They depend on the fixtures schema, so a change there moves them too.

To regenerate after a change you meant to make:

```bash
dotnet build tests/DotNetSchema.Fixtures
sha256sum tests/DotNetSchema.Fixtures/bin/Debug/net10.0/{assessment,schema,school}.json
```

and paste the values into `JsonSchemaGeneratorTests.Goldens.cs`. Never edit one to make a test pass: if
the change was meant to move the output, move the values and say why in the commit message; if it was
not, the change is wrong.

## Where coverage stops

Two diagnostics have no test. `DNS0010` needs an assembly that partially fails to load, which cannot be
produced without shipping a broken binary. `DNS0011` needs a model assembly compiled without nullable
reference types, and every project here enables them. Both are exercised by inspection only.

## License

MIT — see [LICENSE.md](../../LICENSE.md).
