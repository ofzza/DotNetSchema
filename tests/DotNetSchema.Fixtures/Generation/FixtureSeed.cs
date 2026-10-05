namespace DotNetSchema.Fixtures.Generation;

/// <summary>
/// A position in the fixture graph, expressed as a 64-bit seed.
/// </summary>
/// <remarks>
/// Seeds are <em>derived by path</em> rather than drawn from a running stream: the seed of a node is a
/// pure function of its parent's seed and the name (and ordinal) of the edge that reaches it. Two useful
/// properties follow.
/// <list type="bullet">
///     <item>A subtree depends only on its own path, so adding, removing or reordering a sibling never
///     disturbs it — <c>Generate(1).Campuses[2]</c> is the same object no matter how many departments
///     the school gained.</item>
///     <item>A node can be regenerated in isolation, which is what lets a back-reference such as
///     <c>Book.Class</c> reproduce the identity of the very class that prescribes it without the
///     generator having to hold a real object cycle.</item>
/// </list>
/// The mixing function is SplitMix64 over an FNV-1a hash of the edge name, spelled out here rather than
/// taken from the BCL so that fixtures stay byte-for-byte stable across runtimes, platforms and
/// framework upgrades.
/// </remarks>
public readonly record struct FixtureSeed
{
  private const ulong FnvOffsetBasis = 14695981039346656037UL;
  private const ulong FnvPrime = 1099511628211UL;
  private const ulong GoldenGamma = 0x9E3779B97F4A7C15UL;

  /// <summary>Creates a seed over a raw 64-bit state.</summary>
  /// <param name="value">The raw state.</param>
  public FixtureSeed(ulong value)
  {
    Value = value;
  }

  /// <summary>The raw 64-bit state of the seed.</summary>
  public ulong Value { get; }

  /// <summary>Turns the caller-facing seed number into the root seed of a fixture graph.</summary>
  /// <param name="seed">Any integer; every distinct value yields an unrelated graph.</param>
  /// <returns>The root seed.</returns>
  public static FixtureSeed FromSeedNumber(int seed)
  {
    return new FixtureSeed(Mix(unchecked((ulong)(uint)seed * FnvPrime) ^ GoldenGamma));
  }

  /// <summary>Derives the seed of a child node reached over a named edge.</summary>
  /// <param name="edge">Name of the edge, conventionally the property name.</param>
  /// <returns>The child seed.</returns>
  public FixtureSeed Derive(string edge)
  {
    return new FixtureSeed(Mix(Value ^ Hash(edge)));
  }

  /// <summary>Derives the seed of the <paramref name="ordinal" />-th child reached over a named edge.</summary>
  /// <param name="edge">Name of the edge, conventionally the property name.</param>
  /// <param name="ordinal">Zero-based position within the collection.</param>
  /// <returns>The child seed.</returns>
  public FixtureSeed Derive(string edge, int ordinal)
  {
    return new FixtureSeed(Mix(Mix(Value ^ Hash(edge)) ^ unchecked((ulong)(long)ordinal * GoldenGamma)));
  }

  /// <summary>
  /// FNV-1a over the UTF-16 code units of <paramref name="text" />; culture-independent by construction.
  /// </summary>
  private static ulong Hash(string text)
  {
    var hash = FnvOffsetBasis;
    foreach (var character in text)
    {
      unchecked
      {
        hash ^= character;
        hash *= FnvPrime;
      }
    }

    return hash;
  }

  /// <summary>The SplitMix64 finaliser, which spreads a small change in the input across all 64 bits.</summary>
  private static ulong Mix(ulong value)
  {
    unchecked
    {
      value += GoldenGamma;
      value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
      value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
      return value ^ (value >> 31);
    }
  }
}
