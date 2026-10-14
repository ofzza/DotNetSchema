using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DotNetSchema.Generator.Reflection;

/// <summary>
/// Reads the attributes on a model, an enum or a property that the options select, in the
/// language-neutral form they are exposed under as <c>x-decorators</c>.
/// </summary>
/// <remarks>
/// Everything goes through <see cref="CustomAttributeData" />, so an attribute is never instantiated and
/// none of its code runs. The price is that only what the source actually wrote is visible: a constructor
/// argument by its parameter name, a named argument by its member name, and nothing the attribute would
/// have computed or defaulted for itself.
/// </remarks>
/// <param name="options">The generation options in force, which carry the selection.</param>
public sealed class DecoratorReader(SchemaGenerationOptions options)
{
  /// <summary>
  /// Never exposed, whatever the rules say: the compiler's own bookkeeping, the System.Text.Json attributes
  /// the schema already reflects, and this package's attributes. Matched by exact namespace, not by the
  /// rules' wildcard: <c>DotNetSchema</c> as a prefix would also swallow every consumer namespace that
  /// happens to start with it.
  /// </summary>
  private static readonly string[] HardExcluded =
  [
    "System.Runtime.CompilerServices",
    "System.Text.Json.Serialization",
    "DotNetSchema",
  ];

  private static readonly JsonSerializerOptions Compact = new() { WriteIndented = false };

  /// <summary>The selected attributes applied to a type.</summary>
  /// <exception cref="Exception">Whatever the load context throws when an attribute cannot be decoded.</exception>
  public IReadOnlyList<Decorator> Read(Type type) => Sorted(Selected(type.GetCustomAttributesData()));

  /// <summary>
  /// The selected attributes applied to a property — including, for a positional record, those C# put on
  /// the primary-constructor parameter the property was generated from, which is where an attribute written
  /// without a <c>property:</c> target lands.
  /// </summary>
  /// <exception cref="Exception">Whatever the load context throws when an attribute cannot be decoded.</exception>
  public IReadOnlyList<Decorator> Read(PropertyInfo property)
  {
    var decorators = Selected(property.GetCustomAttributesData()).ToList();

    if (PrimaryParameter(property) is { } parameter)
    {
      var seen = decorators.Select(d => d.Key).ToHashSet(StringComparer.Ordinal);
      decorators.AddRange(Selected(parameter.GetCustomAttributesData()).Where(d => seen.Add(d.Key)));
    }

    return Sorted(decorators);
  }

  /// <summary>Renders an attribute argument as JSON; shared with the keyword mapping for <c>default</c>.</summary>
  public static JsonNode? Render(CustomAttributeTypedArgument argument)
  {
    var value = argument.Value;

    if (value is ReadOnlyCollection<CustomAttributeTypedArgument> items)
    {
      return new JsonArray([.. items.Select(Render)]);
    }

    if (value is not null && argument.ArgumentType.IsEnum)
    {
      return EnumFacts.ClrNameOf(argument.ArgumentType, value) is { } name
        ? JsonValue.Create(name)
        : Number(value);
    }

    return value switch
    {
      null => null,
      Type type => JsonValue.Create(MetadataFacts.TypeName(type)),
      string text => JsonValue.Create(text),
      char letter => JsonValue.Create(letter.ToString()),
      bool flag => JsonValue.Create(flag),
      float single => float.IsFinite(single) ? JsonValue.Create(single) : JsonValue.Create(NonFinite(single)),
      double precise => double.IsFinite(precise) ? JsonValue.Create(precise) : JsonValue.Create(NonFinite(precise)),
      _ => Number(value),
    };
  }

  private IEnumerable<Decorator> Selected(IEnumerable<CustomAttributeData> attributes) =>
    attributes.Where(a => IsSelected(a.AttributeType)).Select(ToDecorator);

  private bool IsSelected(Type attributeType)
  {
    if (HardExcluded.Contains(attributeType.Namespace, StringComparer.Ordinal) ||
        string.Equals(attributeType.FullName, options.MarkerAttributeFullName, StringComparison.Ordinal) ||
        attributeType.FullName is not { } name)
    {
      return false;
    }

    // The defaults first, then the caller's rules in the order they were written; the last rule that matches
    // decides, so a later rule can always take back what an earlier one said. Nothing matching means no.
    var rules = SchemaGenerationOptions.DefaultDecorators.Concat(options.DecoratorRules).ToList();
    for (var i = rules.Count - 1; i >= 0; i--)
    {
      if (Matches(rules[i].Pattern, name))
      {
        return rules[i].Include;
      }
    }

    return false;
  }

