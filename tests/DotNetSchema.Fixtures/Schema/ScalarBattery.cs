namespace DotNetSchema.Fixtures.Schema;

/// <summary>
/// Headline figures for one sitting of an assessment. Carries exactly one property of every portable
/// primitive type the fixtures cover.
/// </summary>
public sealed record ScalarBattery
{
  /// <summary><c>bool</c> — whether an invigilator was present.</summary>
  public required bool IsProctored { get; init; }

  /// <summary><c>sbyte</c> — marks added or removed by the curve, -128..127.</summary>
  public required sbyte CurveAdjustment { get; init; }

  /// <summary><c>byte</c> — raw mark before scaling, 0..255.</summary>
  public required byte RawScore { get; init; }

  /// <summary><c>short</c> — allocated seat; negative values mean a remote sitting.</summary>
  public required short SeatNumber { get; init; }

  /// <summary><c>ushort</c> — number of items on the paper.</summary>
  public required ushort ItemCount { get; init; }

  /// <summary><c>int</c> — total points available on the paper.</summary>
  public required int TotalPoints { get; init; }

  /// <summary><c>uint</c> — monotonic submission counter for the sitting.</summary>
  public required uint SubmissionSequence { get; init; }

  /// <summary><c>long</c> — time spent on the paper, in ticks.</summary>
  public required long ElapsedTicks { get; init; }

  /// <summary><c>ulong</c> — checksum of the answer sheet as filed.</summary>
  public required ulong IntegrityChecksum { get; init; }

  /// <summary><c>float</c> — percentile rank within the cohort.</summary>
  public required float PercentileRank { get; init; }

  /// <summary><c>double</c> — scaled score on the reporting scale.</summary>
  public required double ScaledScore { get; init; }

  /// <summary><c>decimal</c> — weighted contribution to the final mark; exact, not binary floating point.</summary>
  public required decimal WeightedAverage { get; init; }

  /// <summary><c>char</c> — letter grade awarded, 'A'..'F'.</summary>
  public required char LetterGrade { get; init; }

  /// <summary><c>string</c> — marker's remarks.</summary>
  public required string Remarks { get; init; }

  /// <summary><c>Guid</c> — identity of the response as filed.</summary>
  public required Guid ResponseId { get; init; }

  /// <summary><c>DateOnly</c> — day the paper was sat.</summary>
  public required DateOnly AdministeredOn { get; init; }

  /// <summary><c>TimeOnly</c> — wall-clock time the sitting started.</summary>
  public required TimeOnly StartedAt { get; init; }

  /// <summary><c>TimeSpan</c> — permitted duration of the sitting.</summary>
  public required TimeSpan AllowedDuration { get; init; }

  /// <summary><c>DateTime</c> — moment the mark was filed, always in UTC.</summary>
  public required DateTime RecordedAtUtc { get; init; }

  /// <summary><c>DateTimeOffset</c> — moment the paper was handed in, with the sitting's local offset.</summary>
  public required DateTimeOffset SubmittedAt { get; init; }
}
