using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DotNetSchema.Fixtures.Schema.Decorators;

/// <summary>
/// A positional record outside the school graph, for the decorator cases the graph cannot carry without
/// changing what it means or what <c>FixturesService</c> generates for it.
/// </summary>
/// <remarks>
/// On a positional record an attribute written without a target lands on the primary-constructor
/// <em>parameter</em>, not on the property — <c>Rank</c> relies on the generator reading it from there.
/// </remarks>
/// <param name="Href">A <c>[property: Url]</c>, mapped to <c>format: uri</c>.</param>
/// <param name="Rank">A <c>[Range]</c> on the parameter, not the property, still mapped.</param>
/// <param name="Label">
/// A DataAnnotations <c>[Required]</c> on an optional, nullable property, which must leave it both optional
/// and nullable: <c>required</c> follows the C# modifier and <c>[JsonRequired]</c> alone.
/// </param>
/// <param name="Note">Deprecated and described, both through attributes.</param>
/// <param name="Tags">
/// Tagged on the parameter, on the property, and identically on both (listed once). Its
/// <c>[DotNetSchemaFormat]</c>, on the parameter, constrains the elements rather than the array.
/// </param>
/// <param name="Anchor">A <c>[DotNetSchemaFormat]</c> replacing the <c>uuid</c> format a <c>Guid</c> has of its own.</param>
public sealed record Bookmark(
  [property: Url] string Href,
  [Range(1, 10)] int Rank,
  [property: Required] string? Label,
  [property: Obsolete("Superseded by Label.")][property: Description("A free-form note.")] string? Note,
  [SampleTag("parameter")][property: SampleTag("property")][SampleTag("both")][property: SampleTag("both")]
  [DotNetSchemaFormat(Pattern = "^[a-z][a-z0-9-]*$")]
  IReadOnlyList<string> Tags,
  [DotNetSchemaFormat("uuid-v7")] Guid Anchor)
{
  /// <summary>
  /// Not <c>required</c> in C#, but <c>[JsonRequired]</c>: System.Text.Json refuses a payload without it,
  /// so the schema lists it as required.
  /// </summary>
  [JsonRequired]
  public int Revision { get; init; }
}
