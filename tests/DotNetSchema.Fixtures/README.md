# DotNetSchema.Fixtures

A deliberately awkward object graph, dressed up as a school.

The project exists to give the generator tests something hard to chew on: a schema that is deeply
nested, mutually recursive, and covers every portable primitive type as a value, inside a collection and
inside a map. `FixturesService` turns a seed number into a concrete instance of that schema,
deterministically and repeatably.

```csharp
IFixturesService fixtures = new FixturesService();

School school = fixtures.Generate(seed: 1);
School again  = fixtures.Generate(seed: 1);   // identical, on any machine, on any runtime

School wider  = fixtures.Generate(1, new FixtureOptions { MaxDepth = 6, MaxCollectionSize = 5 });
AssessmentRecord scalars = fixtures.GenerateAssessmentRecord(42);   // scalars only, no school around them
```

---

## Contents

- [Coverage](#coverage)
- [Schema](#schema)
  - [Pseudocode](#pseudocode)
  - [Graph](#graph)
  - [Recursion](#recursion)
  - [Required and nullable](#required-and-nullable)
  - [Decorators](#decorators)
- [Generation](#generation)
  - [Determinism](#determinism)
  - [Path-derived seeds](#path-derived-seeds)
  - [Depth budget](#depth-budget)
  - [Back-references](#back-references)
  - [Options and cost](#options-and-cost)
- [Schema export](#schema-export)
- [Notes for consumers](#notes-for-consumers)

---

## Coverage

| Requirement | Where it lives |
| --- | --- |
| Every primitive, portable type, once each | `ScalarBattery` — 20 properties, one per type |
| A collection of every such type | `ScalarSeries` — 20 series plus a `byte[]` blob |
| A map of every such type | `ScalarRegister` — 20 string-keyed maps, plus `int`-, `Guid`-, `enum`- and `DateOnly`-keyed maps |
| A nested record | `Campus.Address`, `Assessment.Record`, and most other edges |
| A nested collection of records | `Campus.Buildings`, `Class.Students`, `Book.Chapters`, … |
| A nested map of records | `School.Departments` (enum-keyed), `Library.Catalogue` (enum-keyed) |
| Four or more levels of nesting | `School → Campus → Building → Room → Class → Book → Chapter → Section → Exercise` (9) |
| Direct recursion | `School.SisterSchools`, `Person.Mentor`, `Person.Advisees`, `Class.Prerequisites`, `Department.SubDepartments`, `Chapter.SubChapters` |
| Indirect recursion | `Class ↔ Book`, `Class ↔ Person`, `Class ↔ Room`, `Class ↔ Assessment` |
| Required × nullable, all four combinations, for scalars, arrays, maps, enums and model references | see [Required and nullable](#required-and-nullable) |
| `[JsonRequired]` in place of the `required` modifier | `Book.Isbn` |
| A string-converted enum, with a member renamed by `[JsonStringEnumMemberName]` | `Book.Binding` (`BookBinding`) |
| Every portable type nullable, as a value, as an array item and as a map value | `NullableScalarBattery`, `NullableScalarSeries`, `NullableScalarRegister` — see [Required and nullable](#required-and-nullable) |
| The well-known validation and description attributes, on types, enums and properties | across the graph — see [Decorators](#decorators) |
| Every kind of attribute argument | the `Schema/Decorators/` sampler attributes, on the graph — see [Decorators](#decorators) |
| An attribute on a positional record's constructor parameter; `[JsonRequired]`; DataAnnotations `[Required]` | `Bookmark`, outside the graph — see [Decorators](#decorators) |
| A type described inline by `[DotNetSchemaType]`, serialised by its own converter, nullable | `Person.StudentNumber` (`StudentNumber`, a `pattern`) |
| A generic type mapped on its definition, covering a construction | `Book.CatalogueId` (`CatalogueId<Book>`, `format: uuid`, a converter factory) |

### What counts as "portable"

Twenty types are covered, chosen because each has an unambiguous representation outside the CLR — in
JSON, in TypeScript, on a wire:

`bool` · `sbyte` · `byte` · `short` · `ushort` · `int` · `uint` · `long` · `ulong` · `float` · `double` ·
`decimal` · `char` · `string` · `Guid` · `DateOnly` · `TimeOnly` · `TimeSpan` · `DateTime` ·
`DateTimeOffset`

Deliberately left out, and why:

| Excluded | Reason |
| --- | --- |
| `nint`, `nuint`, `IntPtr`, `UIntPtr` | Width depends on the process, so no fixed representation |
| `Int128`, `UInt128` | No portable counterpart in JSON or TypeScript |
| `Half` | Not portable, and rounds differently across platforms |
| `object`, `dynamic` | No schema to speak of |

Two conventions inside the covered set are worth calling out, because both are places round-tripping
tends to go wrong and the fixtures are built to catch it:

- every `DateTime` is generated with `Kind == Utc`, so a serialiser that silently reinterprets local time
  will produce a mismatch;
- every `DateTimeOffset` is generated with a *non-zero* offset drawn from `-08:00 … +09:00`, so a
  serialiser that normalises to UTC will lose information the fixture can detect.

---

## Schema

### Pseudocode

`Name? : T` marks a property that is not `required` (it may be left out); `Name : T?` marks one that
accepts `null`. Everything else is both required and non-null.

```pseudocode
enum ClassName      { Algebra, Geometry, Calculus, Statistics, Biology, Chemistry, Physics,
                      ComputerScience, Robotics, Astronomy, WorldHistory, Geography, Literature,
                      CreativeWriting, Latin, Philosophy, Economics, MusicTheory, VisualArts,
                      PhysicalEducation }
enum BookName       { PrinciplesOfAlgebra, EuclideanFoundations, CalculusInMotion, ... (20 titles) }
enum DepartmentName { Mathematics, NaturalSciences, ComputerScience, Humanities, SocialSciences,
                      Arts, Athletics, ContinuingEducation }
enum PersonRole     { Student, Professor, TeachingAssistant, Librarian, Counsellor, Administrator }
enum Term           { Autumn, Winter, Spring, Summer }
enum RoomKind       { LectureHall, Classroom, Laboratory, SeminarRoom, Studio, Workshop,
                      Gymnasium, ReadingRoom, Auditorium }
enum AssessmentKind { Quiz, MidtermExam, FinalExam, Essay, LabReport, Practical, OralDefence,
                      GroupProject }
enum BookBinding    { Hardcover, Paperback, SpiralBound }       // written as strings; SpiralBound as "spiral-bound"


// ── The estate ────────────────────────────────────────────────────────────────

record School {
    Id             : Guid
    Name           : string
    Motto?         : string?
    FoundedOn      : DateOnly
    IsAccredited   : bool
    EnrollmentCap? : ushort                                 // 0 = uncapped
    Endowment      : decimal
    Campuses       : Array<Campus>
    Departments    : Map<DepartmentName, Department>        // map of records, enum key
    Library?       : Library?
    SisterSchools  : Array<School>                          // ↺ direct recursion
}

record Campus {
    Id, Name           : Guid, string
    OpenedOn           : DateOnly?
    AreaHectares?      : double
    Address            : Address                            // value record, never truncated
    Buildings          : Array<Building>
}

record Address {
    Street, City, CountryCode             : string
    PostalCode?                           : string?
    Latitude, Longitude                   : double
}

record Building {
    Id, Name      : Guid, string
    FloorCount    : short
    IsAccessible? : bool
    BuiltInYear   : ushort?
    Rooms         : Array<Room>
}

record Room {
    Id, Code : Guid, string
    Kind     : RoomKind?
    Seats?   : ushort
    Floor    : sbyte                                        // negative = basement
    Classes  : Array<Class>                                 // ↻ indirect, back into Class
}


// ── Academia ──────────────────────────────────────────────────────────────────

record Department {
    Id              : Guid
    Name            : DepartmentName
    CostCentre?     : string
    AnnualBudget    : decimal?
    EstablishedOn   : DateOnly
    Head            : Person?
    Courses         : Array<Class>
    SubDepartments  : Array<Department>                     // ↺ direct recursion
}

record Person {
    Id                : Guid
    GivenName         : string
    FamilyName        : string
    Role              : PersonRole
    StudentNumber?    : StudentNumber?                      // [DotNetSchemaType]: inline string, students only
    BornOn            : DateOnly
    Email?            : string?
    IsActive          : bool
    GradePointAverage : double?
    Classes           : Array<Class>                        // ↻ indirect: Person → Class → Person
    Grades?           : Map<ClassName, int>                 // map with an enum key
    ContactMethods?   : Map<string, string>?
    Mentor?           : Person?                             // ↺ direct recursion
    Advisees?         : Array<Person>                       // ↺ direct recursion, across a collection
    Office?           : Room?                               // ↻ indirect: Person → Room → Class → Person
}

record Class {
    Id                 : Guid
    Name               : ClassName
    Code               : string
    Term?              : Term?
    AcademicYear       : ushort
    Credits            : double
    Seats              : byte
    IsEnrollmentOpen   : bool
    Synopsis?          : string?
    Professor          : Person?                            // ↻ indirect
    Students           : Array<Person>                      // ↻ indirect
    Materials          : Array<Book>                        // ↻ indirect: Class → Book → Class
    Prerequisites?     : Array<Class>                       // ↺ direct recursion
    Schedule?          : Array<ClassSession>?
    Assessments        : Array<Assessment>
    Room?              : Room?                              // ↻ indirect
}

record ClassSession {
    Id        : Guid
    Day       : DayOfWeek
    StartsAt  : TimeOnly
    Duration  : TimeSpan
    IsRemote? : bool
    Room?     : Room?
}


// ── The library ───────────────────────────────────────────────────────────────

record Library {
    Id, Name      : Guid, string
    VolumeCount?  : uint
    OpensAt       : TimeOnly
    ClosesAt      : TimeOnly?                               // null = open around the clock
    Catalogue     : Map<BookName, Book>?                    // map of records, enum key
    Librarians    : Array<Person>
    ReadingRooms  : Array<Room>
}

record Book {
    Id                 : Guid
    CatalogueId        : CatalogueId<Book>                  // generic, [DotNetSchemaType] on its definition
    Name               : BookName
    Isbn               : string?                            // [JsonRequired], not the modifier
    Edition            : byte
    PageCount          : ushort
    Price              : decimal
    PublishedOn        : DateOnly
    Binding            : BookBinding                        // the one string-converted enum
    HasDigitalLicence? : bool
    Class?             : Class?                             // ↻ indirect: Book → Class → Book
    Authors            : Array<Person>?
    Chapters           : Array<Chapter>
}

record Chapter {
    Id                    : Guid
    Ordinal               : int
    Title                 : string
    StartPage             : ushort
    EstimatedReadingTime? : TimeSpan?
    Sections              : Array<Section>
    SubChapters?          : Array<Chapter>                  // ↺ direct recursion
}

record Section {
    Id, Number, Body          : Guid, string, string
    Heading                   : string?
    WordCount?                : int
    Exercises                 : Array<Exercise>
}

record Exercise {
    Id, Label, Prompt  : Guid, string, string
    Difficulty?        : float?
    HasWorkedSolution? : bool
    Marks              : byte
}


// ── Assessment, and the scalar coverage bundles ───────────────────────────────

record Assessment {
    Id            : Guid
    Kind?         : AssessmentKind
    Title         : string
    Weight        : decimal
    MaximumMarks  : ushort
    DueAt         : DateTimeOffset?
    IsOpenBook?   : bool
    Class?        : Class?                                  // ↻ indirect
    Record        : AssessmentRecord                        // value record, never truncated
}

record AssessmentRecord {
    Battery   : ScalarBattery      // every portable scalar, once
    Series    : ScalarSeries       // every portable scalar, in a collection
    Register  : ScalarRegister     // every portable scalar, in a map

    NullableBattery   : NullableScalarBattery     // ... and all three again, nullable
    NullableSeries    : NullableScalarSeries
    NullableRegister  : NullableScalarRegister
}

record ScalarBattery {
    IsProctored         : bool              CurveAdjustment     : sbyte
    RawScore            : byte              SeatNumber          : short
    ItemCount           : ushort            TotalPoints         : int
    SubmissionSequence  : uint              ElapsedTicks        : long
    IntegrityChecksum   : ulong             PercentileRank      : float
    ScaledScore         : double            WeightedAverage     : decimal
    LetterGrade         : char              Remarks             : string
    ResponseId          : Guid              AdministeredOn      : DateOnly
    StartedAt           : TimeOnly          AllowedDuration     : TimeSpan
    RecordedAtUtc       : DateTime          SubmittedAt         : DateTimeOffset
}

record ScalarSeries {
    AttemptedFlags       : Array<bool>            CurveAdjustments     : Array<sbyte>
    RawScores            : Array<byte>            SeatNumbers          : Array<short>
    AttemptCounts        : Array<ushort>          PointsPerItem        : Array<int>
    SubmissionSequences  : Array<uint>            ElapsedTicksPerItem  : Array<long>
    IntegrityChecksums   : Array<ulong>           PercentileRanks      : Array<float>
    ScaledScores         : Array<double>          WeightedAverages     : Array<decimal>
    LetterGrades         : Array<char>            Remarks              : Array<string>
    ResponseIds          : Array<Guid>            MarkedOn             : Array<DateOnly>
    OpenedAt             : Array<TimeOnly>        Durations            : Array<TimeSpan>
    RecordedAtUtc        : Array<DateTime>        SavedAt              : Array<DateTimeOffset>
    AnswerSheetScan      : byte[]                 // opaque binary payload, written as base64
}

record ScalarRegister {
    AnsweredByItem       : Map<string, bool>      CurveByItem          : Map<string, sbyte>
    RawScoreByItem       : Map<string, byte>      SeatByItem           : Map<string, short>
    AttemptsByItem       : Map<string, ushort>    PointsByItem         : Map<string, int>
    SequenceByItem       : Map<string, uint>      ElapsedTicksByItem   : Map<string, long>
    ChecksumByItem       : Map<string, ulong>     PercentileByItem     : Map<string, float>
    ScaledScoreByItem    : Map<string, double>    WeightByItem         : Map<string, decimal>
    LetterGradeByItem    : Map<string, char>      RemarkByItem         : Map<string, string>
    ResponseIdByItem     : Map<string, Guid>      MarkedOnByItem       : Map<string, DateOnly>
    OpenedAtByItem       : Map<string, TimeOnly>  DurationByItem       : Map<string, TimeSpan>
    RecordedAtUtcByItem  : Map<string, DateTime>  SavedAtByItem        : Map<string, DateTimeOffset>

    // non-string keys, so key conversion is exercised too
    RemarkByItemNumber      : Map<int, string>
    ScoreByResponseId       : Map<Guid, double>
    WeightByAssessmentKind  : Map<AssessmentKind, decimal>
    AttendanceByDate        : Map<DateOnly, ushort>
}

// The nullable twins: the same property names and types as the bundle each mirrors, with nullable values.

record NullableScalarBattery {     // every property T?, alternately required and optional
    IsProctored   : bool?          CurveAdjustment?   : sbyte?
    RawScore      : byte?          SeatNumber?        : short?
    ...
}

record NullableScalarSeries {      // every item T?; the series cycle R, O, RN, ON
    AttemptedFlags   : Array<bool?>        CurveAdjustments?  : Array<sbyte?>
    RawScores        : Array<byte?>?       SeatNumbers?       : Array<short?>?
    ...
    AnswerSheetScan  : byte[]?             // no items to null, so the blob itself is nullable
}

record NullableScalarRegister {    // every value T?; the maps cycle R, O, RN, ON
    AnsweredByItem   : Map<string, bool?>      CurveByItem?   : Map<string, sbyte?>
    RawScoreByItem   : Map<string, byte?>?     SeatByItem?    : Map<string, short?>?
    ...
}
```

### Graph

Solid edges are ownership, dashed edges are references back into the graph. `[n]` marks a collection,
`{k}` a map keyed by `k`.

```mermaid
flowchart TD
    School["School"]
    Campus["Campus"]
    Address["Address<br/><i>value record</i>"]
    Building["Building"]
    Room["Room"]
    Department["Department"]
    Person["Person"]
    Class["Class"]
    ClassSession["ClassSession"]
    Library["Library"]
    Book["Book"]
    Chapter["Chapter"]
    Section["Section"]
    Exercise["Exercise"]
    Assessment["Assessment"]
    AssessmentRecord["AssessmentRecord<br/><i>value record</i>"]
    Scalars["ScalarBattery + ScalarSeries + ScalarRegister<br/>and their Nullable* twins<br/><i>all 20 portable primitives</i>"]

    School -->|"Campuses [n]"| Campus
    School -->|"Departments {DepartmentName}"| Department
    School -->|"Library ?"| Library
    School -.->|"SisterSchools [n]"| School

    Campus -->|"Address"| Address
    Campus -->|"Buildings [n]"| Building
    Building -->|"Rooms [n]"| Room
    Room -.->|"Classes [n]"| Class

    Department -->|"Head ?"| Person
    Department -->|"Courses [n]"| Class
    Department -.->|"SubDepartments [n]"| Department

    Class -->|"Schedule [n]"| ClassSession
    Class -->|"Assessments [n]"| Assessment
    Class -->|"Materials [n]"| Book
    Class -.->|"Professor ? / Students [n]"| Person
    Class -.->|"Prerequisites [n]"| Class
    Class -.->|"Room ?"| Room
    ClassSession -.->|"Room ?"| Room

    Person -.->|"Classes [n]"| Class
    Person -.->|"Mentor ? / Advisees [n]"| Person
    Person -.->|"Office ?"| Room

    Library -->|"Catalogue {BookName}"| Book
    Library -.->|"Librarians [n]"| Person
    Library -.->|"ReadingRooms [n]"| Room

    Book -->|"Chapters [n]"| Chapter
    Book -.->|"Class ?"| Class
    Book -.->|"Authors [n]"| Person
    Chapter -->|"Sections [n]"| Section
    Chapter -.->|"SubChapters [n]"| Chapter
    Section -->|"Exercises [n]"| Exercise

    Assessment -->|"Record"| AssessmentRecord
    Assessment -.->|"Class ?"| Class
    AssessmentRecord -->|"Battery / Series / Register"| Scalars
```

### Recursion

Stripped of everything else, these are the cycles the schema deliberately contains — the reason a
generator over it needs a termination story at all.

```mermaid
flowchart LR
    subgraph direct["Direct — a type holding its own type"]
        S1["School"] -.->|SisterSchools| S1
        D1["Department"] -.->|SubDepartments| D1
        P1["Person"] -.->|"Mentor, Advisees"| P1
        C1["Class"] -.->|Prerequisites| C1
        Ch1["Chapter"] -.->|SubChapters| Ch1
    end

    subgraph indirect["Indirect — a type reachable from itself through another"]
        C2["Class"] -->|Materials| B2["Book"]
        B2 -->|Class| C2
        C3["Class"] -->|"Professor, Students"| P3["Person"]
        P3 -->|Classes| C3
        C4["Class"] -->|Room| R4["Room"]
        R4 -->|Classes| C4
        C5["Class"] -->|Assessments| A5["Assessment"]
        A5 -->|Class| C5
    end
```

The longest of these runs `Person → Office → Room → Classes → Class → Students → Person`: three hops
home, through two intermediate types. It is also the one most likely to catch a naive reference tracker,
since neither intermediate looks like it belongs to a person.

### Required and nullable

Whether a property is `required` and whether it accepts `null` are separate questions — the schema's
`required` follows the C# `required` modifier or `[JsonRequired]` alone — so the fixtures answer them in all
four combinations, for every shape a property can take. `Book.Isbn` is the one property of the graph marked
with `[JsonRequired]` instead of the modifier (`Bookmark` has another, under [Decorators](#decorators)). A
consumer that turns the schema into types (for example
[`tsschema`](https://www.npmjs.com/package/@ofzza/tsschema)) can therefore tell `x?: T` from
`x: T | null`.

| | Required, non-null<br/>`required T X` → `x: T` | Optional, non-null<br/>`T X { get; init; } = …` → `x?: T` | Required, nullable<br/>`required T? X` → `x: T \| null` | Optional, nullable<br/>`T? X` → `x?: T \| null` |
| --- | --- | --- | --- | --- |
| scalar | most | `School.EnrollmentCap`, `Department.CostCentre`, `Assessment.IsOpenBook`, … | `Campus.OpenedOn`, `Person.GradePointAverage`, `Book.Isbn`, … | `School.Motto`, `Person.Email`, `Exercise.Difficulty`, `Person.StudentNumber` (mapped), … |
| array | most | `Person.Advisees`, `Class.Prerequisites`, `Chapter.SubChapters` | `Book.Authors` | `Class.Schedule` |
| map | most | `Person.Grades` | `Library.Catalogue` | `Person.ContactMethods` |
| enum | most | `Assessment.Kind` | `Room.Kind` | `Class.Term` |
| model | most | — | `Department.Head`, `Class.Professor` | `School.Library`, `Person.Mentor`, `Person.Office`, … |

There is no optional, non-null model reference: a record with `required` members cannot be
default-initialised, so that combination has no C# form to describe.

In the schema, a nullable `$ref` (model or enum) is `{"anyOf": [{"$ref": …}, {"type": "null"}]}`; anything
else is `"type": [T, "null"]`.

The plain scalar bundles stay the required, non-null baseline. Each has a **nullable twin** with exactly the
same property names, hung off `AssessmentRecord`:

- `NullableScalarBattery` — every property `T?`, alternately `required` and optional;
- `NullableScalarSeries` — every series holds `T?` items (`Array<T | null>`), and the series themselves
  cycle by ordinal through the four combinations above; the base64 `AnswerSheetScan` has no items, so it
  is a required, nullable `byte[]?` instead;
- `NullableScalarRegister` — every map holds `T?` values (`Record<K, T | null>`), the maps cycling likewise.
  Keys stay non-null: System.Text.Json cannot write a null property name.

The full lists live in
[`JsonSchemaGeneratorTests.Nullability.cs`](../DotNetSchema.Generator.Tests/JsonSchemaGeneratorTests.Nullability.cs),
and the twins are checked there by rule.

The generator always sets the optional, non-null properties (System.Text.Json writes them regardless). It
fills each nullable property, item and map value at `OptionalFillRate`, deciding from a seed derived from its
path, so the record's other values do not move; a twin is a plain bundle with values knocked out, so it
agrees with the plain bundle drawn from the same seed wherever it is not `null`.

### Decorators

The graph carries attributes for the generator's `x-decorators` export, of two kinds. None of them changes
a generated value: every constraint below already holds for whatever `FixturesService` produces.

**Well-known attributes**, in the generator's default decorator list, so they appear in the documents in
`bin/` and in the golden digests — each as an `x-decorators` entry and as the standard keywords it maps to:

| Attribute | Where |
| --- | --- |
| `[Description]` on a type | `School`, and the enum `PersonRole` |
| `[DisplayName]` on a type | `Campus` |
| `[StringLength(32, MinimumLength = 2)]` | `Person.GivenName` |
| `[MaxLength]` on a string / on an array | `Person.FamilyName` / `Book.Chapters` |
| `[Length(0, 64)]` on an array | `Person.Classes` |
| `[EmailAddress]` on a nullable string | `Person.Email` |
| `[Range]` — `double`, nullable | `Person.GradePointAverage` |
| `[Range]` — `double`, plain and with `MinimumIsExclusive` | `Address.Latitude`, `Address.Longitude` |
| `[Range]` — `int`, narrower than the type's own bounds | `Book.Edition` |
| `[Range(typeof(ushort), "1", "5000")]` | `Book.PageCount` |
| `[RegularExpression]` | `Address.CountryCode` |
| `[Display(Name, Description)]` | `Book.Isbn` |
| `[DefaultValue]` | `Book.HasDigitalLicence` |

**Sampler attributes** — `SampleDecoratorAttribute` and the repeatable `SampleTagAttribute`, in
`Schema/Decorators/` — mean nothing; between them they take every kind of argument an attribute can carry.
That namespace is deliberately outside the default list, so they never reach the documents in `bin/` or the
goldens; a test that wants them includes `DotNetSchema.Fixtures.Schema.Decorators.*`.

| Arguments | Where |
| --- | --- |
| none | `School` |
| every primitive at its minimum, `float.NegativeInfinity`, `double.NaN`, a `null` string | `Person.GivenName` |
| every primitive at its maximum, `float.MaxValue`, `double.PositiveInfinity`, a quote and a non-ASCII character | `Person.FamilyName` |
| a constructed generic type, a single enum member, a boxed `int` | `Book.Isbn` |
| a nested type, a `[Flags]` combination, a boxed `string` | `Book.Name` (a `$ref`) |
| an open generic type, an undeclared enum value, a boxed `Type` | `Address.Street` |
| an array type, the zero enum member, a boxed array | `Address.PostalCode` |
| `int[]`, `string?[]`, `Type[]`, and a mixed `object?[]` | `Address.City` |
| the same four, empty | `Address.CountryCode` |
| named property and field arguments only | `Campus` |
| repeated applications, with and without a `params` array | `School` (`SampleTag` ×3) |
| on an enum type | `PersonRole` (`SampleTag`) |

**`Bookmark`** (`Schema/Decorators/`) is a positional record outside the graph — `FixturesService` never
builds one — for the cases the graph cannot carry without changing what it means:

| Property | Case |
| --- | --- |
| `Href` | `[property: Url]` → `format: uri` |
| `Rank` | `[Range]` written without a target, so on the constructor parameter — still read |
| `Label` | DataAnnotations `[Required]` on an optional, nullable property — listed, but neither required nor narrowed |
| `Note` | `[property: Obsolete]` and `[property: Description]` |
| `Tags` | `SampleTag` on the parameter, on the property, and identically on both (listed once) |
| `Revision` | `[JsonRequired]` without the C# `required` modifier — required in the schema |

---

## Generation

### Determinism

`FixturesService.Generate(seed)` is a pure function. The same seed and the same options produce an equal
graph on any machine, any OS and any runtime version.

That last clause is why the implementation does not use `System.Random`. Its seeded algorithm is an
implementation detail the BCL reserves the right to change, and a fixture that shifts under a framework
upgrade is worse than no fixture at all. `StableRandom` is SplitMix64, written out in
[`Generation/StableRandom.cs`](Generation/StableRandom.cs), and every draw is
defined in terms of integer arithmetic:

- `double` and `float` come from exactly representable integer ratios (`x >> 11` over `2^53`), so no
  platform-dependent rounding creeps in;
- `decimal` is built from an integer numerator over a power of ten, so it is exact rather than a rounded
  binary approximation;
- `Guid` values are drawn from the stream and then stamped with the RFC 4122 version-4 bits, so node
  identity is reproducible;
- every string is formatted with `CultureInfo.InvariantCulture`, so the machine's locale cannot leak in.

### Path-derived seeds

Seeds are derived by *path*, not drawn from one running stream. A node's seed is a pure function of its
parent's seed and the name — and ordinal — of the edge that reached it:

```
seed(root)                   = mix(seedNumber)
seed(parent → "Campuses"[2]) = mix(mix(seed(parent) ^ hash("Campuses")) ^ 2 · γ)
```

Two properties follow, and both are load-bearing:

- **Siblings are independent.** Adding a property to a record, widening a collection or reordering a
  sibling cannot disturb an existing subtree. `Generate(1).Campuses[2]` is the same object whether the
  school has three departments or thirty — which is what makes these fixtures usable in assertions that
  are supposed to stay green across unrelated schema edits.
- **A node can be regenerated in isolation**, without walking to it from the root. That is what makes
  back-references work.

### Depth budget

The schema is cyclic. The graphs it produces are not.

Each traversal carries a `MaxDepth` budget that every entity level spends one unit of. When the budget
runs out the graph is truncated: optional references are left `null`, collections are left empty. The
result is always a finite tree, safe to hand to a naive serialiser, a recursive comparer or a plain
`ToString()` — none of which survive a real object cycle.

Value records — `Address`, `AssessmentRecord`, and the three scalar bundles — do not spend budget and are
always filled in. They are cheap, finite, and they are the point of the fixture; truncating them would
mean generating a marking record with no marks in it.

### Back-references

`Book.Class` and `Assessment.Class` point back at the node that owns them. Rather than inventing an
unrelated class there, the generator re-derives the owner from the owner's own seed under a small budget
(`FixtureOptions.BackReferenceDepth`, `0` by default — identity and scalars, no children).

So this holds, and is worth asserting against:

```csharp
var course = fixtures.Generate(1).Departments.Values.First().Courses.First();

foreach (var book in course.Materials)
{
  Debug.Assert(book.Class!.Id   == course.Id);     // same node, re-derived
  Debug.Assert(book.Class!.Name == course.Name);
  Debug.Assert(book.Class!.Code == course.Code);
  Debug.Assert(book.Class!.Materials.Count == 0);  // …but truncated, so the graph stays a tree
}
```

The back-reference is the one place a chain can run one level past `MaxDepth`: a budget of `n` yields at
most `n` entity edges from the root, plus one more if the last node is a back-reference.

### Options and cost

| Option | Default | Effect |
| --- | --- | --- |
| `MaxDepth` | `4` | Entity levels below the root before truncation |
| `MinCollectionSize` / `MaxCollectionSize` | `1` / `3` | Elements per collection |
| `MapSize` | `3` | Entries per map |
| `SeriesLength` | `4` | Elements per scalar series (the binary blob gets `8×`) |
| `OptionalFillRate` | `0.75` | Chance a nullable property, array item or map value is filled rather than `null` |
| `BackReferenceDepth` | `0` | Budget granted to a back-reference |

Breadth is cheap; depth is not. Cost grows by roughly an order of magnitude per level, so the default
sits at the point where a fixture is still comfortable to eyeball. Measured on `Generate(7)` with
everything else left at its default, serialised size via `System.Text.Json`:

| `MaxDepth` | Time | Serialised size |
| --- | --- | --- |
| 4 (default) | ~5 ms | 0.7 MiB |
| 5 | ~60 ms | 4.5 MiB |
| 6 | ~320 ms | 24 MiB |

Sizes are exact; timings are from one developer machine and are there for the order of magnitude, not
the absolute number.

Two settings worth knowing about:

- `OptionalFillRate = 1.0` fills every nullable property — the densest graph the depth budget allows.
- `OptionalFillRate = 0.0` leaves them all `null`, which is the quick way to exercise the null paths.

---

## Schema export

This project is a live, in-repository consumer of
[`DotNetSchema`](../../src/DotNetSchema/README.md): it references it, imports its `.targets` and sets
`DotNetSchemaGenerate`. It also pins `DotNetSchemaDefaultFileName` to `schema.json`, so the documents in
`bin/` match the generator's default options and the golden digests. Building the project writes three
JSON Schema documents into the output directory:

| Marked type | Document | What it is there to exercise |
| --- | --- | --- |
| `School` | `school.json` | The whole graph — 19 records, 8 enums and `System.DayOfWeek`, nine levels deep, with every cycle closed by `$ref`. |
| `Assessment`, `AssessmentRecord` | `assessment.json` | Two entry points in one document, sharing `Class`, `Person` and `Room` with `school.json`. |
| `ScalarRegister` | `schema.json` | The default file name, and a type defined in two documents at once. |

The attributes change no generated value: a fixture graph is exactly what it was before they were added.
That holds for the [decorators](#decorators) too. The two mapped types, `StudentNumber` and `CatalogueId<T>`,
are described inline rather than defined, and each carries a real `JsonConverter` that writes what its
`[DotNetSchemaType]` declares — the validating tests serialise a generated graph through those converters and
check it against the schema, which is the only thing keeping a declaration and its converter in agreement.
Both values are drawn from seeds of their own, so adding them moved no other generated value.

`Schema/Invalid/` is not part of the school graph and `FixturesService` never constructs any of it. It
holds the shapes an exporter has to have an answer for — a property typed `object`, a generic model, a
record used as a dictionary key, two types with the same short name, two properties with the same JSON
name, a `[Range]` on a string, a malformed `[DotNetSchemaType]`, a marked type that is also mapped, a
converter-written type with no mapping — so that the diagnostics can be tested against real declarations rather
than against types invented inside a test. They carry `[ExportForTesting]` rather than `[DotNetSchema]`, because marking them
properly would export them, and exporting them would fail this project's own build.

---

## Notes for consumers

**Record equality does not go deep.** The compiler-synthesised `Equals` on a `record` compares reference
members with `EqualityComparer<T>.Default`, which for `IReadOnlyList<T>` and `IReadOnlyDictionary<K,V>`
means *reference* equality. So two separately generated schools are **not** `==`, even though they are
byte-for-byte identical once serialised:

```csharp
fixtures.Generate(1) == fixtures.Generate(1)                        // false — collections differ by reference
Serialize(fixtures.Generate(1)) == Serialize(fixtures.Generate(1))  // true
fixtures.GenerateAssessmentRecord(42).Battery
  == fixtures.GenerateAssessmentRecord(42).Battery                  // true — ScalarBattery holds no collections
```

Compare fixtures on their serialised form, or on the seed that produced them.

**The service is stateless and thread-safe.** `FixturesService` holds no fields; a single instance can be
shared, or registered as a singleton:

```csharp
services.AddSingleton<IFixturesService, FixturesService>();
```

**Where to start, depending on what is under test.**

| Under test | Fixture |
| --- | --- |
| Scalar conversion, precision, date handling | `GenerateAssessmentRecord(seed)` |
| Cycles, depth limits, reference handling | `Generate(seed)` |
| Collection and map handling | `Generate(seed)`, or `ScalarSeries` / `ScalarRegister` directly |
| Throughput or payload size | `Generate(seed, new FixtureOptions { MaxDepth = 6 })` |
| A stable corpus of unrelated inputs | `GenerateMany(seed, count)` |

---

## Layout

```
Schema/          the records — plain, immutable, no dependency on the generator
  Enums/         the eight enums
  Decorators/    the sampler attributes, their [Flags] enum, and Bookmark — see Decorators
  Invalid/       shapes that are deliberately wrong, for the JSON Schema exporter's diagnostic tests
Generation/
  FixtureSeed         path-derived seeds
  FixtureOptions      depth and breadth
  FixtureVocabulary   word banks, so fixtures read like a prospectus
  IFixturesService    the public surface
  FixturesService     .cs domain generation · .Graph.cs traversal · .Scalars.cs the scalar bundles and their nullable twins
  StableRandom        the deterministic value source (SplitMix64)
```

---

## License

MIT — see [LICENSE.md](../../LICENSE.md).
