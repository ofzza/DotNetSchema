using DotNetSchema.Generator.Diagnostics;
using DotNetSchema.Generator.Reflection;

namespace DotNetSchema.Generator.Model;

/// <summary>
/// Collects every type that has to be defined in one document: the marked types, and everything reachable
/// from them through properties, array elements, and both halves of a dictionary.
/// </summary>
/// <remarks>
/// Cycles need no special handling beyond the visited set, and no depth budget — which is worth saying
/// out loud in a repository whose fixtures are built around one. A schema describes a shape rather than
/// an instance, so <c>Person.mentor</c> pointing at <c>#/$defs/Person</c> is a complete and honest
/// answer where a generator producing an object graph would have to truncate.
/// </remarks>
/// <param name="options">The generation options in force.</param>
/// <param name="diagnostics">Where to report types that cannot be defined.</param>
public sealed class ClosureWalker(SchemaGenerationOptions options, IList<SchemaDiagnostic> diagnostics)
{
  private readonly TypeClassifier _classifier = new(options, diagnostics);
  private readonly SortedSet<string> _unannotated = new(StringComparer.Ordinal);

  /// <summary>
  /// Assemblies that contributed a reference-typed member the compiler said nothing about, so its
  /// nullability had to be guessed at. Named rather than reported here, because one warning per assembly
  /// is actionable and one per member is noise.
  /// </summary>
  /// <remarks>
  /// Determined from the members actually walked, not from the assembly as a whole. There is no
  /// assembly-level signal to read: a project with nullable reference types enabled whose public surface
  /// happens to use no reference types emits no nullability metadata either, and would be indistinguishable
  /// from one that has the feature switched off.
  /// </remarks>
  public IReadOnlyCollection<string> UnannotatedAssemblies => _unannotated;

  /// <summary>
  /// Every type needing a definition, ordered by full name so the result does not depend on discovery order.
  /// </summary>
  public IReadOnlyList<Type> Walk(IEnumerable<Type> roots)
  {
    var visited = new Dictionary<string, Type>(StringComparer.Ordinal);
    var pending = new Queue<Type>();

    foreach (var root in roots)
    {
      Enqueue(root, visited, pending);
    }

    while (pending.Count > 0)
    {
      var type = pending.Dequeue();
      if (type.IsEnum)
      {
        continue;
      }

      foreach (var member in JsonMemberReader.Read(type, options))
      {
        if (member.Nullability.IsUnknown && !member.Type.IsValueType)
        {
          _unannotated.Add(type.Assembly.GetName().Name ?? type.Assembly.FullName ?? "<unknown>");
        }

        var reference = _classifier.Classify(
          member.Type,
          member.Nullability,
          member.ConverterTypeFullName,
          type.FullName ?? type.Name,
          member.Property.Name);

        foreach (var nested in Definable(reference))
        {
          Enqueue(nested, visited, pending);
        }
      }

      ReportNameCollisions(type);
    }

    return [.. visited.Values.OrderBy(t => t.FullName, StringComparer.Ordinal)];
  }

  /// <summary>The types inside a position that need a definition of their own.</summary>
  private static IEnumerable<Type> Definable(SchemaTypeRef reference)
  {
    switch (reference.Kind)
    {
      case SchemaTypeKind.Model or SchemaTypeKind.Enum:
        yield return reference.Type;
        break;

      case SchemaTypeKind.Array or SchemaTypeKind.Map:
        foreach (var nested in new[] { reference.Key, reference.Item }.OfType<SchemaTypeRef>())
        {
          foreach (var type in Definable(nested))
          {
            yield return type;
          }
        }

        break;
    }
  }

  private static void Enqueue(Type type, Dictionary<string, Type> visited, Queue<Type> pending)
  {
    if (visited.TryAdd(MetadataFacts.TypeKey(type), type))
    {
      pending.Enqueue(type);
    }
  }

  private void ReportNameCollisions(Type type)
  {
    var duplicates = JsonMemberReader
      .Read(type, options)
      .GroupBy(m => m.JsonName, StringComparer.Ordinal)
      .Where(g => g.Count() > 1);

    foreach (var duplicate in duplicates)
    {
      diagnostics.Add(new SchemaDiagnostic(
        SchemaDiagnosticSeverity.Warning,
        SchemaDiagnosticCodes.PropertyNameCollision,
        $"Type '{type.FullName}' has {duplicate.Count()} properties that all serialise as " +
        $"'{duplicate.Key}' ({string.Join(", ", duplicate.Select(m => m.Property.Name))}). " +
        "System.Text.Json throws on this at run time.",
        type.FullName));
    }
  }
}
