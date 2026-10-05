using System.Globalization;
using DotNetSchema.Fixtures.Schema;
using DotNetSchema.Fixtures.Schema.Enums;
using DotNetSchema.Fixtures.Generation;

namespace DotNetSchema.Fixtures.Generation;

/// <summary>
/// The default <see cref="IFixturesService" />.
/// </summary>
/// <remarks>
/// Two ideas carry the whole implementation.
/// <list type="number">
///     <item><b>Path-derived seeds.</b> Every node draws its values from a <see cref="FixtureSeed" />
///     derived from its parent's seed and the edge that reached it, so a node's content depends on where
///     it sits in the graph and on nothing else — not on generation order, and not on its siblings.</item>
///     <item><b>A depth budget.</b> The schema is cyclic, so expansion is bounded by a budget that each
///     entity level spends one unit of. When it runs out, optional references are left <c>null</c> and
///     collections are left empty, which turns the cyclic schema into a finite tree.</item>
/// </list>
/// Back-references — <c>Book.Class</c>, <c>Assessment.Class</c> — are the one place the two ideas pay off
/// together: rather than inventing an unrelated class, the generator re-derives the owning node from the
/// owner's own seed under a small budget. The result agrees with the real parent on identity and on every
/// scalar, while still terminating.
/// </remarks>
public sealed partial class FixturesService : IFixturesService
{
  /// <inheritdoc />
  public School Generate(int seed, FixtureOptions? options = null)
  {
    var settings = options ?? FixtureOptions.Default;
    return GenerateSchool(FixtureSeed.FromSeedNumber(seed).Derive(nameof(School)), settings.MaxDepth, settings);
  }

  /// <inheritdoc />
  public IReadOnlyList<School> GenerateMany(int seed, int count, FixtureOptions? options = null)
  {
    ArgumentOutOfRangeException.ThrowIfNegative(count);

    var settings = options ?? FixtureOptions.Default;
    var root = FixtureSeed.FromSeedNumber(seed);

    var schools = new School[count];
    for (var i = 0; i < count; i++)
    {
      schools[i] = GenerateSchool(root.Derive(nameof(School), i), settings.MaxDepth, settings);
    }

    return schools;
  }

  /// <inheritdoc />
  public Person GeneratePerson(int seed, FixtureOptions? options = null)
  {
    var settings = options ?? FixtureOptions.Default;
    return GeneratePerson(FixtureSeed.FromSeedNumber(seed).Derive(nameof(Person)), settings.MaxDepth, settings);
  }

  /// <inheritdoc />
  public Class GenerateClass(int seed, FixtureOptions? options = null)
  {
    var settings = options ?? FixtureOptions.Default;
    return GenerateClass(FixtureSeed.FromSeedNumber(seed).Derive(nameof(Class)), settings.MaxDepth, settings);
  }

  /// <inheritdoc />
  public Book GenerateBook(int seed, FixtureOptions? options = null)
  {
    var settings = options ?? FixtureOptions.Default;
    return GenerateBook(FixtureSeed.FromSeedNumber(seed).Derive(nameof(Book)), settings.MaxDepth, settings);
  }

  /// <inheritdoc />
  public AssessmentRecord GenerateAssessmentRecord(int seed, FixtureOptions? options = null)
  {
    var settings = options ?? FixtureOptions.Default;
    return GenerateAssessmentRecord(FixtureSeed.FromSeedNumber(seed).Derive(nameof(AssessmentRecord)), settings);
  }

  private static School GenerateSchool(FixtureSeed seed, int budget, FixtureOptions options)
  {
    var random = new StableRandom(seed.Value);

    return new School
    {
      Id = random.NextGuid(),
      Name = $"{random.Pick(FixtureVocabulary.SchoolPrefixes)} {random.Pick(FixtureVocabulary.SchoolSuffixes)}",
      Motto = random.Pick(FixtureVocabulary.Mottos),
      FoundedOn = random.NextDateOnly(new DateOnly(1875, 1, 1), 45_000),
      IsAccredited = random.NextBool(0.85d),
      EnrollmentCap = random.NextUInt16(180, 4_500),
      Endowment = random.NextDecimal(250_000m, 90_000_000m),
      Campuses = GenerateList(
            seed,
            nameof(School.Campuses),
            budget,
            options,
            (s, b) => GenerateCampus(s, b, options)),
      Departments = GenerateKeyedMap<DepartmentName, Department>(
            seed,
            nameof(School.Departments),
            budget,
            options,
            (s, b, name) => GenerateDepartment(s, b, options, name)),
      Library = GenerateOptional(
            seed,
            nameof(School.Library),
            budget,
            options,
            (s, b) => GenerateLibrary(s, b, options)),
      SisterSchools = GenerateList(
            seed,
            nameof(School.SisterSchools),
            budget,
            options,
            (s, b) => GenerateSchool(s, b, options))
    };
  }

