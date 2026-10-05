using DotNetSchema.Fixtures.Generation;

namespace DotNetSchema.Fixtures.Generation;

/// <summary>
/// The traversal primitives shared by every generator: how a collection, an optional reference and a
/// keyed map spend the depth budget and derive their children's seeds.
/// </summary>
public sealed partial class FixturesService
{
  /// <summary>
  /// Generates a collection of child entities. The element count is drawn from a seed of its own rather
  /// than from the parent's stream, so adding a property to the parent record cannot shift it.
  /// </summary>
  private static IReadOnlyList<T> GenerateList<T>(
      FixtureSeed parent,
      string edge,
      int budget,
      FixtureOptions options,
      Func<FixtureSeed, int, T> factory)
  {
    return GenerateList(parent, edge, budget, options, (seed, remaining, _) => factory(seed, remaining));
  }

  /// <summary>
  /// Generates a collection of child entities, passing each factory call the element's ordinal — used
  /// wherever a child needs to know its position, such as a chapter or section number.
  /// </summary>
  private static IReadOnlyList<T> GenerateList<T>(
      FixtureSeed parent,
      string edge,
      int budget,
      FixtureOptions options,
      Func<FixtureSeed, int, int, T> factory)
  {
    if (budget <= 0)
    {
      return [];
    }

    var collection = parent.Derive(edge);
    var count = new StableRandom(collection.Derive("$count").Value)
        .NextInt32(options.MinCollectionSize, options.MaxCollectionSize + 1);

    var items = new T[count];
    for (var i = 0; i < count; i++)
    {
      items[i] = factory(collection.Derive("$item", i), budget - 1, i);
    }

    return items;
  }

  /// <summary>
  /// Generates an optional reference. Beyond the depth budget the answer is always <c>null</c>; within
  /// it, <see cref="FixtureOptions.OptionalFillRate" /> decides.
  /// </summary>
  private static T? GenerateOptional<T>(
      FixtureSeed parent,
      string edge,
      int budget,
      FixtureOptions options,
      Func<FixtureSeed, int, T> factory)
      where T : class
  {
    if (budget <= 0)
    {
      return null;
    }

    var child = parent.Derive(edge);
    if (!new StableRandom(child.Derive("$present").Value).NextBool(options.OptionalFillRate))
    {
      return null;
    }

    return factory(child, budget - 1);
  }

  /// <summary>
  /// Generates a map of child entities keyed by an enum, where the key is also the child's own name —
  /// the school's departments and the library's catalogue. Keys are drawn without replacement, so the
  /// map is never quietly short of entries because two children happened to pick the same name.
  /// </summary>
  private static IReadOnlyDictionary<TKey, TValue> GenerateKeyedMap<TKey, TValue>(
      FixtureSeed parent,
      string edge,
      int budget,
      FixtureOptions options,
      Func<FixtureSeed, int, TKey, TValue> factory)
      where TKey : struct, Enum
  {
    if (budget <= 0)
    {
      return new Dictionary<TKey, TValue>();
    }

    var collection = parent.Derive(edge);
    var keys = new StableRandom(collection.Derive("$keys").Value).PickDistinctEnums<TKey>(options.MapSize);

    var map = new Dictionary<TKey, TValue>(keys.Count);
    for (var i = 0; i < keys.Count; i++)
    {
      map[keys[i]] = factory(collection.Derive("$item", i), budget - 1, keys[i]);
    }

    return map;
  }

  /// <summary>
  /// Builds a map keyed by item code — <c>"Q01"</c>, <c>"Q02"</c>, … — which is how every scalar map in
  /// a marking register is laid out.
  /// </summary>
  private static IReadOnlyDictionary<string, TValue> GenerateItemMap<TValue>(
      FixtureSeed parent,
      string edge,
      int count,
      Func<StableRandom, int, TValue> factory)
  {
    var random = new StableRandom(parent.Derive(edge).Value);
    var map = new Dictionary<string, TValue>(count, StringComparer.Ordinal);

    for (var i = 0; i < count; i++)
    {
      map[StableRandom.Code("Q", i + 1)] = factory(random, i);
    }

    return map;
  }

  /// <summary>Builds a fixed-length series, drawing every element from one stream.</summary>
  private static IReadOnlyList<TValue> GenerateSeries<TValue>(
      FixtureSeed parent,
      string edge,
      int count,
      Func<StableRandom, int, TValue> factory)
  {
    var random = new StableRandom(parent.Derive(edge).Value);

    var items = new TValue[count];
    for (var i = 0; i < count; i++)
    {
      items[i] = factory(random, i);
    }

    return items;
  }
}
