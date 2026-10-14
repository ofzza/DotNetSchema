using DotNetSchema.Generator;

namespace DotNetSchema.Tool;

/// <summary>Everything the export run was told to do.</summary>
/// <param name="Scan">Assemblies to search for marked types.</param>
/// <param name="References">Assemblies needed to resolve what the scanned ones mention.</param>
/// <param name="OutputDirectory">Where the documents are written.</param>
/// <param name="DefaultFileName">Document a marked type without a named file belongs to.</param>
/// <param name="ManifestPath">File recording what was produced, so the build can track and prune it.</param>
/// <param name="FailOnEmpty">Whether producing nothing is an error.</param>
/// <param name="EnumStyle">The shape enum values take in the documents.</param>
/// <param name="Decorators">Attribute patterns replacing the default decorator list, or <see langword="null" /> to keep it.</param>
/// <param name="DecoratorsAllow">Attribute patterns added to the decorator list.</param>
/// <param name="DecoratorsDeny">Attribute patterns removed from the decorator list.</param>
public sealed record ToolArguments(
  IReadOnlyList<string> Scan,
  IReadOnlyList<string> References,
  string OutputDirectory,
  string DefaultFileName,
  string ManifestPath,
  bool FailOnEmpty,
  EnumStyle EnumStyle,
  IReadOnlyList<string>? Decorators,
  IReadOnlyList<string> DecoratorsAllow,
  IReadOnlyList<string> DecoratorsDeny)
{
  /// <summary>
  /// Parses <c>--key=value</c> arguments, expanding any <c>@file</c> into one argument per line.
  /// </summary>
  /// <remarks>
  /// The build always passes a response file. A full framework closure runs to well over a hundred paths,
  /// which exceeds the command-line length limit on Windows, and one <c>--key=value</c> per line needs no
  /// quoting and never reaches a shell.
  /// <para>
  /// The decorator arguments each carry a <c>;</c>-separated list, and may be repeated; the build passes an
  /// MSBuild property through as one line. <c>--decorators</c> given at all, even empty, replaces the
  /// default list.
  /// </para>
  /// </remarks>
  public static ToolArguments Parse(IEnumerable<string> arguments)
  {
    List<string> scan = [];
    List<string> references = [];
    var outputDirectory = Directory.GetCurrentDirectory();
    var defaultFileName = "schema.json";
    var manifestPath = string.Empty;
    var failOnEmpty = false;
    var enumStyle = EnumStyle.OneOfConstTitle;
    List<string>? decorators = null;
    List<string> decoratorsAllow = [];
    List<string> decoratorsDeny = [];

    foreach (var argument in Expand(arguments))
    {
      var separator = argument.IndexOf('=');
      var key = separator < 0 ? argument : argument[..separator];
      var value = separator < 0 ? string.Empty : argument[(separator + 1)..];

      switch (key)
      {
        case "--scan":
          scan.Add(value);
          break;
        case "--reference":
          references.Add(value);
          break;
        case "--output-directory":
          outputDirectory = value;
          break;
        case "--default-file-name":
          defaultFileName = value;
          break;
        case "--manifest":
          manifestPath = value;
          break;
        case "--fail-on-empty":
          failOnEmpty = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
          break;
        case "--enum-style":
          enumStyle = ParseEnumStyle(value);
          break;
        case "--decorators":
          (decorators ??= []).AddRange(Patterns(value));
          break;
        case "--decorators-allow":
          decoratorsAllow.AddRange(Patterns(value));
          break;
        case "--decorators-deny":
          decoratorsDeny.AddRange(Patterns(value));
          break;
        default:
          throw new ArgumentException($"Unrecognised argument '{key}'.");
      }
    }

    return new ToolArguments(
      scan,
      references,
      outputDirectory,
      defaultFileName,
      manifestPath,
      failOnEmpty,
      enumStyle,
      decorators,
      decoratorsAllow,
      decoratorsDeny);
  }

  /// <summary>Reads an <see cref="EnumStyle" /> by member name, ignoring case.</summary>
  /// <remarks>
  /// Matched against the declared names rather than through <c>Enum.TryParse</c>, which would also accept a
  /// number — and a number would quietly select whichever style happens to sit at that position.
  /// </remarks>
  private static EnumStyle ParseEnumStyle(string value) =>
    Enum.GetNames<EnumStyle>().FirstOrDefault(n => string.Equals(n, value, StringComparison.OrdinalIgnoreCase)) is { } name
      ? Enum.Parse<EnumStyle>(name)
      : throw new ArgumentException(
        $"Unrecognised enum style '{value}'; expected one of {string.Join(", ", Enum.GetNames<EnumStyle>())}.");

  private static IEnumerable<string> Patterns(string value) =>
    value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

  private static IEnumerable<string> Expand(IEnumerable<string> arguments)
  {
    foreach (var argument in arguments)
    {
      if (!argument.StartsWith('@'))
      {
        yield return argument;
        continue;
      }

      foreach (var line in File.ReadLines(argument[1..]))
      {
        if (!string.IsNullOrWhiteSpace(line))
        {
          yield return line.Trim();
        }
      }
    }
  }
}
