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
/// <param name="DecoratorRules">Decorator selection rules, in the order they were written.</param>
/// <param name="TypeMappings">Types described by a declared inline schema, in configuration order.</param>
public sealed record ToolArguments(
  IReadOnlyList<string> Scan,
  IReadOnlyList<string> References,
  string OutputDirectory,
  string DefaultFileName,
  string ManifestPath,
  bool FailOnEmpty,
  EnumStyle EnumStyle,
  IReadOnlyList<DecoratorRule> DecoratorRules,
  IReadOnlyList<TypeMapping> TypeMappings)
{
  /// <summary>
  /// Parses <c>--key=value</c> arguments, expanding any <c>@file</c> into one argument per line.
  /// </summary>
  /// <remarks>
  /// The build always passes a response file. A full framework closure runs to well over a hundred paths,
  /// which exceeds the command-line length limit on Windows, and one <c>--key=value</c> per line needs no
  /// quoting and never reaches a shell.
  /// <para>
  /// Each <c>--decorator</c> is one <c>DotNetSchemaDecorator</c> item, as <c>true|pattern</c> or
  /// <c>false|pattern</c>. Their order is the selection's meaning — the last matching rule wins — so they are
  /// kept exactly in the order given.
  /// </para>
  /// <para>
  /// Each <c>--type-mapping</c> is one <c>DotNetSchemaType</c> item, as <c>name|type|format|pattern</c>. The
  /// pattern goes last because it is the one field that may itself contain a <c>|</c>; a split into at most
  /// four parts leaves it whole. Validation is the generator's, which reports a bad one as a diagnostic.
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
    List<DecoratorRule> decoratorRules = [];
    List<TypeMapping> typeMappings = [];

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
        case "--decorator":
          decoratorRules.Add(ParseDecoratorRule(value));
          break;
        case "--type-mapping":
          typeMappings.Add(ParseTypeMapping(value));
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
      decoratorRules,
      typeMappings);
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

  private static TypeMapping ParseTypeMapping(string value)
  {
    var parts = value.Split('|', 4);
    string? Part(int index) => parts.Length > index && parts[index].Length > 0 ? parts[index] : null;

    return new TypeMapping(parts[0].Trim(), Part(1) ?? string.Empty, Part(2), Part(3));
  }

  /// <summary>Reads a <c>true|pattern</c> or <c>false|pattern</c> decorator rule.</summary>
  /// <remarks>
  /// Strict, unlike a missing optional field elsewhere: a rule that silently read as the opposite of what was
  /// written would invert the selection with no sign of it in the output.
  /// </remarks>
  private static DecoratorRule ParseDecoratorRule(string value)
  {
    var separator = value.IndexOf('|');
    var include = separator < 0 ? value : value[..separator];
    var pattern = separator < 0 ? string.Empty : value[(separator + 1)..].Trim();

    if (pattern.Length == 0)
    {
      throw new ArgumentException(
        $"The DotNetSchemaDecorator item '{include}' has no Attribute; expected a full name or a pattern.");
    }

    if (string.Equals(include.Trim(), "true", StringComparison.OrdinalIgnoreCase))
    {
      return new DecoratorRule(pattern, Include: true);
    }

    if (string.Equals(include.Trim(), "false", StringComparison.OrdinalIgnoreCase))
    {
      return new DecoratorRule(pattern, Include: false);
    }

    throw new ArgumentException(
      $"The DotNetSchemaDecorator item for '{pattern}' has Include=\"{include}\"; expected true or false.");
  }

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
