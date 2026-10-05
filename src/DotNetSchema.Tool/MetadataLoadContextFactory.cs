using System.Reflection;

namespace DotNetSchema.Tool;

/// <summary>Builds the metadata-only load context the assemblies are read through.</summary>
public static class MetadataLoadContextFactory
{
  /// <summary>
  /// Opens a context over the given assemblies. Nothing is executed: no module initialiser, no static
  /// constructor, no code from the project being built.
  /// </summary>
  /// <param name="paths">Every assembly that might need resolving, the ones to scan included.</param>
  public static MetadataLoadContext Create(IEnumerable<string> paths)
  {
    // A PathAssemblyResolver rejects a second file carrying an assembly identity it already holds, so the
    // list has to be deduplicated before it gets there. MSBuild can legitimately hand over two paths for
    // one assembly - an implementation assembly and a reference assembly, say - and the highest version
    // is the one that describes the most.
    var deduplicated = paths
      .Where(File.Exists)
      .Select(path => (Path: path, Name: Identify(path)))
      .Where(entry => entry.Name is not null)
      .GroupBy(entry => entry.Name!.Name, StringComparer.OrdinalIgnoreCase)
      .Select(group => group.OrderByDescending(entry => entry.Name!.Version).First().Path)
      .ToList();

    return new MetadataLoadContext(new PathAssemblyResolver(deduplicated), coreAssemblyName: "System.Runtime");
  }

  private static AssemblyName? Identify(string path)
  {
    try
    {
      return AssemblyName.GetAssemblyName(path);
    }
    catch (Exception exception) when (exception is BadImageFormatException or FileLoadException or IOException)
    {
      // A native library or a stray file among the reference paths is not something to fail the build over.
      return null;
    }
  }
}