  private static Campus GenerateCampus(FixtureSeed seed, int budget, FixtureOptions options)
  {
    var random = new StableRandom(seed.Value);

    return new Campus
    {
      Id = random.NextGuid(),
      Name = random.Pick(FixtureVocabulary.CampusNames),
      Address = GenerateAddress(seed.Derive(nameof(Campus.Address))),
      OpenedOn = random.NextDateOnly(new DateOnly(1900, 1, 1), 44_000),
      AreaHectares = random.NextDouble(0.4d, 65d),
      Buildings = GenerateList(
            seed,
            nameof(Campus.Buildings),
            budget,
            options,
            (s, b) => GenerateBuilding(s, b, options))
    };
  }

  private static Address GenerateAddress(FixtureSeed seed)
  {
    var random = new StableRandom(seed.Value);

    return new Address
    {
      Street = string.Create(
            CultureInfo.InvariantCulture,
            $"{random.Pick(FixtureVocabulary.Streets)} {random.NextInt32(1, 180)}"),
      City = random.Pick(FixtureVocabulary.Cities),
      PostalCode = random.NextInt32(10_000, 99_999).ToString(CultureInfo.InvariantCulture),
      CountryCode = random.Pick(FixtureVocabulary.CountryCodes),
      Latitude = random.NextDouble(35d, 62d, 6),
      Longitude = random.NextDouble(-9d, 26d, 6)
    };
  }

  private static Building GenerateBuilding(FixtureSeed seed, int budget, FixtureOptions options)
  {
    var random = new StableRandom(seed.Value);

    return new Building
    {
      Id = random.NextGuid(),
      Name = random.Pick(FixtureVocabulary.BuildingNames),
      FloorCount = random.NextInt16(1, 9),
      IsAccessible = random.NextBool(0.7d),
      BuiltInYear = random.NextUInt16(1890, 2024),
      Rooms = GenerateList(seed, nameof(Building.Rooms), budget, options, (s, b) => GenerateRoom(s, b, options))
    };
  }

  private static Room GenerateRoom(FixtureSeed seed, int budget, FixtureOptions options)
  {
    var random = new StableRandom(seed.Value);

    return new Room
    {
      Id = random.NextGuid(),
      Code = string.Create(
            CultureInfo.InvariantCulture,
            $"{random.NextChar("ABCDE")}-{random.NextInt32(1, 5)}{random.NextInt32(0, 40):00}"),
      Kind = random.PickEnum<RoomKind>(),
      Seats = random.NextUInt16(8, 320),
      Floor = random.NextSByte(-2, 8),
      Classes = GenerateList(seed, nameof(Room.Classes), budget, options, (s, b) => GenerateClass(s, b, options))
    };
  }

  private static Department GenerateDepartment(
      FixtureSeed seed,
      int budget,
      FixtureOptions options,
      DepartmentName? name = null)
  {
    var random = new StableRandom(seed.Value);

    return new Department
    {
      Id = random.NextGuid(),
      Name = name ?? random.PickEnum<DepartmentName>(),
      CostCentre = StableRandom.Code("CC-", random.NextInt32(100, 999), 3),
      AnnualBudget = random.NextDecimal(35_000m, 4_200_000m),
      EstablishedOn = random.NextDateOnly(new DateOnly(1920, 1, 1), 36_000),
      Head = GenerateOptional(
            seed,
            nameof(Department.Head),
            budget,
            options,
            (s, b) => GeneratePerson(s, b, options, PersonRole.Professor)),
      Courses = GenerateList(
            seed,
            nameof(Department.Courses),
            budget,
            options,
            (s, b) => GenerateClass(s, b, options)),
      SubDepartments = GenerateList(
            seed,
            nameof(Department.SubDepartments),
            budget,
            options,
            (s, b) => GenerateDepartment(s, b, options))
    };
  }

