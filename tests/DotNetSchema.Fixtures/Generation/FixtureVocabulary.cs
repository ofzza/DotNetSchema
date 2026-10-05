namespace DotNetSchema.Fixtures.Generation;

/// <summary>
/// Word banks the generator draws from, so that fixtures read like a school prospectus rather than
/// like a hex dump.
/// </summary>
internal static class FixtureVocabulary
{
  /// <summary>The academic epoch every generated date is measured from.</summary>
  internal static readonly DateOnly Epoch = new(2019, 9, 1);

  /// <summary>The same epoch as an instant, for the <c>DateTime</c> and <c>DateTimeOffset</c> draws.</summary>
  internal static readonly DateTime EpochUtc = new(2019, 9, 1, 0, 0, 0, DateTimeKind.Utc);

  internal static readonly string[] GivenNames =
  [
      "Ada", "Bruno", "Clara", "Dario", "Elena", "Felix", "Greta", "Hugo", "Ines", "Jonas",
        "Kata", "Luka", "Mira", "Nikola", "Olga", "Petar", "Quinn", "Rosa", "Stjepan", "Tara",
        "Uma", "Vedran", "Wanda", "Xenia", "Yara", "Zoran"
  ];

  internal static readonly string[] FamilyNames =
  [
      "Abramov", "Bernard", "Castellan", "Dahl", "Everly", "Farkas", "Gallo", "Horvat", "Ivanic",
        "Jelinek", "Kovac", "Lindqvist", "Marchetti", "Novak", "Olsen", "Pavlovic", "Radic",
        "Sandoval", "Tomasi", "Urban", "Vidmar", "Weiss", "Zenker"
  ];

  internal static readonly string[] SchoolPrefixes =
  [
      "Northgate", "Ashgrove", "Kingsmere", "Silverbrook", "Thornfield", "Westhaven", "Eastmoor",
        "Lindenhall", "Blackwater", "Rosemont"
  ];

  internal static readonly string[] SchoolSuffixes =
  [
      "Preparatory Academy", "Grammar School", "Collegiate Institute", "School of Sciences",
        "Academy of Arts and Letters", "Lyceum", "Polytechnic School"
  ];

  internal static readonly string[] Mottos =
  [
      "Per aspera ad astra", "Doce ut discas", "Lux et veritas", "Sapere aude", "Non scholae sed vitae",
        "Vincit qui se vincit", "Ex libris, lumen", "Disce aut discede"
  ];

  internal static readonly string[] CampusNames =
  [
      "Riverside Campus", "Old Town Campus", "Hillcrest Campus", "Lakefront Campus",
        "Foundry Quarter Campus", "Orchard Campus"
  ];

  internal static readonly string[] BuildingNames =
  [
      "Faraday Hall", "Curie Wing", "Gutenberg House", "Turing Block", "Hypatia Pavilion",
        "Mendel Annex", "Lovelace Court", "Bohr Laboratory", "Tesla Workshop", "Aristotle Rotunda"
  ];

  internal static readonly string[] LibraryNames =
  [
      "The Ashgrove Reading Rooms", "Hypatia Library", "The Long Room", "Alexandria Wing",
        "The Quiet Stacks", "Codex Hall"
  ];

  internal static readonly string[] Streets =
  [
      "Chapel Lane", "Meridian Avenue", "Founders Road", "Old Mill Street", "Vine Terrace",
        "Belvedere Walk", "Kestrel Way", "Quarry Hill Road"
  ];

  internal static readonly string[] Cities =
  [
      "Zagreb", "Bergen", "Utrecht", "Bologna", "Ghent", "Aarhus", "Coimbra", "Uppsala", "Graz", "Leuven"
  ];

  internal static readonly string[] CountryCodes = ["HR", "NO", "NL", "IT", "BE", "DK", "PT", "SE", "AT"];

  internal static readonly string[] ChapterTitles =
  [
      "First Principles", "Notation and Conventions", "Working Methods", "Common Pitfalls",
        "Historical Notes", "Applications in the Field", "Proofs and Derivations", "Case Studies",
        "Further Reading", "Review and Consolidation"
  ];

  internal static readonly string[] SectionHeadings =
  [
      "Defining the terms", "A worked example", "Why the naive approach fails", "Two useful identities",
        "Reading the diagram", "From theory to practice", "Boundary conditions", "A note on units",
        "Summary of results", "Points of confusion"
  ];

  internal static readonly string[] SectionBodies =
  [
      "The material below assumes only what was established in the preceding section; nothing further is required.",
        "Students find this step the hardest, so it is worked through in full before the shortcut is given.",
        "Note that the result holds only under the stated assumptions; the general case is deferred to the exercises.",
        "What follows is best read with pen in hand, reproducing each line before moving on to the next.",
        "The derivation is short, but every term in it earns its place; none may be dropped without consequence."
  ];

  internal static readonly string[] ExercisePrompts =
  [
      "State the definition in your own words, then give a counterexample to the converse.",
        "Derive the result from first principles, showing every intermediate step.",
        "Identify the error in the working shown opposite and correct it.",
        "Estimate the answer before computing it, then account for the difference.",
        "Sketch the relationship and mark the points at which it fails.",
        "Compare the two methods and justify your preference in a short paragraph."
  ];

  internal static readonly string[] MarkerRemarks =
  [
      "Well argued throughout.", "Method sound, arithmetic slipped.", "Answer correct, working incomplete.",
        "Shows genuine insight.", "Misread the question; the work itself is fine.", "Needs more supporting detail.",
        "Concise and correct.", "Reasoning circular in the second half."
  ];

  internal static readonly string[] Synopses =
  [
      "An introduction to the subject for students with no prior exposure, taught from worked examples.",
        "A second course, consolidating the first and extending it towards independent project work.",
        "A seminar course; enrolment is capped and attendance at every session is expected.",
        "A laboratory course, assessed continuously on written reports rather than on a final paper.",
        "A survey course, intended to give breadth before students choose a specialism."
  ];

  internal static readonly string[] ContactChannels = ["mobile", "office", "home", "emergency", "signal"];

  internal const string GradeLetters = "ABCDEF";
}
