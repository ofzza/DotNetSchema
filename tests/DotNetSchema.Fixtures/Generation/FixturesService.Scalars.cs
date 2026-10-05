using DotNetSchema.Fixtures.Schema;
using DotNetSchema.Fixtures.Schema.Enums;
using DotNetSchema.Fixtures.Generation;

namespace DotNetSchema.Fixtures.Generation;

/// <summary>
/// Generation of the scalar coverage bundles. Every portable primitive the fixtures support is produced
/// here three times over: once as a value, once inside a collection and once inside a map.
/// </summary>
public sealed partial class FixturesService
{
  private static AssessmentRecord GenerateAssessmentRecord(FixtureSeed seed, FixtureOptions options)
  {
    return new AssessmentRecord
    {
      Battery = GenerateScalarBattery(seed.Derive(nameof(AssessmentRecord.Battery))),
      Series = GenerateScalarSeries(seed.Derive(nameof(AssessmentRecord.Series)), options),
      Register = GenerateScalarRegister(seed.Derive(nameof(AssessmentRecord.Register)), options)
    };
  }

  private static ScalarBattery GenerateScalarBattery(FixtureSeed seed)
  {
    var random = new StableRandom(seed.Value);

    return new ScalarBattery
    {
      IsProctored = random.NextBool(0.65d),
      CurveAdjustment = random.NextSByte(-12, 12),
      RawScore = random.NextByte(0, 100),
      SeatNumber = random.NextInt16(-1, 400),
      ItemCount = random.NextUInt16(5, 120),
      TotalPoints = random.NextInt32(20, 400),
      SubmissionSequence = random.NextUInt32(),
      ElapsedTicks = random.NextInt64(TimeSpan.TicksPerMinute * 5L, TimeSpan.TicksPerHour * 4L),
      IntegrityChecksum = random.NextUInt64(),
      PercentileRank = random.NextSingle(0f, 100f, 2),
      ScaledScore = random.NextDouble(200d, 800d, 3),
      WeightedAverage = random.NextDecimal(0m, 100m, 4),
      LetterGrade = random.NextChar(FixtureVocabulary.GradeLetters),
      Remarks = random.Pick(FixtureVocabulary.MarkerRemarks),
      ResponseId = random.NextGuid(),
      AdministeredOn = random.NextDateOnly(FixtureVocabulary.Epoch, 2_200),
      StartedAt = random.NextTimeOnly(8, 18, 5),
      AllowedDuration = random.NextTimeSpan(30, 240),
      RecordedAtUtc = random.NextDateTimeUtc(FixtureVocabulary.EpochUtc, 2_200),
      SubmittedAt = random.NextDateTimeOffset(FixtureVocabulary.EpochUtc, 2_200)
    };
  }

