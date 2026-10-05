using DotNetSchema.Fixtures.Schema.Enums;
using DotNetSchema;

namespace DotNetSchema.Fixtures.Schema;

/// <summary>
/// Per-item lookups for one sitting, keyed by item code (e.g. "Q07"). Carries exactly one map of every
/// portable primitive type, plus a handful of maps with non-string keys to exercise key handling.
/// </summary>
[DotNetSchema]
public sealed record ScalarRegister
{
  /// <summary><c>bool</c> — whether each item was answered.</summary>
  public required IReadOnlyDictionary<string, bool> AnsweredByItem { get; init; }

  /// <summary><c>sbyte</c> — curve adjustment applied to each item.</summary>
  public required IReadOnlyDictionary<string, sbyte> CurveByItem { get; init; }

  /// <summary><c>byte</c> — raw mark of each item.</summary>
  public required IReadOnlyDictionary<string, byte> RawScoreByItem { get; init; }

  /// <summary><c>short</c> — seat the item was answered from.</summary>
  public required IReadOnlyDictionary<string, short> SeatByItem { get; init; }

  /// <summary><c>ushort</c> — attempts made on each item.</summary>
  public required IReadOnlyDictionary<string, ushort> AttemptsByItem { get; init; }

  /// <summary><c>int</c> — points available on each item.</summary>
  public required IReadOnlyDictionary<string, int> PointsByItem { get; init; }

  /// <summary><c>uint</c> — submission counter at which each item was saved.</summary>
  public required IReadOnlyDictionary<string, uint> SequenceByItem { get; init; }

  /// <summary><c>long</c> — ticks spent on each item.</summary>
  public required IReadOnlyDictionary<string, long> ElapsedTicksByItem { get; init; }

  /// <summary><c>ulong</c> — checksum of each item response.</summary>
  public required IReadOnlyDictionary<string, ulong> ChecksumByItem { get; init; }

  /// <summary><c>float</c> — percentile rank of each item.</summary>
  public required IReadOnlyDictionary<string, float> PercentileByItem { get; init; }

  /// <summary><c>double</c> — scaled score of each item.</summary>
  public required IReadOnlyDictionary<string, double> ScaledScoreByItem { get; init; }

  /// <summary><c>decimal</c> — weight of each item in the final mark.</summary>
  public required IReadOnlyDictionary<string, decimal> WeightByItem { get; init; }

  /// <summary><c>char</c> — letter grade of each item.</summary>
  public required IReadOnlyDictionary<string, char> LetterGradeByItem { get; init; }

  /// <summary><c>string</c> — marker's remark on each item.</summary>
  public required IReadOnlyDictionary<string, string> RemarkByItem { get; init; }

  /// <summary><c>Guid</c> — identity of each item response.</summary>
  public required IReadOnlyDictionary<string, Guid> ResponseIdByItem { get; init; }

  /// <summary><c>DateOnly</c> — day each item was marked.</summary>
  public required IReadOnlyDictionary<string, DateOnly> MarkedOnByItem { get; init; }

  /// <summary><c>TimeOnly</c> — wall-clock time each item was opened.</summary>
  public required IReadOnlyDictionary<string, TimeOnly> OpenedAtByItem { get; init; }

  /// <summary><c>TimeSpan</c> — time taken on each item.</summary>
  public required IReadOnlyDictionary<string, TimeSpan> DurationByItem { get; init; }

  /// <summary><c>DateTime</c> — moment each item was filed, always in UTC.</summary>
  public required IReadOnlyDictionary<string, DateTime> RecordedAtUtcByItem { get; init; }

  /// <summary><c>DateTimeOffset</c> — moment each item was saved, with the sitting's local offset.</summary>
  public required IReadOnlyDictionary<string, DateTimeOffset> SavedAtByItem { get; init; }

  /// <summary>Map with an <c>int</c> key — remark by 1-based item number.</summary>
  public required IReadOnlyDictionary<int, string> RemarkByItemNumber { get; init; }

  /// <summary>Map with a <c>Guid</c> key — scaled score by response identity.</summary>
  public required IReadOnlyDictionary<Guid, double> ScoreByResponseId { get; init; }

  /// <summary>Map with an <c>enum</c> key — weighting policy per assessment form.</summary>
  public required IReadOnlyDictionary<AssessmentKind, decimal> WeightByAssessmentKind { get; init; }

  /// <summary>Map with a <c>DateOnly</c> key — heads counted at each sitting of the paper.</summary>
  public required IReadOnlyDictionary<DateOnly, ushort> AttendanceByDate { get; init; }
}
