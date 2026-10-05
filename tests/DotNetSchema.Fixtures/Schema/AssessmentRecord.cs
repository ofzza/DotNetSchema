using DotNetSchema;

namespace DotNetSchema.Fixtures.Schema;

/// <summary>
/// The marking record behind an <see cref="Assessment" />. It is the schema's deliberate coverage point
/// for the portable primitive types: every supported scalar appears once as a value, once inside a
/// collection and once inside a map.
/// </summary>
[DotNetSchema("assessment.json")]
public sealed record AssessmentRecord
{
  /// <summary>Headline figures for the sitting — one property per portable scalar type.</summary>
  public required ScalarBattery Battery { get; init; }

  /// <summary>Per-item readings taken during the sitting — one collection per portable scalar type.</summary>
  public required ScalarSeries Series { get; init; }

  /// <summary>Per-item lookups keyed by item code — one map per portable scalar type.</summary>
  public required ScalarRegister Register { get; init; }
}
