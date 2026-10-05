using System.Reflection;

namespace DotNetSchema.Generator.Reflection;

/// <summary>
/// Decodes the nullable-reference annotations the C# compiler writes into metadata, so that
/// <c>Person? Mentor</c> can be told from <c>Person Mentor</c> without loading either for execution.
/// </summary>
/// <remarks>
/// <see cref="NullabilityInfoContext" /> cannot be used here, and the reason is worth spelling out
/// because it fails quietly rather than loudly: it routes through <c>Nullable.GetUnderlyingType</c>,
/// which is a bare <c>ReferenceEquals(genericType, typeof(Nullable&lt;&gt;))</c> against a runtime type.
/// A type read for metadata only can never satisfy that, so every <c>T?</c> value type is reported as
/// non-nullable — and, worse, the byte-array index walk desynchronises for any generic containing one,
/// which corrupts the answer for unrelated members.
/// <para>
/// So the encoding is decoded by hand. The compiler writes <c>[Nullable]</c> on a member as either a
/// single byte (every component shares that state) or one byte per annotatable component in pre-order,
/// and <c>[NullableContext]</c> on an enclosing scope as the default when <c>[Nullable]</c> is absent.
/// A component is annotatable if it is a reference type, an array, or a generic type; a plain value type
/// contributes no byte, and <c>Nullable&lt;T&gt;</c> is unwrapped before the question is asked. Both
/// attributes are compiler-minted internal types, one per assembly, so they have no shared identity even
/// at runtime and must be matched by name.
/// </para>
/// </remarks>
public static class NullableFlagsReader
{
  private const string NullableAttributeName = "System.Runtime.CompilerServices.NullableAttribute";
  private const string NullableContextAttributeName = "System.Runtime.CompilerServices.NullableContextAttribute";

  private const byte Oblivious = 0;
  private const byte NotAnnotated = 1;
  private const byte Annotated = 2;

  /// <summary>Reads the nullability of a property's type, and of everything nested inside it.</summary>
  public static NullabilityNode Read(PropertyInfo property)
  {
    var flags = ReadFlags(property);
    var cursor = 0;
    return Build(property.PropertyType, flags, ref cursor);
  }

  private static IReadOnlyList<byte> ReadFlags(PropertyInfo property)
  {
    if (MetadataFacts.FindAttribute(property, NullableAttributeName) is { } nullable)
    {
      return ReadArgument(nullable);
    }

    // No [Nullable] on the member: the enclosing scope's [NullableContext] supplies the state for every
    // component. Nested types inherit from the type they are declared in, so the chain is walked outward.
    for (var scope = property.DeclaringType; scope is not null; scope = scope.DeclaringType)
    {
      if (MetadataFacts.FindAttribute(scope, NullableContextAttributeName) is { } context)
      {
        return ReadArgument(context);
      }
    }

    return [Oblivious];
  }

  private static IReadOnlyList<byte> ReadArgument(CustomAttributeData attribute)
  {
    if (attribute.ConstructorArguments is not [var argument])
    {
      return [Oblivious];
    }

    if (argument.Value is byte single)
    {
      return [single];
    }

    if (argument.Value is IReadOnlyList<CustomAttributeTypedArgument> many)
    {
      return [.. many.Select(a => a.Value is byte b ? b : Oblivious)];
    }

    return [Oblivious];
  }

  private static NullabilityNode Build(Type type, IReadOnlyList<byte> flags, ref int cursor)
  {
    // Nullable<T> is nullable by construction and contributes no flag of its own; the walk continues into
    // T so that, say, the value side of IReadOnlyDictionary<string, int?> still lines up.
    var isValueNullable = MetadataFacts.IsNullableOfT(type, out var unwrapped);

    byte state;
    List<NullabilityNode> arguments = [];

    if (unwrapped.IsGenericType)
    {
      state = Take(flags, ref cursor);
      foreach (var argument in unwrapped.GetGenericArguments())
      {
        arguments.Add(Build(argument, flags, ref cursor));
      }
    }
    else if (unwrapped.HasElementType)
    {
      state = unwrapped.IsArray ? Take(flags, ref cursor) : Oblivious;
      arguments.Add(Build(unwrapped.GetElementType()!, flags, ref cursor));
    }
    else
    {
      state = unwrapped.IsValueType ? NotAnnotated : Take(flags, ref cursor);
    }

    return new NullabilityNode(isValueNullable || state == Annotated, state == Oblivious, arguments);
  }

  private static byte Take(IReadOnlyList<byte> flags, ref int cursor)
  {
    // A single-byte [Nullable] or an inherited [NullableContext] states one value for every component, so
    // it is never consumed; only a per-component array advances.
    if (flags.Count == 1)
    {
      return flags[0];
    }

    return cursor < flags.Count ? flags[cursor++] : Oblivious;
  }
}

/// <summary>
/// The nullability of one position in a type, and of the positions nested inside it. Mirrors the shape of
/// the type: <see cref="Arguments" /> holds the generic arguments, or the element type of an array.
/// </summary>
/// <param name="IsNullable">Whether a null is permitted here.</param>
/// <param name="IsUnknown">Whether the compiler stated nothing, so <see cref="IsNullable" /> is a guess.</param>
/// <param name="Arguments">Nullability of the nested positions, in declaration order.</param>
public sealed record NullabilityNode(
  bool IsNullable,
  bool IsUnknown,
  IReadOnlyList<NullabilityNode> Arguments)
{
  /// <summary>The nested position at <paramref name="index" />, or an unknown node when there is none.</summary>
  public NullabilityNode Argument(int index) =>
    index < Arguments.Count ? Arguments[index] : new NullabilityNode(false, true, []);
}
