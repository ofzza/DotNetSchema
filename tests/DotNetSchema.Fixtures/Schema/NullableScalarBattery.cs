namespace DotNetSchema.Fixtures.Schema;

/// <summary>
/// The nullable twin of <see cref="ScalarBattery" />: the same properties, each of a nullable type, and
/// alternately <c>required</c> and optional — so every portable primitive is covered as <c>x: T | null</c>
/// or <c>x?: T | null</c>, while the plain battery stays the <c>x: T</c> baseline.
/// </summary>
public sealed record NullableScalarBattery
{
  /// <summary><c>bool?</c> — whether an invigilator was present. Required but nullable.</summary>
  public required bool? IsProctored { get; init; }

  /// <summary><c>sbyte?</c> — marks added or removed by the curve, -128..127. Optional and nullable.</summary>
  public sbyte? CurveAdjustment { get; init; }

  /// <summary><c>byte?</c> — raw mark before scaling, 0..255. Required but nullable.</summary>
  public required byte? RawScore { get; init; }

  /// <summary><c>short?</c> — allocated seat; negative values mean a remote sitting. Optional and nullable.</summary>
  public short? SeatNumber { get; init; }

  /// <summary><c>ushort?</c> — number of items on the paper. Required but nullable.</summary>
  public required ushort? ItemCount { get; init; }

  /// <summary><c>int?</c> — total points available on the paper. Optional and nullable.</summary>
  public int? TotalPoints { get; init; }

  /// <summary><c>uint?</c> — monotonic submission counter for the sitting. Required but nullable.</summary>
  public required uint? SubmissionSequence { get; init; }

  /// <summary><c>long?</c> — time spent on the paper, in ticks. Optional and nullable.</summary>
  public long? ElapsedTicks { get; init; }

  /// <summary><c>ulong?</c> — checksum of the answer sheet as filed. Required but nullable.</summary>
  public required ulong? IntegrityChecksum { get; init; }

  /// <summary><c>float?</c> — percentile rank within the cohort. Optional and nullable.</summary>
  public float? PercentileRank { get; init; }

  /// <summary><c>double?</c> — scaled score on the reporting scale. Required but nullable.</summary>
  public required double? ScaledScore { get; init; }

  /// <summary>
  /// <c>decimal?</c> — weighted contribution to the final mark; exact, not binary floating point. Optional
  /// and nullable.
  /// </summary>
  public decimal? WeightedAverage { get; init; }

  /// <summary><c>char?</c> — letter grade awarded, 'A'..'F'. Required but nullable.</summary>
  public required char? LetterGrade { get; init; }

  /// <summary><c>string?</c> — marker's remarks. Optional and nullable.</summary>
  public string? Remarks { get; init; }

  /// <summary><c>Guid?</c> — identity of the response as filed. Required but nullable.</summary>
  public required Guid? ResponseId { get; init; }

  /// <summary><c>DateOnly?</c> — day the paper was sat. Optional and nullable.</summary>
  public DateOnly? AdministeredOn { get; init; }

  /// <summary><c>TimeOnly?</c> — wall-clock time the sitting started. Required but nullable.</summary>
  public required TimeOnly? StartedAt { get; init; }

  /// <summary><c>TimeSpan?</c> — permitted duration of the sitting. Optional and nullable.</summary>
  public TimeSpan? AllowedDuration { get; init; }

  /// <summary><c>DateTime?</c> — moment the mark was filed, always in UTC. Required but nullable.</summary>
  public required DateTime? RecordedAtUtc { get; init; }

  /// <summary>
  /// <c>DateTimeOffset?</c> — moment the paper was handed in, with the sitting's local offset. Optional and
  /// nullable.
  /// </summary>
  public DateTimeOffset? SubmittedAt { get; init; }
}