  /// <summary>
  /// Whether a full name matches a pattern in which <c>*</c> stands for any run of characters, dots
  /// included — so <c>Ns.*</c> reaches sub-namespaces too, and <c>Ns.*.*</c> is exactly those. Everything
  /// else is literal and compared ordinally.
  /// </summary>
  internal static bool Matches(string pattern, string name)
  {
    // Classic greedy wildcard matching with a single backtrack point, linear in practice.
    int p = 0, n = 0, star = -1, resume = 0;
    while (n < name.Length)
    {
      if (p < pattern.Length && pattern[p] == '*')
      {
        star = p++;
        resume = n;
      }
      else if (p < pattern.Length && pattern[p] == name[n])
      {
        p++;
        n++;
      }
      else if (star >= 0)
      {
        p = star + 1;
        n = ++resume;
      }
      else
      {
        return false;
      }
    }

    while (p < pattern.Length && pattern[p] == '*')
    {
      p++;
    }

    return p == pattern.Length;
  }

  private static Decorator ToDecorator(CustomAttributeData attribute)
  {
    var parameters = attribute.Constructor.GetParameters();
    var arguments = new JsonObject();
    for (var i = 0; i < attribute.ConstructorArguments.Count; i++)
    {
      arguments[parameters[i].Name ?? $"arg{i}"] = Render(attribute.ConstructorArguments[i]);
    }

    var properties = new JsonObject();
    foreach (var named in attribute.NamedArguments.OrderBy(a => a.MemberName, StringComparer.Ordinal))
    {
      properties[named.MemberName] = Render(named.TypedValue);
    }

    var name = MetadataFacts.TypeName(attribute.AttributeType);
    var rendered = new JsonObject { ["name"] = name };
    if (arguments.Count > 0)
    {
      rendered["arguments"] = arguments;
    }

    if (properties.Count > 0)
    {
      rendered["properties"] = properties;
    }

    return new Decorator(name, attribute, rendered, rendered.ToJsonString(Compact));
  }

  /// <summary>
  /// Name first, then the rendered text: the order attributes are stored in metadata is not one any
  /// specification promises, and repeated applications of one attribute need a tie-break that does not
  /// depend on it.
  /// </summary>
  private static IReadOnlyList<Decorator> Sorted(IEnumerable<Decorator> decorators) =>
  [
    .. decorators
      .OrderBy(d => d.Name, StringComparer.Ordinal)
      .ThenBy(d => d.Key, StringComparer.Ordinal),
  ];

  /// <summary>
  /// The primary-constructor parameter a positional record generated the property from, or
  /// <see langword="null" />. A positional record — class or struct — is recognised by the
  /// <c>Deconstruct</c> the compiler emits beside its primary constructor, with the same parameter names in
  /// the same order; an ordinary constructor that merely shares a parameter name does not qualify.
  /// </summary>
  internal static ParameterInfo? PrimaryParameter(PropertyInfo property)
  {
    if (property.DeclaringType is not { } type)
    {
      return null;
    }

    var deconstructions = type
      .GetMethods(BindingFlags.Public | BindingFlags.Instance)
      .Where(m => m.Name == "Deconstruct")
      .Select(m => m.GetParameters().Select(p => p.Name).ToList())
      .ToList();

    if (deconstructions.Count == 0)
    {
      return null;
    }

    return type
      .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
      .Select(c => c.GetParameters())
      .Where(p => deconstructions.Any(d => d.SequenceEqual(p.Select(q => q.Name), StringComparer.Ordinal)))
      .SelectMany(p => p)
      .FirstOrDefault(p => string.Equals(p.Name, property.Name, StringComparison.Ordinal));
  }

  private static JsonNode? Number(object value) => value switch
  {
    sbyte v => JsonValue.Create(v),
    byte v => JsonValue.Create(v),
    short v => JsonValue.Create(v),
    ushort v => JsonValue.Create(v),
    int v => JsonValue.Create(v),
    uint v => JsonValue.Create(v),
    long v => JsonValue.Create(v),
    ulong v => JsonValue.Create(v),
    _ => JsonValue.Create(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)),
  };

  /// <summary>JSON has no literal for these, so they travel as the strings .NET and JavaScript both print.</summary>
  private static string NonFinite(double value) =>
    double.IsNaN(value) ? "NaN" : value > 0 ? "Infinity" : "-Infinity";
}

/// <summary>One selected attribute application.</summary>
/// <param name="Name">The attribute's type name, as <see cref="MetadataFacts.TypeName" /> renders it.</param>
/// <param name="Data">The raw application, for the keyword mapping to read its arguments from.</param>
/// <param name="Rendered">Its <c>x-decorators</c> entry.</param>
/// <param name="Key">The entry as compact JSON: the sort tie-break, and the identity duplicates are found by.</param>
public sealed record Decorator(string Name, CustomAttributeData Data, JsonObject Rendered, string Key);
