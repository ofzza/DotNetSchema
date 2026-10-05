using DotNetSchema.Generator;

namespace DotNetSchema.Tool;

/// <summary>Puts the produced documents on disk, and takes away the ones that are no longer produced.</summary>
public static class SchemaFileWriter
{
  /// <summary>
  /// Writes each document, removes anything the previous run left behind that this one did not produce,
  /// and records what now exists.
  /// </summary>
  /// <param name="documents">The documents to write.</param>
  /// <param name="outputDirectory">Where they go.</param>
  /// <param name="manifestPath">Where the record of them goes.</param>
  /// <returns>The absolute paths written.</returns>
  public static IReadOnlyList<string> Write(
    IReadOnlyList<SchemaDocument> documents,
    string outputDirectory,
    string manifestPath)
  {
    Directory.CreateDirectory(outputDirectory);

    var previous = ReadManifest(manifestPath);
    var written = new List<string>();

    foreach (var document in documents)
    {
      var path = Path.GetFullPath(Path.Combine(outputDirectory, document.FileName));
      var bytes = document.ToUtf8Bytes();

      // Only touch the file when its content actually changed. A no-op build that rewrote identical bytes
      // would move the timestamp and make everything downstream of it look out of date.
      if (!File.Exists(path) || !File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes))
      {
        File.WriteAllBytes(path, bytes);
      }

      written.Add(path);
    }

    Prune(previous, written, outputDirectory);
    WriteManifest(manifestPath, written);

    return written;
  }

  /// <summary>
  /// Deletes documents a previous run produced and this one did not — after, say, renaming the file a type
  /// exports to. Confined to the output directory, so a manifest that has been tampered with cannot be
  /// turned into a way of deleting something else.
  /// </summary>
  private static void Prune(
    IReadOnlyList<string> previous,
    IReadOnlyList<string> written,
    string outputDirectory)
  {
    var root = Path.GetFullPath(outputDirectory);
    var current = written.ToHashSet(StringComparer.Ordinal);

    foreach (var stale in previous.Where(path => !current.Contains(path)))
    {
      if (Path.GetFullPath(stale).StartsWith(root, StringComparison.Ordinal) && File.Exists(stale))
      {
        File.Delete(stale);
      }
    }
  }

  private static IReadOnlyList<string> ReadManifest(string manifestPath) =>
    string.IsNullOrEmpty(manifestPath) || !File.Exists(manifestPath)
      ? []
      : [.. File.ReadAllLines(manifestPath).Where(line => !string.IsNullOrWhiteSpace(line))];

  private static void WriteManifest(string manifestPath, IReadOnlyList<string> written)
  {
    if (string.IsNullOrEmpty(manifestPath))
    {
      return;
    }

    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(manifestPath))!);
    File.WriteAllLines(manifestPath, written);
  }
}
