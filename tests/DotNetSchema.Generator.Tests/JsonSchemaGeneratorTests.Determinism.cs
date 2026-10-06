using System.Reflection;
using DotNetSchema.Fixtures.Schema;

namespace DotNetSchema.Generator.Tests;

public sealed partial class JsonSchemaGeneratorTests
{
  /// <summary>
  /// The load-bearing test of the whole design.
  /// </summary>
  /// <remarks>
  /// The generator is used two ways: against the running assemblies, which is how these tests reach it,
  /// and against assemblies opened for metadata only, which is how the build does. Those two views of a
  /// type are not interchangeable — a metadata-only type is never reference-equal to a runtime one — so a
  /// single stray <c>typeof(X) == t</c>, <c>Nullable.GetUnderlyingType</c> or
  /// <c>NullabilityInfoContext</c> anywhere in the generator would quietly answer differently on the two
  /// paths. None of those throw; they just return the wrong answer.
  /// <para>
  /// So the invariant is asserted directly: the same input, read both ways, must produce the same bytes.
  /// This test is the reason <c>MetadataFacts</c> can be trusted, and it is the one to look at first if it
  /// ever starts failing.
  /// </para>
  /// </remarks>
  [Fact]
  public void ReadingTypesForMetadataOnly_ProducesTheSameBytesAsReadingThemAtRuntime()
  {
    var fromRuntime = new JsonSchemaGenerator().Generate([FixturesAssembly]);

    using var context = MetadataOnly();
    var fixtures = context.LoadFromAssemblyPath(FixturesAssembly.Location);
    var fromMetadata = new JsonSchemaGenerator().Generate([fixtures]);

    Assert.Equal(
      fromRuntime.Documents.Select(d => d.FileName),
      fromMetadata.Documents.Select(d => d.FileName));

    foreach (var (runtime, metadata) in fromRuntime.Documents.Zip(fromMetadata.Documents))
    {
      Assert.Equal(runtime.Json, metadata.Json);
    }

    Assert.Equal(
      fromRuntime.Diagnostics.Select(d => d.ToCanonicalString("test")),
      fromMetadata.Diagnostics.Select(d => d.ToCanonicalString("test")));
  }

  [Fact]
  public void GeneratingTwice_ProducesTheSameBytes()
  {
    var first = new JsonSchemaGenerator().Generate([FixturesAssembly]);
    var second = new JsonSchemaGenerator().Generate([FixturesAssembly]);

    Assert.Equal(first.Documents.Select(d => d.Json), second.Documents.Select(d => d.Json));
  }

  [Fact]
  public void EveryDocument_UsesLineFeedsAndEndsWithExactlyOneNewline()
  {
    foreach (var document in new JsonSchemaGenerator().Generate([FixturesAssembly]).Documents)
    {
      Assert.DoesNotContain('\r', document.Json);
      Assert.EndsWith("}\n", document.Json, StringComparison.Ordinal);
    }
  }

  [Fact]
  public void EveryDocument_IsWrittenWithoutAByteOrderMark()
  {
    foreach (var document in new JsonSchemaGenerator().Generate([FixturesAssembly]).Documents)
    {
      // A schema file is read by toolchains outside .NET, several of which choke on a BOM. This is a
      // deliberate departure from the C# sources in this repository, which carry one.
      Assert.Equal((byte)'{', document.ToUtf8Bytes()[0]);
    }
  }

  private static MetadataLoadContext MetadataOnly()
  {
    var runtime = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

    var paths = Directory
      .EnumerateFiles(AppContext.BaseDirectory, "*.dll")
      .Concat(Directory.EnumerateFiles(runtime, "*.dll"))
      .GroupBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
      .Select(group => group.First())
      .ToList();

    return new MetadataLoadContext(new PathAssemblyResolver(paths), coreAssemblyName: "System.Runtime");
  }
}
