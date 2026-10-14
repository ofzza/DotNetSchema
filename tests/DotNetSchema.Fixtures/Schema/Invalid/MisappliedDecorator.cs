using System.ComponentModel.DataAnnotations;

namespace DotNetSchema.Fixtures.Schema.Invalid;

/// <summary>
/// A <c>[Range]</c> on a string: legal C#, but a numeric bound has nothing to say about a JSON string.
/// Raises <c>DNS0015</c>, a warning — the attribute stays listed under <c>x-decorators</c> and the document
/// is still produced.
/// </summary>
[ExportForTesting("misapplied-decorator.json")]
public sealed record MisappliedDecorator
{
  /// <summary>A code that is text, with a range that assumes it is a number.</summary>
  [Range(1, 10)]
  public required string Code { get; init; }
}
