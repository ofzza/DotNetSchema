using DotNetSchema.Fixtures.Schema.Enums;

namespace DotNetSchema.Fixtures.Schema;

/// <summary>
/// The nullable twin of <see cref="ScalarRegister" />: the same maps, each with nullable values, and the
/// maps themselves cycling through required, optional, required-but-nullable and optional-and-nullable —
/// so <c>Record&lt;K, T | null&gt;</c> is covered for every portable primitive in all four positions. Keys
/// stay non-null: System.Text.Json cannot write a null property name.
/// </summary>
public sealed record NullableScalarRegister
{
  /// <summary>
  /// <c>bool</c> — whether each item was answered. Values are <c>null</c> where nothing was recorded. The map
  /// itself is required and never null.
  /// </summary>
  public required IReadOnlyDictionary<string, bool?> AnsweredByItem { get; init; }

  /// <summary>
  /// <c>sbyte</c> — curve adjustment applied to each item. Values are <c>null</c> where nothing was recorded.
  /// The map itself is optional but never null, and empty by default.
  /// </summary>
  public IReadOnlyDictionary<string, sbyte?> CurveByItem { get; init; } = new Dictionary<string, sbyte?>();

  /// <summary>
  /// <c>byte</c> — raw mark of each item. Values are <c>null</c> where nothing was recorded. The map itself
  /// is required but may be null.
  /// </summary>
  public required IReadOnlyDictionary<string, byte?>? RawScoreByItem { get; init; }

  /// <summary>
  /// <c>short</c> — seat the item was answered from. Values are <c>null</c> where nothing was recorded. The
  /// map itself is optional and may be null.
  /// </summary>
  public IReadOnlyDictionary<string, short?>? SeatByItem { get; init; }

  /// <summary>
  /// <c>ushort</c> — attempts made on each item. Values are <c>null</c> where nothing was recorded. The map
  /// itself is required and never null.
  /// </summary>
  public required IReadOnlyDictionary<string, ushort?> AttemptsByItem { get; init; }

  /// <summary>
  /// <c>int</c> — points available on each item. Values are <c>null</c> where nothing was recorded. The map
  /// itself is optional but never null, and empty by default.
  /// </summary>
  public IReadOnlyDictionary<string, int?> PointsByItem { get; init; } = new Dictionary<string, int?>();

  /// <summary>
  /// <c>uint</c> — submission counter at which each item was saved. Values are <c>null</c> where nothing was
  /// recorded. The map itself is required but may be null.
  /// </summary>
  public required IReadOnlyDictionary<string, uint?>? SequenceByItem { get; init; }

  /// <summary>
  /// <c>long</c> — ticks spent on each item. Values are <c>null</c> where nothing was recorded. The map
  /// itself is optional and may be null.
  /// </summary>
  public IReadOnlyDictionary<string, long?>? ElapsedTicksByItem { get; init; }

  /// <summary>
  /// <c>ulong</c> — checksum of each item response. Values are <c>null</c> where nothing was recorded. The
  /// map itself is required and never null.
  /// </summary>
  public required IReadOnlyDictionary<string, ulong?> ChecksumByItem { get; init; }

  /// <summary>
  /// <c>float</c> — percentile rank of each item. Values are <c>null</c> where nothing was recorded. The map
  /// itself is optional but never null, and empty by default.
  /// </summary>
  public IReadOnlyDictionary<string, float?> PercentileByItem { get; init; } = new Dictionary<string, float?>();

  /// <summary>
  /// <c>double</c> — scaled score of each item. Values are <c>null</c> where nothing was recorded. The map
  /// itself is required but may be null.
  /// </summary>
  public required IReadOnlyDictionary<string, double?>? ScaledScoreByItem { get; init; }

  /// <summary>
  /// <c>decimal</c> — weight of each item in the final mark. Values are <c>null</c> where nothing was
  /// recorded. The map itself is optional and may be null.
  /// </summary>
  public IReadOnlyDictionary<string, decimal?>? WeightByItem { get; init; }

  /// <summary>
  /// <c>char</c> — letter grade of each item. Values are <c>null</c> where nothing was recorded. The map
  /// itself is required and never null.
  /// </summary>
  public required IReadOnlyDictionary<string, char?> LetterGradeByItem { get; init; }

  /// <summary>
  /// <c>string</c> — marker's remark on each item. Values are <c>null</c> where nothing was recorded. The map
  /// itself is optional but never null, and empty by default.
  /// </summary>
  public IReadOnlyDictionary<string, string?> RemarkByItem { get; init; } = new Dictionary<string, string?>();

  /// <summary>
  /// <c>Guid</c> — identity of each item response. Values are <c>null</c> where nothing was recorded. The map
  /// itself is required but may be null.
  /// </summary>
  public required IReadOnlyDictionary<string, Guid?>? ResponseIdByItem { get; init; }

  /// <summary>
  /// <c>DateOnly</c> — day each item was marked. Values are <c>null</c> where nothing was recorded. The map
  /// itself is optional and may be null.
  /// </summary>
  public IReadOnlyDictionary<string, DateOnly?>? MarkedOnByItem { get; init; }

  /// <summary>
  /// <c>TimeOnly</c> — wall-clock time each item was opened. Values are <c>null</c> where nothing was
  /// recorded. The map itself is required and never null.
  /// </summary>
  public required IReadOnlyDictionary<string, TimeOnly?> OpenedAtByItem { get; init; }

  /// <summary>
  /// <c>TimeSpan</c> — time taken on each item. Values are <c>null</c> where nothing was recorded. The map
  /// itself is optional but never null, and empty by default.
  /// </summary>
  public IReadOnlyDictionary<string, TimeSpan?> DurationByItem { get; init; } = new Dictionary<string, TimeSpan?>();

  /// <summary>
  /// <c>DateTime</c> — moment each item was filed, always in UTC. Values are <c>null</c> where nothing was
  /// recorded. The map itself is required but may be null.
  /// </summary>
  public required IReadOnlyDictionary<string, DateTime?>? RecordedAtUtcByItem { get; init; }

  /// <summary>
  /// <c>DateTimeOffset</c> — moment each item was saved, with the sitting's local offset. Values are
  /// <c>null</c> where nothing was recorded. The map itself is optional and may be null.
  /// </summary>
  public IReadOnlyDictionary<string, DateTimeOffset?>? SavedAtByItem { get; init; }

  /// <summary>
  /// Map with an <c>int</c> key — remark by 1-based item number. Values are <c>null</c> where nothing was
  /// recorded. The map itself is required and never null.
  /// </summary>
  public required IReadOnlyDictionary<int, string?> RemarkByItemNumber { get; init; }

  /// <summary>
  /// Map with a <c>Guid</c> key — scaled score by response identity. Values are <c>null</c> where nothing was
  /// recorded. The map itself is optional but never null, and empty by default.
  /// </summary>
  public IReadOnlyDictionary<Guid, double?> ScoreByResponseId { get; init; } = new Dictionary<Guid, double?>();

  /// <summary>
  /// Map with an <c>enum</c> key — weighting policy per assessment form. Values are <c>null</c> where nothing
  /// was recorded. The map itself is required but may be null.
  /// </summary>
  public required IReadOnlyDictionary<AssessmentKind, decimal?>? WeightByAssessmentKind { get; init; }

  /// <summary>
  /// Map with a <c>DateOnly</c> key — heads counted at each sitting of the paper. Values are <c>null</c>
  /// where nothing was recorded. The map itself is optional and may be null.
  /// </summary>
  public IReadOnlyDictionary<DateOnly, ushort?>? AttendanceByDate { get; init; }
}
