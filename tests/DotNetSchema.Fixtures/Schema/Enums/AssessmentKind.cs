namespace DotNetSchema.Fixtures.Schema.Enums;

/// <summary>
/// The form an assessment takes. Also used as a (non-string) key in <see cref="ScalarRegister" />.
/// </summary>
public enum AssessmentKind
{
  Quiz,
  MidtermExam,
  FinalExam,
  Essay,
  LabReport,
  Practical,
  OralDefence,
  GroupProject
}
