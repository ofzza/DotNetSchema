using System.Reflection;
using DotNetSchema.Generator;
using DotNetSchema.Tool;

// Entry point for the build integration. Everything interesting happens in the generator; this is the
// shell that turns MSBuild's arguments into a metadata-only load, and diagnostics back into build output.
const string Origin = "dotnet-schema";

try
{
  var arguments = ToolArguments.Parse(args);

  using var context = MetadataLoadContextFactory.Create(arguments.References.Concat(arguments.Scan));

  var assemblies = new List<Assembly>();
  foreach (var path in arguments.Scan.Where(File.Exists))
  {
    assemblies.Add(context.LoadFromAssemblyPath(path));
  }

  var options = SchemaGenerationOptions.Default with { DefaultFileName = arguments.DefaultFileName };
  var result = new JsonSchemaGenerator().Generate(assemblies, options);

  foreach (var diagnostic in result.Diagnostics)
  {
    Console.WriteLine(diagnostic.ToCanonicalString(Origin));
  }

  if (result.HasErrors)
  {
    return 1;
  }

  if (result.Documents.Count == 0 && arguments.FailOnEmpty)
  {
    Console.WriteLine(
      $"{Origin} : error DNS0013: No types are marked with [DotNetSchema], and " +
      "DotNetSchemaFailOnEmpty is set.");
    return 1;
  }

  var written = SchemaFileWriter.Write(result.Documents, arguments.OutputDirectory, arguments.ManifestPath);
  foreach (var path in written)
  {
    Console.WriteLine($"DotNetSchema -> {path}");
  }

  return 0;
}
catch (Exception exception)
{
  // Canonical form, so MSBuild renders this as a build error rather than as stray tool output.
  Console.WriteLine($"{Origin} : error DNS0000: {exception.GetType().Name}: {exception.Message}");
  return 1;
}
