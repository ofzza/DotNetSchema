using DotNetSchema.Fixtures.Schema;

namespace DotNetSchema.Fixtures.Generation;

/// <summary>
/// Produces fixture graphs over the schooling schema. Every method is a pure function of its arguments:
/// the same seed and the same options always yield an equal graph, on any machine and any runtime.
/// </summary>
public interface IFixturesService
{
  /// <summary>
  /// Generates a whole school — the root of the fixture graph, and the entry point most callers want.
  /// </summary>
  /// <param name="seed">
  /// Any integer. Distinct seeds give unrelated graphs; the same seed always gives the same graph.
  /// </param>
  /// <param name="options">
  /// Depth and breadth of the expansion; <see cref="FixtureOptions.Default" /> when omitted.
  /// </param>
  /// <returns>A finite, acyclic school graph.</returns>
  School Generate(int seed, FixtureOptions? options = null);

  /// <summary>Generates <paramref name="count" /> unrelated schools from a single seed.</summary>
  /// <param name="seed">Any integer.</param>
  /// <param name="count">How many schools to generate.</param>
  /// <param name="options">Depth and breadth of the expansion.</param>
  /// <returns>The generated schools, in a stable order.</returns>
  IReadOnlyList<School> GenerateMany(int seed, int count, FixtureOptions? options = null);

  /// <summary>Generates a single person, without the surrounding school.</summary>
  /// <param name="seed">Any integer.</param>
  /// <param name="options">Depth and breadth of the expansion.</param>
  /// <returns>A person and as much of their context as the depth budget allows.</returns>
  Person GeneratePerson(int seed, FixtureOptions? options = null);

  /// <summary>Generates a single class offering, without the surrounding school.</summary>
  /// <param name="seed">Any integer.</param>
  /// <param name="options">Depth and breadth of the expansion.</param>
  /// <returns>A class offering and as much of its context as the depth budget allows.</returns>
  Class GenerateClass(int seed, FixtureOptions? options = null);

  /// <summary>Generates a single book, without the surrounding school.</summary>
  /// <param name="seed">Any integer.</param>
  /// <param name="options">Depth and breadth of the expansion.</param>
  /// <returns>A book and as much of its context as the depth budget allows.</returns>
  Book GenerateBook(int seed, FixtureOptions? options = null);

  /// <summary>
  /// Generates a marking record on its own. This is the narrow fixture to reach for when the thing under
  /// test is scalar handling — every portable primitive appears in it as a value, in a collection and in
  /// a map — and the surrounding school would only be noise.
  /// </summary>
  /// <param name="seed">Any integer.</param>
  /// <param name="options">Series length and map size are read from here; depth is not used.</param>
  /// <returns>A fully populated marking record.</returns>
  AssessmentRecord GenerateAssessmentRecord(int seed, FixtureOptions? options = null);
}
