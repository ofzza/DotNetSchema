using System.ComponentModel.DataAnnotations;

namespace DotNetSchema.Fixtures.Schema.Invalid;

/// <summary>
/// <c>[DotNetSchemaFormat]</c> where it cannot apply, and where it clashes with a DataAnnotations format.
/// Raises <c>DNS0021</c> twice and <c>DNS0015</c> once, all warnings, so the document is still produced.
/// </summary>
[ExportForTesting("misapplied-format.json")]
public sealed record MisappliedFormat
{
  /// <summary>A model: described by its own definition, which takes no format. DNS0021.</summary>
  [DotNetSchemaFormat("isbn")]
  public required FormatTarget Target { get; init; }

  /// <summary>A number with a pattern, which only a string can carry. DNS0021.</summary>
  [DotNetSchemaFormat(Pattern = "^\\d+$")]
  public required int Count { get; init; }

  /// <summary>
  /// Two formats on one string. The explicit declaration is the one kept; <c>[EmailAddress]</c> gives way,
  /// with DNS0015, exactly as it does to a <c>Guid</c>'s own format.
  /// </summary>
  [EmailAddress]
  [DotNetSchemaFormat("idn-email")]
  public required string Contact { get; init; }
}

/// <summary>A plain model, for <see cref="MisappliedFormat.Target" /> to reference.</summary>
public sealed record FormatTarget(string Name);
