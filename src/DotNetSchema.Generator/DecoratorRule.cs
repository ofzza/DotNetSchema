namespace DotNetSchema.Generator;

/// <summary>
/// One step of the decorator selection: include or exclude the attributes whose full names match a pattern.
/// Rules are applied in order and the last one matching an attribute decides; see
/// <see cref="SchemaGenerationOptions.DecoratorRules" />.
/// </summary>
/// <param name="Pattern">An attribute full name, in which <c>*</c> matches any run of characters, dots included.</param>
/// <param name="Include">Whether a matching attribute is exposed.</param>
public sealed record DecoratorRule(string Pattern, bool Include);
