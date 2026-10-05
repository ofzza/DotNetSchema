using DotNetSchema.Generator.Diagnostics;
using DotNetSchema.Generator.Reflection;

namespace DotNetSchema.Generator.Model;

/// <summary>Assigns each type in one document the name its definition is filed under.</summary>
/// <remarks>
/// Short names, until two types want the same one. Then the shortest namespace suffix that tells the
/// group apart is prepended to <em>every</em> member of it — including the one that was there first,
/// which is why the collision is worth a diagnostic: adding a second <c>Person</c> renames the incumbent
/// <c>Person</c> and moves every golden that mentions it.
/// <para>
/// Names are resolved per document, over that document's closure alone, so that adding a type to one file
/// cannot perturb another. A type can therefore be <c>Person</c> in one document and
/// <c>Fixtures.Person</c> in another.
/// </para>
/// </remarks>
public static class DefinitionNameResolver
{
  /// <summary>Maps each type, by <see cref="MetadataFacts.TypeKey" />, to its definition name.</summary>
  public static IReadOnlyDictionary<string, string> Resolve(
    IReadOnlyList<Type> types,
    IList<SchemaDiagnostic> diagnostics)
  {
    var names = new Dictionary<string, string>(StringComparer.Ordinal);

    foreach (var group in types.GroupBy(ShortName, StringComparer.Ordinal).OrderBy(g => g.Key, StringComparer.Ordinal))
    {
      var members = group.OrderBy(t => t.FullName, StringComparer.Ordinal).ToList();
      if (members.Count == 1)
      {
        names[MetadataFacts.TypeKey(members[0])] = group.Key;
        continue;
      }

      var qualified = Disambiguate(members);
      foreach (var (type, name) in members.Zip(qualified))
      {
        names[MetadataFacts.TypeKey(type)] = name;
      }

      diagnostics.Add(new SchemaDiagnostic(
        SchemaDiagnosticSeverity.Warning,
        SchemaDiagnosticCodes.DefinitionNameCollision,
        $"{members.Count} types share the short name '{group.Key}' " +
        $"({string.Join(", ", members.Select(t => t.FullName))}). They are filed as " +
        $"{string.Join(", ", qualified)}; note that this renames every one of them, not only the newcomer."));
    }

    return names;
  }

  /// <summary>The shortest namespace suffix that tells the group apart, applied to all of it.</summary>
  private static IReadOnlyList<string> Disambiguate(IReadOnlyList<Type> members)
  {
    var segments = members
      .Select(t => (t.Namespace ?? string.Empty).Split('.', StringSplitOptions.RemoveEmptyEntries))
      .ToList();

    var deepest = segments.Max(s => s.Length);

    for (var take = 1; take <= deepest; take++)
    {
      var candidates = members
        .Select((type, index) => Qualify(segments[index], take, ShortName(type)))
        .ToList();

      if (candidates.Distinct(StringComparer.Ordinal).Count() == members.Count)
      {
        return candidates;
      }
    }

    // Unreachable for distinct types, since a full name is unique; kept so the method is total.
    return [.. members.Select(t => (t.FullName ?? t.Name).Replace('+', '.'))];
  }

  private static string Qualify(string[] segments, int take, string shortName)
  {
    var suffix = segments.Skip(Math.Max(0, segments.Length - take));
    return string.Join('.', suffix.Append(shortName));
  }

  /// <summary>The type's name without its namespace, with nesting spelled with dots rather than a plus.</summary>
  private static string ShortName(Type type)
  {
    var full = type.FullName ?? type.Name;
    var withoutNamespace = type.Namespace is { Length: > 0 } space &&
                           full.StartsWith(space + ".", StringComparison.Ordinal)
      ? full[(space.Length + 1)..]
      : full;

    return withoutNamespace.Replace('+', '.');
  }
}
