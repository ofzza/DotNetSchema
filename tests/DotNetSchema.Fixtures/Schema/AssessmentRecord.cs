using DotNetSchema;

namespace DotNetSchema.Fixtures.Schema;

/// <summary>
/// The marking record behind an <see cref="Assessment" />. It is the schema's deliberate coverage point
/// for the portable primitive types: every supported scalar appears once as a value, once inside a
/// collection and once inside a map — and then all three again in a nullable twin.
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

  /// <summary>The headline figures again, every one nullable — required and optional in turn.</summary>
  public required NullableScalarBattery NullableBattery { get; init; }

  /// <summary>The per-item readings again, with nullable items in nullable and optional series.</summary>
  public required NullableScalarSeries NullableSeries { get; init; }

  /// <summary>The per-item lookups again, with nullable values in nullable and optional maps.</summary>
  public required NullableScalarRegister NullableRegister { get; init; }
}