  private static ScalarSeries GenerateScalarSeries(FixtureSeed seed, FixtureOptions options)
  {
    var length = Math.Max(0, options.SeriesLength);

    // Each series draws from a stream of its own, keyed by the property name, so adding or removing a
    // series leaves the others exactly where they were.
    IReadOnlyList<T> Series<T>(string edge, Func<StableRandom, T> draw)
    {
      return GenerateSeries(seed, edge, length, (random, _) => draw(random));
    }

    return new ScalarSeries
    {
      AttemptedFlags = Series(nameof(ScalarSeries.AttemptedFlags), r => r.NextBool(0.8d)),
      CurveAdjustments = Series(nameof(ScalarSeries.CurveAdjustments), r => r.NextSByte(-8, 8)),
      RawScores = Series(nameof(ScalarSeries.RawScores), r => r.NextByte(0, 100)),
      SeatNumbers = Series(nameof(ScalarSeries.SeatNumbers), r => r.NextInt16(-1, 400)),
      AttemptCounts = Series(nameof(ScalarSeries.AttemptCounts), r => r.NextUInt16(0, 6)),
      PointsPerItem = Series(nameof(ScalarSeries.PointsPerItem), r => r.NextInt32(1, 40)),
      SubmissionSequences = Series(nameof(ScalarSeries.SubmissionSequences), r => r.NextUInt32()),
      ElapsedTicksPerItem = Series(
            nameof(ScalarSeries.ElapsedTicksPerItem),
            r => r.NextInt64(TimeSpan.TicksPerSecond * 10L, TimeSpan.TicksPerMinute * 25L)),
      IntegrityChecksums = Series(nameof(ScalarSeries.IntegrityChecksums), r => r.NextUInt64()),
      PercentileRanks = Series(nameof(ScalarSeries.PercentileRanks), r => r.NextSingle(0f, 100f, 2)),
      ScaledScores = Series(nameof(ScalarSeries.ScaledScores), r => r.NextDouble(0d, 100d, 3)),
      WeightedAverages = Series(nameof(ScalarSeries.WeightedAverages), r => r.NextDecimal(0m, 1m, 4)),
      LetterGrades = Series(nameof(ScalarSeries.LetterGrades), r => r.NextChar(FixtureVocabulary.GradeLetters)),
      Remarks = Series(nameof(ScalarSeries.Remarks), r => r.Pick(FixtureVocabulary.MarkerRemarks)),
      ResponseIds = Series(nameof(ScalarSeries.ResponseIds), r => r.NextGuid()),
      MarkedOn = Series(nameof(ScalarSeries.MarkedOn), r => r.NextDateOnly(FixtureVocabulary.Epoch, 2_200)),
      OpenedAt = Series(nameof(ScalarSeries.OpenedAt), r => r.NextTimeOnly(8, 18, 5)),
      Durations = Series(nameof(ScalarSeries.Durations), r => r.NextTimeSpan(1, 45)),
      RecordedAtUtc = Series(
            nameof(ScalarSeries.RecordedAtUtc),
            r => r.NextDateTimeUtc(FixtureVocabulary.EpochUtc, 2_200)),
      SavedAt = Series(
            nameof(ScalarSeries.SavedAt),
            r => r.NextDateTimeOffset(FixtureVocabulary.EpochUtc, 2_200)),

      // The blob is deliberately longer than the rest; it stands in for an opaque binary payload
      // rather than for a per-item reading.
      AnswerSheetScan = GenerateSeries(
            seed,
            nameof(ScalarSeries.AnswerSheetScan),
            length * 8,
            (random, _) => random.NextByte())
    };
  }

  private static ScalarRegister GenerateScalarRegister(FixtureSeed seed, FixtureOptions options)
  {
    var count = Math.Max(0, options.MapSize);

    // Every string-keyed register is laid out the same way: keys "Q01", "Q02", … and a stream of its own.
    IReadOnlyDictionary<string, T> ByItem<T>(string edge, Func<StableRandom, T> draw)
    {
      return GenerateItemMap(seed, edge, count, (random, _) => draw(random));
    }

    return new ScalarRegister
    {
      AnsweredByItem = ByItem(nameof(ScalarRegister.AnsweredByItem), r => r.NextBool(0.75d)),
      CurveByItem = ByItem(nameof(ScalarRegister.CurveByItem), r => r.NextSByte(-8, 8)),
      RawScoreByItem = ByItem(nameof(ScalarRegister.RawScoreByItem), r => r.NextByte(0, 100)),
      SeatByItem = ByItem(nameof(ScalarRegister.SeatByItem), r => r.NextInt16(-1, 400)),
      AttemptsByItem = ByItem(nameof(ScalarRegister.AttemptsByItem), r => r.NextUInt16(0, 6)),
      PointsByItem = ByItem(nameof(ScalarRegister.PointsByItem), r => r.NextInt32(1, 40)),
      SequenceByItem = ByItem(nameof(ScalarRegister.SequenceByItem), r => r.NextUInt32()),
      ElapsedTicksByItem = ByItem(
            nameof(ScalarRegister.ElapsedTicksByItem),
            r => r.NextInt64(TimeSpan.TicksPerSecond * 10L, TimeSpan.TicksPerMinute * 25L)),
      ChecksumByItem = ByItem(nameof(ScalarRegister.ChecksumByItem), r => r.NextUInt64()),
      PercentileByItem = ByItem(nameof(ScalarRegister.PercentileByItem), r => r.NextSingle(0f, 100f, 2)),
      ScaledScoreByItem = ByItem(nameof(ScalarRegister.ScaledScoreByItem), r => r.NextDouble(0d, 100d, 3)),
      WeightByItem = ByItem(nameof(ScalarRegister.WeightByItem), r => r.NextDecimal(0m, 1m, 4)),
      LetterGradeByItem = ByItem(
            nameof(ScalarRegister.LetterGradeByItem),
            r => r.NextChar(FixtureVocabulary.GradeLetters)),
      RemarkByItem = ByItem(nameof(ScalarRegister.RemarkByItem), r => r.Pick(FixtureVocabulary.MarkerRemarks)),
      ResponseIdByItem = ByItem(nameof(ScalarRegister.ResponseIdByItem), r => r.NextGuid()),
      MarkedOnByItem = ByItem(
            nameof(ScalarRegister.MarkedOnByItem),
            r => r.NextDateOnly(FixtureVocabulary.Epoch, 2_200)),
      OpenedAtByItem = ByItem(nameof(ScalarRegister.OpenedAtByItem), r => r.NextTimeOnly(8, 18, 5)),
      DurationByItem = ByItem(nameof(ScalarRegister.DurationByItem), r => r.NextTimeSpan(1, 45)),
      RecordedAtUtcByItem = ByItem(
            nameof(ScalarRegister.RecordedAtUtcByItem),
            r => r.NextDateTimeUtc(FixtureVocabulary.EpochUtc, 2_200)),
      SavedAtByItem = ByItem(
            nameof(ScalarRegister.SavedAtByItem),
            r => r.NextDateTimeOffset(FixtureVocabulary.EpochUtc, 2_200)),

      // Maps whose keys are not strings, so that key conversion is exercised too.
      RemarkByItemNumber = GenerateNumberedMap(
            seed,
            nameof(ScalarRegister.RemarkByItemNumber),
            count,
            (random, _) => random.Pick(FixtureVocabulary.MarkerRemarks)),
      ScoreByResponseId = GenerateGuidKeyedMap(
            seed,
            nameof(ScalarRegister.ScoreByResponseId),
            count,
            (random, _) => random.NextDouble(0d, 100d, 3)),
      WeightByAssessmentKind = GenerateEnumKeyedMap<AssessmentKind, decimal>(
            seed,
            nameof(ScalarRegister.WeightByAssessmentKind),
            count,
            (random, _) => random.NextDecimal(0.05m, 0.5m, 4)),
      AttendanceByDate = GenerateDateKeyedMap(
            seed,
            nameof(ScalarRegister.AttendanceByDate),
            count,
            (random, _) => random.NextUInt16(0, 400))
    };
  }

