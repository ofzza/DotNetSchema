using System.Globalization;

namespace DotNetSchema.Fixtures.Generation;

/// <summary>
/// A stable value source: a SplitMix64 stream opened on a 64-bit seed, with typed draws for every
/// portable primitive. Stable means the sequence is fixed by the seed alone — the same seed yields the
/// same draws on any machine, any platform and any runtime version.
/// </summary>
/// <remarks>
/// That guarantee is why <see cref="Random" /> is deliberately not used: its seeded algorithm is an
/// implementation detail the BCL reserves the right to change, and a seeded stream that shifts under a
/// framework upgrade is worse than none at all. Everything here is integer arithmetic with an explicit
/// contract, and the floating-point draws are built from exactly representable integer ratios, so they
/// carry no platform-dependent rounding.
/// </remarks>
public sealed class StableRandom
{
  private const ulong GoldenGamma = 0x9E3779B97F4A7C15UL;

  private ulong state;

  /// <summary>Opens a stream on the given seed.</summary>
  /// <param name="seed">The 64-bit state the stream starts from.</param>
  public StableRandom(ulong seed)
  {
    state = seed;
  }

  /// <summary>Draws the next 64 raw bits and advances the stream.</summary>
  public ulong NextUInt64()
  {
    unchecked
    {
      state += GoldenGamma;
      var value = state;
      value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
      value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
      return value ^ (value >> 31);
    }
  }

  /// <summary>Draws the next 32 raw bits, taken from the high half of the stream.</summary>
  public uint NextUInt32()
  {
    return (uint)(NextUInt64() >> 32);
  }

  /// <summary>Draws a <c>long</c> in <c>[minInclusive, maxExclusive)</c>.</summary>
  public long NextInt64(long minInclusive, long maxExclusive)
  {
    if (maxExclusive <= minInclusive)
    {
      return minInclusive;
    }

    var span = unchecked((ulong)(maxExclusive - minInclusive));
    return unchecked(minInclusive + (long)(NextUInt64() % span));
  }

  /// <summary>Draws an <c>int</c> in <c>[minInclusive, maxExclusive)</c>.</summary>
  public int NextInt32(int minInclusive, int maxExclusive)
  {
    return (int)NextInt64(minInclusive, maxExclusive);
  }

  /// <summary>Draws a <c>bool</c> that is true with the given probability.</summary>
  /// <param name="probability">Chance of drawing true, clamped to <c>[0, 1]</c>.</param>
  public bool NextBool(double probability = 0.5d)
  {
    return NextDouble() < Math.Clamp(probability, 0d, 1d);
  }

  /// <summary>Draws a <c>double</c> in <c>[0, 1)</c> with full 53-bit resolution.</summary>
  public double NextDouble()
  {
    return (NextUInt64() >> 11) * (1.0d / 9007199254740992.0d);
  }

  /// <summary>Draws a <c>double</c> in <c>[min, max)</c>, rounded to <paramref name="decimals" /> places.</summary>
  public double NextDouble(double min, double max, int decimals = 2)
  {
    return Math.Round(min + (NextDouble() * (max - min)), decimals, MidpointRounding.AwayFromZero);
  }

  /// <summary>Draws a <c>float</c> in <c>[min, max)</c>, rounded to <paramref name="decimals" /> places.</summary>
  public float NextSingle(float min, float max, int decimals = 2)
  {
    var unit = (NextUInt32() >> 8) * (1.0f / 16777216.0f);
    return MathF.Round(min + (unit * (max - min)), decimals, MidpointRounding.AwayFromZero);
  }

  /// <summary>
  /// Draws a <c>decimal</c> in <c>[min, max)</c> with exactly <paramref name="decimals" /> places, built
  /// from an integer numerator so the result is exact rather than a rounded binary approximation.
  /// </summary>
  public decimal NextDecimal(decimal min, decimal max, int decimals = 2)
  {
    var scale = (decimal)Math.Pow(10d, decimals);
    var steps = (long)((max - min) * scale);
    return min + (NextInt64(0L, Math.Max(1L, steps)) / scale);
  }

  /// <summary>Draws a <c>sbyte</c> in <c>[minInclusive, maxInclusive]</c>.</summary>
  public sbyte NextSByte(sbyte minInclusive = sbyte.MinValue, sbyte maxInclusive = sbyte.MaxValue)
  {
    return (sbyte)NextInt64(minInclusive, maxInclusive + 1L);
  }

  /// <summary>Draws a <c>byte</c> in <c>[minInclusive, maxInclusive]</c>.</summary>
  public byte NextByte(byte minInclusive = byte.MinValue, byte maxInclusive = byte.MaxValue)
  {
    return (byte)NextInt64(minInclusive, maxInclusive + 1L);
  }

  /// <summary>Draws a <c>short</c> in <c>[minInclusive, maxInclusive]</c>.</summary>
  public short NextInt16(short minInclusive = short.MinValue, short maxInclusive = short.MaxValue)
  {
    return (short)NextInt64(minInclusive, maxInclusive + 1L);
  }

