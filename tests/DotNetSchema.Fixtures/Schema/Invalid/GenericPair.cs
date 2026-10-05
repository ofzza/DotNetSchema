namespace DotNetSchema.Fixtures.Schema.Invalid;

/// <summary>
/// A generic model, which has no stable definition name. Reached from <see cref="GenericProperty" />.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public sealed record GenericPair<T>
{
  /// <summary>The first element.</summary>
  public required T First { get; init; }

  /// <summary>The second element.</summary>
  public required T Second { get; init; }
}