  private static Person GeneratePerson(FixtureSeed seed, int budget, FixtureOptions options, PersonRole? role = null)
  {
    var random = new StableRandom(seed.Value);

    var givenName = random.Pick(FixtureVocabulary.GivenNames);
    var familyName = random.Pick(FixtureVocabulary.FamilyNames);
    var effectiveRole = role ?? random.PickEnum<PersonRole>();

    var classes = GenerateList(
        seed,
        nameof(Person.Classes),
        budget,
        options,
        (s, b) => GenerateClass(s, b, options));

    return new Person
    {
      Id = random.NextGuid(),
      GivenName = givenName,
      FamilyName = familyName,
      Role = effectiveRole,
      BornOn = effectiveRole == PersonRole.Student
            ? random.NextDateOnly(new DateOnly(2001, 1, 1), 2_600)
            : random.NextDateOnly(new DateOnly(1958, 1, 1), 14_600),
      Email = $"{givenName}.{familyName}@school.example".ToLowerInvariant(),
      IsActive = random.NextBool(0.9d),
      GradePointAverage = random.NextDouble(1d, 5d),
      Classes = classes,
      Grades = GenerateGrades(seed.Derive(nameof(Person.Grades)), classes, options),
      ContactMethods = GenerateContactMethods(seed.Derive(nameof(Person.ContactMethods)), options),
      Mentor = GenerateOptional(
            seed,
            nameof(Person.Mentor),
            budget,
            options,
            (s, b) => GeneratePerson(s, b, options, PersonRole.Professor)),
      Advisees = GenerateList(
            seed,
            nameof(Person.Advisees),
            budget,
            options,
            (s, b) => GeneratePerson(s, b, options, PersonRole.Student)),
      Office = GenerateOptional(
            seed,
            nameof(Person.Office),
            budget,
            options,
            (s, b) => GenerateRoom(s, b, options))
    };
  }

  /// <summary>
  /// Marks are keyed by the classes the person actually attends, topped up with further classes when the
  /// depth budget left the <c>Classes</c> collection short — a transcript naturally outlives an enrolment.
  /// </summary>
  private static IReadOnlyDictionary<ClassName, int> GenerateGrades(
      FixtureSeed seed,
      IReadOnlyList<Class> classes,
      FixtureOptions options)
  {
    var random = new StableRandom(seed.Value);
    var grades = new Dictionary<ClassName, int>();

    foreach (var attended in classes)
    {
      grades[attended.Name] = random.NextInt32(41, 101);
    }

    foreach (var name in random.PickDistinctEnums<ClassName>(options.MapSize))
    {
      if (grades.Count >= options.MapSize + classes.Count)
      {
        break;
      }

      grades.TryAdd(name, random.NextInt32(41, 101));
    }

    return grades;
  }

  private static IReadOnlyDictionary<string, string> GenerateContactMethods(FixtureSeed seed, FixtureOptions options)
  {
    var random = new StableRandom(seed.Value);
    var contacts = new Dictionary<string, string>(StringComparer.Ordinal);

    var channels = FixtureVocabulary.ContactChannels;
    for (var i = 0; i < Math.Min(options.MapSize, channels.Length); i++)
    {
      contacts[channels[i]] = string.Create(
          CultureInfo.InvariantCulture,
          $"+385 {random.NextInt32(91, 99)} {random.NextInt32(100, 999)} {random.NextInt32(1000, 9999)}");
    }

    return contacts;
  }

