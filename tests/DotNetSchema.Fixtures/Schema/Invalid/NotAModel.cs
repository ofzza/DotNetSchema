namespace DotNetSchema.Fixtures.Schema.Invalid;

/// <summary>An interface marked for export, which cannot be given an object definition. Raises DNS0001.</summary>
[ExportForTesting("not-a-model.json")]
public interface INotAModel
{
  /// <summary>Any member; the type is rejected before its members are looked at.</summary>
  string Name { get; }
}
