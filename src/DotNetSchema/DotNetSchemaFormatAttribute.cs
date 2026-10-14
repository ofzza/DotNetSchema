namespace DotNetSchema;

/// <summary>
/// Declares the JSON Schema <c>format</c> and/or <c>pattern</c> of one property's value, without changing its
/// <c>type</c> — for a value whose type says too little on its own, such as a <c>string</c> holding an ISBN.
/// </summary>
/// <remarks>
/// <para>
/// <b>This changes the schema only, never serialisation or validation.</b> The attribute is a promise about
/// the values the property holds, and nothing checks it. It replaces whatever format or pattern the
/// property's type would otherwise have — a <c>Guid</c>'s <c>uuid</c>, a mapped type's declared one — for
/// this property alone, so a wrong declaration makes the schema wrong, silently.
/// </para>
/// <para>
/// On a collection it applies to the elements (of an array) or the values (of a dictionary), at whatever
/// depth they are; it never constrains dictionary keys. It has no effect on a model or an enum. On a
/// positional record it may be written on the parameter, without a <c>property:</c> target. To describe a
/// type the same way wherever it appears, use <see cref="DotNetSchemaTypeAttribute" /> instead. See the
/// package README, "Property formats".
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class DotNetSchemaFormatAttribute : Attribute
{
  /// <summary>Declares a pattern alone, given with <see cref="Pattern" />.</summary>
  public DotNetSchemaFormatAttribute()
  {
  }

  /// <summary>Declares the JSON Schema format the property's values are written in.</summary>
  /// <param name="format">The format, for example <c>isbn</c> or <c>email</c>.</param>
  public DotNetSchemaFormatAttribute(string format)
  {
    Format = format;
  }

  /// <summary>
  /// The JSON Schema <c>format</c>: a registered one (<c>uuid</c>, <c>date-time</c>, <c>email</c>, …) or your
  /// own, which a reader that does not know it treats as an annotation — or, in a strict validator, rejects
  /// until it is registered there.
  /// </summary>
  public string? Format { get; }

  /// <summary>
  /// A regular expression every value matches, emitted as the JSON Schema <c>pattern</c>; only valid where
  /// the value is a <c>string</c>. Unanchored, as JSON Schema patterns are — add <c>^</c> and <c>$</c> to
  /// match the whole value.
  /// </summary>
  public string? Pattern { get; init; }
}
