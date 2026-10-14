using System.ComponentModel;
using DotNetSchema.Fixtures.Schema.Decorators;
namespace DotNetSchema.Fixtures.Schema.Enums;

/// <summary>
/// The capacity in which a <see cref="Person" /> is attached to the school.
/// </summary>
[Description("The capacity in which a person is attached to the school.")]
[SampleTag("enum-level")]
public enum PersonRole
{
  Student,
  Professor,
  TeachingAssistant,
  Librarian,
  Counsellor,
  Administrator
}
