namespace DotNetSchema.Fixtures.Schema.Decorators;

/// <summary>
/// An attribute with no meaning, whose constructors between them take every kind of argument an attribute
/// can carry, so the <c>x-decorators</c> rendering of each kind is pinned by a test.
/// </summary>
/// <remarks>
/// Deliberately outside the default decorator list, so the school graph can carry it without moving the
/// golden digests; a test that wants to see it allows <c>DotNetSchema.Fixtures.Schema.Decorators.*</c>.
/// </remarks>
[AttributeUsage(
  AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property |
  AttributeTargets.Parameter)]
public sealed class SampleDecoratorAttribute : Attribute
{
  /// <summary>An application with no arguments at all.</summary>
  public SampleDecoratorAttribute()
  {
  }

  /// <summary>Every primitive an attribute argument can be, once each.</summary>
  public SampleDecoratorAttribute(
    bool flag,
    sbyte tiny,
    byte octet,
    short small,
    ushort smallUnsigned,
    int whole,
    uint wholeUnsigned,
    long wide,
    ulong wideUnsigned,
    float single,
    double precise,
    char letter,
    string? text)
  {
  }

  /// <summary>A type, an enum, and an <see cref="object" /> parameter boxing anything else.</summary>
  public SampleDecoratorAttribute(Type type, SampleShade shade, object? boxed)
  {
  }

  /// <summary>One-dimensional arrays, of each element kind.</summary>
  public SampleDecoratorAttribute(int[] numbers, string?[] words, Type[] types, object?[] mixed)
  {
  }

  /// <summary>A named property argument of string type.</summary>
  public string? Note { get; set; }

  /// <summary>A named property argument of enum type.</summary>
  public SampleShade Shade { get; set; }

  /// <summary>A named property argument of <see cref="System.Type" /> type.</summary>
  public Type? Kind { get; set; }

  /// <summary>A named property argument of array type.</summary>
  public int[]? Numbers { get; set; }

  /// <summary>A named <em>field</em> argument, which metadata records differently from a property.</summary>
  public double Weight;

  /// <summary>A nested type, so a <see cref="System.Type" /> argument can name one.</summary>
  public sealed class Nested;
}
