namespace DotNetSchema.Fixtures.Schema;

/// <summary>
/// Per-item readings taken during one sitting. Carries exactly one collection of every portable
/// primitive type; all series in a single record share the same length.
/// </summary>
public sealed record ScalarSeries
{
  /// <summary><c>bool</c> — whether each item was attempted.</summary>
  public required IReadOnlyList<bool> AttemptedFlags { get; init; }

  /// <summary><c>sbyte</c> — per-item curve adjustments.</summary>
  public required IReadOnlyList<sbyte> CurveAdjustments { get; init; }

  /// <summary><c>byte</c> — per-item raw marks.</summary>
  public required IReadOnlyList<byte> RawScores { get; init; }

  /// <summary><c>short</c> — seat used for each item in a rotating practical.</summary>
  public required IReadOnlyList<short> SeatNumbers { get; init; }

  /// <summary><c>ushort</c> — attempts made on each item.</summary>
  public required IReadOnlyList<ushort> AttemptCounts { get; init; }

  /// <summary><c>int</c> — points available on each item.</summary>
  public required IReadOnlyList<int> PointsPerItem { get; init; }

  /// <summary><c>uint</c> — submission counter as each item was saved.</summary>
  public required IReadOnlyList<uint> SubmissionSequences { get; init; }

  /// <summary><c>long</c> — ticks spent on each item.</summary>
  public required IReadOnlyList<long> ElapsedTicksPerItem { get; init; }

  /// <summary><c>ulong</c> — per-item checksums.</summary>
  public required IReadOnlyList<ulong> IntegrityChecksums { get; init; }

  /// <summary><c>float</c> — per-item percentile ranks.</summary>
  public required IReadOnlyList<float> PercentileRanks { get; init; }

  /// <summary><c>double</c> — per-item scaled scores.</summary>
  public required IReadOnlyList<double> ScaledScores { get; init; }

  /// <summary><c>decimal</c> — per-item weighted contributions.</summary>
  public required IReadOnlyList<decimal> WeightedAverages { get; init; }

  /// <summary><c>char</c> — per-item letter grades.</summary>
  public required IReadOnlyList<char> LetterGrades { get; init; }

  /// <summary><c>string</c> — per-item marker remarks.</summary>
  public required IReadOnlyList<string> Remarks { get; init; }

  /// <summary><c>Guid</c> — identity of each item response.</summary>
  public required IReadOnlyList<Guid> ResponseIds { get; init; }

  /// <summary><c>DateOnly</c> — day each item was marked.</summary>
  public required IReadOnlyList<DateOnly> MarkedOn { get; init; }

  /// <summary><c>TimeOnly</c> — wall-clock time each item was opened.</summary>
  public required IReadOnlyList<TimeOnly> OpenedAt { get; init; }

  /// <summary><c>TimeSpan</c> — time taken on each item.</summary>
  public required IReadOnlyList<TimeSpan> Durations { get; init; }

  /// <summary><c>DateTime</c> — moment each item was filed, always in UTC.</summary>
  public required IReadOnlyList<DateTime> RecordedAtUtc { get; init; }

  /// <summary><c>DateTimeOffset</c> — moment each item was saved, with the sitting's local offset.</summary>
  public required IReadOnlyList<DateTimeOffset> SavedAt { get; init; }

  /// <summary><c>byte[]</c> — scanned answer sheet, as an opaque binary payload.</summary>
  public required IReadOnlyList<byte> AnswerSheetScan { get; init; }
}