  /// <summary>Draws a <c>ushort</c> in <c>[minInclusive, maxInclusive]</c>.</summary>
  public ushort NextUInt16(ushort minInclusive = ushort.MinValue, ushort maxInclusive = ushort.MaxValue)
  {
    return (ushort)NextInt64(minInclusive, maxInclusive + 1L);
  }

  /// <summary>Draws a <c>char</c> from the supplied alphabet.</summary>
  public char NextChar(string alphabet)
  {
    return alphabet[NextInt32(0, alphabet.Length)];
  }

  /// <summary>Picks one element of <paramref name="candidates" />.</summary>
  public T Pick<T>(IReadOnlyList<T> candidates)
  {
    return candidates[NextInt32(0, candidates.Count)];
  }

  /// <summary>Picks one member of an enum.</summary>
  public TEnum PickEnum<TEnum>() where TEnum : struct, Enum
  {
    return Pick(Enum.GetValues<TEnum>());
  }

  /// <summary>
  /// Picks <paramref name="count" /> distinct members of an enum, in declaration order. Used wherever an
  /// enum is a map key and duplicates would collide.
  /// </summary>
  public IReadOnlyList<TEnum> PickDistinctEnums<TEnum>(int count) where TEnum : struct, Enum
  {
    var all = Enum.GetValues<TEnum>();
    count = Math.Clamp(count, 0, all.Length);

    // Partial Fisher-Yates, then re-sort so the result reads naturally.
    var pool = (TEnum[])all.Clone();
    for (var i = 0; i < count; i++)
    {
      var j = NextInt32(i, pool.Length);
      (pool[i], pool[j]) = (pool[j], pool[i]);
    }

    var picked = pool[..count];
    Array.Sort(picked);
    return picked;
  }

  /// <summary>
  /// Draws a <c>Guid</c> shaped as an RFC 4122 version 4 value. The bits come from the stream rather than
  /// from the platform's entropy source, so the identity it carries is reproducible.
  /// </summary>
  public Guid NextGuid()
  {
    Span<byte> bytes = stackalloc byte[16];
    BitConverter.TryWriteBytes(bytes[..8], NextUInt64());
    BitConverter.TryWriteBytes(bytes[8..], NextUInt64());

    bytes[6] = (byte)((bytes[6] & 0x0F) | 0x40);
    bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);

    return new Guid(bytes, bigEndian: true);
  }

  /// <summary>
  /// Draws a <c>DateOnly</c> within <paramref name="dayRange" /> days after <paramref name="origin" />.
  /// </summary>
  public DateOnly NextDateOnly(DateOnly origin, int dayRange)
  {
    return origin.AddDays(NextInt32(0, Math.Max(1, dayRange)));
  }

  /// <summary>Draws a <c>TimeOnly</c> on the given minute grid, between two whole hours.</summary>
  public TimeOnly NextTimeOnly(int fromHour = 0, int toHour = 24, int minuteStep = 5)
  {
    var minutes = NextInt32(fromHour * 60, toHour * 60) / minuteStep * minuteStep;
    return new TimeOnly(minutes / 60 % 24, minutes % 60);
  }

  /// <summary>Draws a <c>TimeSpan</c> in whole minutes.</summary>
  public TimeSpan NextTimeSpan(int fromMinutes, int toMinutes)
  {
    return TimeSpan.FromMinutes(NextInt32(fromMinutes, toMinutes));
  }

  /// <summary>
  /// Draws a <c>DateTime</c> whose <see cref="DateTime.Kind" /> is always <see cref="DateTimeKind.Utc" />.
  /// </summary>
  public DateTime NextDateTimeUtc(DateTime origin, int dayRange)
  {
    var offset = TimeSpan.FromSeconds(NextInt64(0L, Math.Max(1, dayRange) * 86400L));
    return DateTime.SpecifyKind(origin.Date + offset, DateTimeKind.Utc);
  }

  /// <summary>
  /// Draws a <c>DateTimeOffset</c> with a deliberately non-zero UTC offset, so that anything round-tripping
  /// the value has to preserve the offset rather than quietly normalise it.
  /// </summary>
  public DateTimeOffset NextDateTimeOffset(DateTime origin, int dayRange)
  {
    var instant = NextDateTimeUtc(origin, dayRange);
    var offsetHours = Pick<int>([-8, -5, 0, 1, 2, 5, 9]);
    return new DateTimeOffset(
        DateTime.SpecifyKind(instant, DateTimeKind.Unspecified),
        TimeSpan.FromHours(offsetHours));
  }

  /// <summary>Formats a zero-padded integer, e.g. <c>"Q07"</c>, without touching the current culture.</summary>
  public static string Code(string prefix, int value, int width = 2)
  {
    return prefix + value.ToString(CultureInfo.InvariantCulture).PadLeft(width, '0');
  }
}