  private static IReadOnlyDictionary<int, TValue> GenerateNumberedMap<TValue>(
      FixtureSeed parent,
      string edge,
      int count,
      Func<StableRandom, int, TValue> factory)
  {
    var random = new StableRandom(parent.Derive(edge).Value);
    var map = new Dictionary<int, TValue>(count);

    for (var i = 0; i < count; i++)
    {
      map[i + 1] = factory(random, i);
    }

    return map;
  }

  private static IReadOnlyDictionary<Guid, TValue> GenerateGuidKeyedMap<TValue>(
      FixtureSeed parent,
      string edge,
      int count,
      Func<StableRandom, int, TValue> factory)
  {
    var seed = parent.Derive(edge);
    var map = new Dictionary<Guid, TValue>(count);

    for (var i = 0; i < count; i++)
    {
      // A stream per ordinal rather than one shared stream, so the keys stay distinct however the
      // values move.
      var entry = new StableRandom(seed.Derive("$entry", i).Value);
      map[entry.NextGuid()] = factory(entry, i);
    }

    return map;
  }

  private static IReadOnlyDictionary<TKey, TValue> GenerateEnumKeyedMap<TKey, TValue>(
      FixtureSeed parent,
      string edge,
      int count,
      Func<StableRandom, int, TValue> factory)
      where TKey : struct, Enum
  {
    var random = new StableRandom(parent.Derive(edge).Value);
    var keys = random.PickDistinctEnums<TKey>(count);

    var map = new Dictionary<TKey, TValue>(keys.Count);
    for (var i = 0; i < keys.Count; i++)
    {
      map[keys[i]] = factory(random, i);
    }

    return map;
  }

  private static IReadOnlyDictionary<DateOnly, TValue> GenerateDateKeyedMap<TValue>(
      FixtureSeed parent,
      string edge,
      int count,
      Func<StableRandom, int, TValue> factory)
  {
    var random = new StableRandom(parent.Derive(edge).Value);
    var first = random.NextDateOnly(FixtureVocabulary.Epoch, 2_000);

    var map = new Dictionary<DateOnly, TValue>(count);
    for (var i = 0; i < count; i++)
    {
      map[first.AddDays(i * 7)] = factory(random, i);
    }

    return map;
  }
}
