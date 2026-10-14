namespace DotNetSchema;

/// <summary>
/// Declares the JSON Schema a type is described by wherever it appears: inline, as the given
/// <c>type</c> with an optional <c>format</c> and <c>pattern</c>, instead of as a <c>$ref</c> to an object
/// definition.
/// </summary>
/// <remarks>
/// <para>
/// <b>This changes the schema only, never serialisation.</b> The attribute is a promise that the type is
/// written as described — typically by a <c>[JsonConverter]</c> registered alongside it — and nothing checks
/// that promise. If the converter and the declaration disagree, the schema is simply wrong. The promise also
/// travels with the type: every project that exports a model using it gets this declaration.
/// </para>
/// <para>
/// A project can replace the declaration with a <c>DotNetSchemaType</c> item in its own build configuration,
/// which always takes precedence. On a generic type the declaration covers every construction of it. See the
/// package README, "Custom type mappings".
/// </para>
/// <para>
/// To give a single property a format or pattern instead — say, an ISBN held in a <c>string</c> — use
/// <see cref="DotNetSchemaFormatAttribute" /> on the property.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, AllowMultiple = false, Inherited = false)]
public sealed class DotNetSchemaTypeAttribute : Attribute
{
  /// <summary>Declares the JSON Schema type the attributed type is written as.</summary>
  /// <param name="type">One of <c>string</c>, <c>number</c>, <c>integer</c> or <c>boolean</c>.</param>
  public DotNetSchemaTypeAttribute(string type)
  {
    Type = type;
  }

  /// <summary>The JSON Schema <c>type</c>: <c>string</c>, <c>number</c>, <c>integer</c> or <c>boolean</c>.</summary>
  public string Type { get; }

  /// <summary>
  /// The JSON Schema <c>format</c>, for example <c>uuid</c> or <c>date-time</c>, or <see langword="null" /> for
  /// none. A format the reader does not know is an annotation rather than a constraint.
  /// </summary>
  public string? Format { get; init; }

  /// <summary>
  /// A regular expression every written value matches, emitted as the JSON Schema <c>pattern</c>; only valid
  /// with a <c>string</c> type. Unanchored, as JSON Schema patterns are — add <c>^</c> and <c>$</c> to match the
  /// whole value.
  /// </summary>
  public string? Pattern { get; init; }
}
