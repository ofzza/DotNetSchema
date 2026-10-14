namespace DotNetSchema.Sample.Models;

/// <summary>
/// An attribute of the application's own, marking personal data. Not in the default decorator list; the
/// host allows this namespace, so it appears under <c>x-decorators</c> for a schema consumer to act on.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class PersonalDataAttribute : Attribute;
