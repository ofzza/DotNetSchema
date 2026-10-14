namespace DotNetSchema.Fixtures.Schema.Decorators;

/// <summary>
/// A <c>[Flags]</c> enum used only as an attribute argument, so the decorator rendering meets a single
/// member (written by name), a combination of members and an undeclared value (both written as numbers).
/// </summary>
[Flags]
public enum SampleShade
{
  None = 0,
  Red = 1,
  Green = 2,
  Blue = 4,
}