  private static Class GenerateClass(FixtureSeed seed, int budget, FixtureOptions options, ClassName? name = null)
  {
    var random = new StableRandom(seed.Value);

    var className = name ?? random.PickEnum<ClassName>();

    return new Class
    {
      Id = random.NextGuid(),
      Name = className,
      Code = string.Create(
            CultureInfo.InvariantCulture,
            $"{className.ToString()[..3].ToUpperInvariant()}-{random.NextInt32(100, 500)}"
            + $"-{random.NextChar("ABC")}"),
      Term = random.PickEnum<Term>(),
      AcademicYear = random.NextUInt16(2019, 2027),
      Credits = random.NextDouble(1d, 12d, 1),
      Seats = random.NextByte(6, 200),
      IsEnrollmentOpen = random.NextBool(0.6d),
      Synopsis = random.Pick(FixtureVocabulary.Synopses),
      Professor = GenerateOptional(
            seed,
            nameof(Class.Professor),
            budget,
            options,
            (s, b) => GeneratePerson(s, b, options, PersonRole.Professor)),
      Students = GenerateList(
            seed,
            nameof(Class.Students),
            budget,
            options,
            (s, b) => GeneratePerson(s, b, options, PersonRole.Student)),
      Materials = GenerateList(
            seed,
            nameof(Class.Materials),
            budget,
            options,
            (s, b) => GenerateBook(s, b, options, owningClassSeed: seed)),
      Prerequisites = GenerateList(
            seed,
            nameof(Class.Prerequisites),
            budget,
            options,
            (s, b) => GenerateClass(s, b, options)),
      Schedule = GenerateList(
            seed,
            nameof(Class.Schedule),
            budget,
            options,
            (s, b) => GenerateClassSession(s, b, options)),
      Assessments = GenerateList(
            seed,
            nameof(Class.Assessments),
            budget,
            options,
            (s, b) => GenerateAssessment(s, b, options, owningClassSeed: seed)),
      Room = GenerateOptional(seed, nameof(Class.Room), budget, options, (s, b) => GenerateRoom(s, b, options))
    };
  }

  private static ClassSession GenerateClassSession(FixtureSeed seed, int budget, FixtureOptions options)
  {
    var random = new StableRandom(seed.Value);

    return new ClassSession
    {
      Id = random.NextGuid(),
      Day = random.Pick<DayOfWeek>(
        [
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday
        ]),
      StartsAt = random.NextTimeOnly(8, 19, 15),
      Duration = random.NextTimeSpan(45, 180),
      IsRemote = random.NextBool(0.25d),
      Room = GenerateOptional(
            seed,
            nameof(ClassSession.Room),
            budget,
            options,
            (s, b) => GenerateRoom(s, b, options))
    };
  }

  private static Book GenerateBook(
      FixtureSeed seed,
      int budget,
      FixtureOptions options,
      BookName? name = null,
      FixtureSeed? owningClassSeed = null)
  {
    var random = new StableRandom(seed.Value);

    // A back-reference is re-derived from the owner's own seed under a small budget, so it agrees with
    // the real parent on identity and scalars without the graph ever closing a cycle.
    var prescribedBy = owningClassSeed is not null
        ? GenerateClass(owningClassSeed.Value, options.BackReferenceDepth, options)
        : GenerateOptional(seed, nameof(Book.Class), budget, options, (s, b) => GenerateClass(s, b, options));

    return new Book
    {
      Id = random.NextGuid(),
      Name = name ?? random.PickEnum<BookName>(),
      Isbn = string.Create(
            CultureInfo.InvariantCulture,
            $"978-{random.NextInt32(0, 10)}-{random.NextInt32(100, 999)}"
            + $"-{random.NextInt32(10_000, 99_999)}-{random.NextInt32(0, 10)}"),
      Edition = random.NextByte(1, 12),
      PageCount = random.NextUInt16(64, 1_400),
      Price = random.NextDecimal(9.99m, 240m),
      PublishedOn = random.NextDateOnly(new DateOnly(1960, 1, 1), 23_000),
      HasDigitalLicence = random.NextBool(0.55d),
      Class = prescribedBy,
      Authors = GenerateList(
            seed,
            nameof(Book.Authors),
            budget,
            options,
            (s, b) => GeneratePerson(s, b, options, PersonRole.Professor)),
      Chapters = GenerateList(
            seed,
            nameof(Book.Chapters),
            budget,
            options,
            (s, b) => GenerateChapter(s, b, options, 1))
    };
  }

