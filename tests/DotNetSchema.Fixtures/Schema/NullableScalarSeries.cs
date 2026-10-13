namespace DotNetSchema.Fixtures.Schema;

/// <summary>
/// The nullable twin of <see cref="ScalarSeries" />: the same series, each with nullable items, and the
/// series themselves cycling through required, optional, required-but-nullable and optional-and-nullable
/// — so <c>Array&lt;T | null&gt;</c> is covered for every portable primitive in all four positions. The one
/// exception is <see cref="AnswerSheetScan" />, a base64 blob with no items to null, which is nullable as a
/// whole instead.
/// </summary>
public sealed record NullableScalarSeries
{
  /// <summary>
  /// <c>bool</c> — whether each item was attempted. Items are <c>null</c> where nothing was recorded. The
  /// series itself is required and never null.
  /// </summary>
  public required IReadOnlyList<bool?> AttemptedFlags { get; init; }

  /// <summary>
  /// <c>sbyte</c> — per-item curve adjustments. Items are <c>null</c> where nothing was recorded. The series
  /// itself is optional but never null, and empty by default.
  /// </summary>
  public IReadOnlyList<sbyte?> CurveAdjustments { get; init; } = [];

  /// <summary>
  /// <c>byte</c> — per-item raw marks. Items are <c>null</c> where nothing was recorded. The series itself is
  /// required but may be null.
  /// </summary>
  public required IReadOnlyList<byte?>? RawScores { get; init; }

  /// <summary>
  /// <c>short</c> — seat used for each item in a rotating practical. Items are <c>null</c> where nothing was
  /// recorded. The series itself is optional and may be null.
  /// </summary>
  public IReadOnlyList<short?>? SeatNumbers { get; init; }

  /// <summary>
  /// <c>ushort</c> — attempts made on each item. Items are <c>null</c> where nothing was recorded. The series
  /// itself is required and never null.
  /// </summary>
  public required IReadOnlyList<ushort?> AttemptCounts { get; init; }

  /// <summary>
  /// <c>int</c> — points available on each item. Items are <c>null</c> where nothing was recorded. The series
  /// itself is optional but never null, and empty by default.
  /// </summary>
  public IReadOnlyList<int?> PointsPerItem { get; init; } = [];

  /// <summary>
  /// <c>uint</c> — submission counter as each item was saved. Items are <c>null</c> where nothing was
  /// recorded. The series itself is required but may be null.
  /// </summary>
  public required IReadOnlyList<uint?>? SubmissionSequences { get; init; }

  /// <summary>
  /// <c>long</c> — ticks spent on each item. Items are <c>null</c> where nothing was recorded. The series
  /// itself is optional and may be null.
  /// </summary>
  public IReadOnlyList<long?>? ElapsedTicksPerItem { get; init; }

  /// <summary>
  /// <c>ulong</c> — per-item checksums. Items are <c>null</c> where nothing was recorded. The series itself
  /// is required and never null.
  /// </summary>
  public required IReadOnlyList<ulong?> IntegrityChecksums { get; init; }

  /// <summary>
  /// <c>float</c> — per-item percentile ranks. Items are <c>null</c> where nothing was recorded. The series
  /// itself is optional but never null, and empty by default.
  /// </summary>
  public IReadOnlyList<float?> PercentileRanks { get; init; } = [];

  /// <summary>
  /// <c>double</c> — per-item scaled scores. Items are <c>null</c> where nothing was recorded. The series
  /// itself is required but may be null.
  /// </summary>
  public required IReadOnlyList<double?>? ScaledScores { get; init; }

  /// <summary>
  /// <c>decimal</c> — per-item weighted contributions. Items are <c>null</c> where nothing was recorded. The
  /// series itself is optional and may be null.
  /// </summary>
  public IReadOnlyList<decimal?>? WeightedAverages { get; init; }

  /// <summary>
  /// <c>char</c> — per-item letter grades. Items are <c>null</c> where nothing was recorded. The series
  /// itself is required and never null.
  /// </summary>
  public required IReadOnlyList<char?> LetterGrades { get; init; }

  /// <summary>
  /// <c>string</c> — per-item marker remarks. Items are <c>null</c> where nothing was recorded. The series
  /// itself is optional but never null, and empty by default.
  /// </summary>
  public IReadOnlyList<string?> Remarks { get; init; } = [];

  /// <summary>
  /// <c>Guid</c> — identity of each item response. Items are <c>null</c> where nothing was recorded. The
  /// series itself is required but may be null.
  /// </summary>
  public required IReadOnlyList<Guid?>? ResponseIds { get; init; }

  /// <summary>
  /// <c>DateOnly</c> — day each item was marked. Items are <c>null</c> where nothing was recorded. The series
  /// itself is optional and may be null.
  /// </summary>
  public IReadOnlyList<DateOnly?>? MarkedOn { get; init; }

  /// <summary>
  /// <c>TimeOnly</c> — wall-clock time each item was opened. Items are <c>null</c> where nothing was
  /// recorded. The series itself is required and never null.
  /// </summary>
  public required IReadOnlyList<TimeOnly?> OpenedAt { get; init; }

  /// <summary>
  /// <c>TimeSpan</c> — time taken on each item. Items are <c>null</c> where nothing was recorded. The series
  /// itself is optional but never null, and empty by default.
  /// </summary>
  public IReadOnlyList<TimeSpan?> Durations { get; init; } = [];

  /// <summary>
  /// <c>DateTime</c> — moment each item was filed, always in UTC. Items are <c>null</c> where nothing was
  /// recorded. The series itself is required but may be null.
  /// </summary>
  public required IReadOnlyList<DateTime?>? RecordedAtUtc { get; init; }

  /// <summary>
  /// <c>DateTimeOffset</c> — moment each item was saved, with the sitting's local offset. Items are
  /// <c>null</c> where nothing was recorded. The series itself is optional and may be null.
  /// </summary>
  public IReadOnlyList<DateTimeOffset?>? SavedAt { get; init; }

  /// <summary>
  /// <c>byte[]?</c> — scanned answer sheet, as an opaque binary payload. A base64 string has no items to
  /// null, so the blob itself is required but nullable: <c>null</c> when no sheet was scanned.
  /// </summary>
  public required byte[]? AnswerSheetScan { get; init; }
}
