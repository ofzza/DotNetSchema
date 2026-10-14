namespace DotNetSchema.Fixtures.Schema.Decorators;

/// <summary>
/// A repeatable attribute with a <c>params</c> array, so the sort order of several applications of one
/// attribute is pinned by a test, and so is the rendering of a <c>params</c> argument.
/// </summary>
[AttributeUsage(
  AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property |
  AttributeTargets.Parameter,
  AllowMultiple = true)]
public sealed class SampleTagAttribute : Attribute
{
  /// <summary>A tag, with any number of weights.</summary>
  public SampleTagAttribute(string tag, params int[] weights)
  {
  }
}