  private static Chapter GenerateChapter(FixtureSeed seed, int budget, FixtureOptions options, int ordinal)
  {
    var random = new StableRandom(seed.Value);

    return new Chapter
    {
      Id = random.NextGuid(),
      Ordinal = ordinal,
      Title = random.Pick(FixtureVocabulary.ChapterTitles),
      StartPage = random.NextUInt16(1, 1_200),
      EstimatedReadingTime = random.NextTimeSpan(10, 240),
      Sections = GenerateList(
            seed,
            nameof(Chapter.Sections),
            budget,
            options,
            (s, b, i) => GenerateSection(s, b, options, ordinal, i + 1)),
      SubChapters = GenerateList(
            seed,
            nameof(Chapter.SubChapters),
            budget,
            options,
            (s, b, i) => GenerateChapter(s, b, options, i + 1))
    };
  }

  private static Section GenerateSection(
      FixtureSeed seed,
      int budget,
      FixtureOptions options,
      int chapter,
      int ordinal)
  {
    var random = new StableRandom(seed.Value);
    var number = string.Create(CultureInfo.InvariantCulture, $"{chapter}.{ordinal}");

    return new Section
    {
      Id = random.NextGuid(),
      Number = number,
      Heading = random.Pick(FixtureVocabulary.SectionHeadings),
      Body = random.Pick(FixtureVocabulary.SectionBodies),
      WordCount = random.NextInt32(120, 3_500),
      Exercises = GenerateList(
            seed,
            nameof(Section.Exercises),
            budget,
            options,
            (s, _, i) => GenerateExercise(s, number, i + 1))
    };
  }

  private static Exercise GenerateExercise(FixtureSeed seed, string sectionNumber, int ordinal)
  {
    var random = new StableRandom(seed.Value);

    return new Exercise
    {
      Id = random.NextGuid(),
      Label = string.Create(CultureInfo.InvariantCulture, $"{sectionNumber}.{ordinal}"),
      Prompt = random.Pick(FixtureVocabulary.ExercisePrompts),
      Difficulty = random.NextSingle(1f, 5f, 1),
      HasWorkedSolution = random.NextBool(0.4d),
      Marks = random.NextByte(1, 20)
    };
  }

  private static Library GenerateLibrary(FixtureSeed seed, int budget, FixtureOptions options)
  {
    var random = new StableRandom(seed.Value);
    var opensAt = random.NextTimeOnly(6, 10, 30);

    return new Library
    {
      Id = random.NextGuid(),
      Name = random.Pick(FixtureVocabulary.LibraryNames),
      VolumeCount = (uint)random.NextInt64(1_200L, 2_400_000L),
      OpensAt = opensAt,
      ClosesAt = opensAt.AddHours(random.NextInt32(8, 15)),
      Catalogue = GenerateKeyedMap<BookName, Book>(
            seed,
            nameof(Library.Catalogue),
            budget,
            options,
            (s, b, name) => GenerateBook(s, b, options, name)),
      Librarians = GenerateList(
            seed,
            nameof(Library.Librarians),
            budget,
            options,
            (s, b) => GeneratePerson(s, b, options, PersonRole.Librarian)),
      ReadingRooms = GenerateList(
            seed,
            nameof(Library.ReadingRooms),
            budget,
            options,
            (s, b) => GenerateRoom(s, b, options))
    };
  }

  private static Assessment GenerateAssessment(
      FixtureSeed seed,
      int budget,
      FixtureOptions options,
      FixtureSeed? owningClassSeed = null)
  {
    var random = new StableRandom(seed.Value);

    var setFor = owningClassSeed is not null
        ? GenerateClass(owningClassSeed.Value, options.BackReferenceDepth, options)
        : GenerateOptional(
            seed,
            nameof(Assessment.Class),
            budget,
            options,
            (s, b) => GenerateClass(s, b, options));

    return new Assessment
    {
      Id = random.NextGuid(),
      Kind = random.PickEnum<AssessmentKind>(),
      Title = $"{random.PickEnum<AssessmentKind>()} — {random.Pick(FixtureVocabulary.ChapterTitles)}",
      Weight = random.NextDecimal(0.05m, 0.6m),
      MaximumMarks = random.NextUInt16(10, 200),
      DueAt = random.NextDateTimeOffset(FixtureVocabulary.EpochUtc, 2_200),
      IsOpenBook = random.NextBool(0.3d),
      Class = setFor,

      // A value record: cheap, finite and the whole point of the fixture, so it is never truncated.
      Record = GenerateAssessmentRecord(seed.Derive(nameof(Assessment.Record)), options)
    };
  }
}
