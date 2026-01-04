# GitHub Copilot Instructions for LanguageTags

## Project Overview

**LanguageTags** is a C# .NET library for handling ISO 639-2, ISO 639-3, and RFC 5646 / BCP 47 language tags. The project serves two primary purposes:

1. **Data Publishing**: Provides ISO 639-2, ISO 639-3, and RFC 5646 language tag records in JSON and C# formats
2. **Tag Processing**: Implements IETF BCP 47 language tag construction and parsing per RFC 5646 semantic rules

## Solution Structure

### Projects

- **LanguageTags** (`LanguageTags/LanguageTags.csproj`)
  - Core library project
  - NuGet package: `ptr727.LanguageTags`
  - Contains language tag data models, parser, builder, and lookup functionality
  - Target framework: .NET 10.0
  - C# language version: 14.0

- **LanguageTagsCreate** (`LanguageTagsCreate/LanguageTagsCreate.csproj`)
  - CLI utility for downloading and generating language data
  - Downloads data from official sources (Library of Congress, SIL, IANA)
  - Converts to JSON and generates C# code files
  - Target framework: .NET 10.0

- **LanguageTagsTests** (`LanguageTagsTests/LanguageTagsTests.csproj`)
  - xUnit test suite with 211+ comprehensive tests
  - Uses AwesomeAssertions for test assertions
  - 100% coverage of all public APIs
  - Target framework: .NET 10.0

### Key Directories

- **LanguageData/**
  - Contains downloaded language data files
  - JSON converted data files
  - Updated weekly via GitHub Actions

- **.github/workflows/**
  - `update-languagedata.yml`: Weekly scheduled job to update language data
  - `publish-release.yml`: Release and NuGet publishing workflow
  - `merge-bot-pr.yml`: Automated PR merge workflow

## Core Components

### LanguageTag Class (LanguageTag.cs)

The main public API for working with language tags:

**Static Factory Methods:**
- `Parse(string tag)`: Parse a language tag string, returns null on failure
- `TryParse(string tag, out LanguageTag? result)`: Safe parsing with out parameter
- `ParseOrDefault(string tag, LanguageTag? defaultTag = null)`: Parse with fallback to "und"
- `ParseAndNormalize(string tag)`: Parse and normalize in one step
- `CreateBuilder()`: Create a fluent builder instance
- `FromLanguage(string language)`: Factory for simple language tags
- `FromLanguageRegion(string language, string region)`: Factory for language+region tags
- `FromLanguageScriptRegion(string language, string script, string region)`: Factory for full tags

**Properties:**
- `Language`: Primary language subtag (internal set)
- `ExtendedLanguage`: Extended language subtag (internal set)
- `Script`: Script subtag (internal set)
- `Region`: Region subtag (internal set)
- `Variants`: ImmutableArray of variant subtags
- `Extensions`: ImmutableArray of ExtensionTag objects
- `PrivateUse`: PrivateUseTag object
- `IsValid`: Property to check if tag is valid

**Instance Methods:**
- `Validate()`: Verify tag correctness
- `Normalize()`: Return normalized copy of tag
- `ToString()`: String representation
- `Equals()`: Equality comparison (case-insensitive)
- `GetHashCode()`: Hash code for collections
- Operators: `==`, `!=`

**Design Characteristics:**
- Implements `IEquatable<LanguageTag>`
- Constructors are internal, use factory methods or builder
- Properties use internal setters to maintain immutability for public API
- Collections exposed as ImmutableArray for thread safety

### LanguageTagBuilder Class (LanguageTagBuilder.cs)

Fluent builder for constructing language tags:

**Methods:**
- `Language(string value)`: Set primary language
- `ExtendedLanguage(string value)`: Set extended language
- `Script(string value)`: Set script
- `Region(string value)`: Set region
- `VariantAdd(string value)`: Add a variant
- `VariantAddRange(IEnumerable<string> values)`: Add multiple variants
- `ExtensionAdd(char prefix, IEnumerable<string> values)`: Add extension with prefix and values
- `PrivateUseAdd(string value)`: Add private use tag
- `PrivateUseAddRange(IEnumerable<string> values)`: Add multiple private use tags
- `Build()`: Return constructed tag (no validation)
- `Normalize()`: Return normalized tag (with validation)

### LanguageTagParser Class (LanguageTagParser.cs)

**Internal implementation** - Not exposed in public API. Use `LanguageTag.Parse()` instead.

- Parses language tags according to RFC 5646 Section 2.1
- Handles grandfathered tags and converts them to current forms
- Normalizes tag casing according to RFC conventions:
  - Language: lowercase
  - Extended language: lowercase
  - Script: Title case
  - Region: UPPERCASE
  - Variants: lowercase
  - Extensions: lowercase
  - Private use: lowercase

### LanguageLookup Class (LanguageLookup.cs)

Provides language code conversion and matching:

**Properties:**
- `Undetermined`: Constant for "und" (undetermined language)
- `Overrides`: User-defined (IETF, ISO) mapping pairs

**Methods:**
- `GetIetfFromIso(string languageTag)`: Convert ISO to IETF format
- `GetIsoFromIetf(string languageTag)`: Convert IETF to ISO format
- `IsMatch(string prefix, string languageTag)`: Prefix matching for content selection

### Data Models

#### Iso6392Data.cs
- ISO 639-2 language codes (3-letter bibliographic/terminologic codes)
- **Public Methods:**
  - `Create()`: Load embedded data
  - `LoadData(string fileName)`: Load from file
  - `LoadJson(string fileName)`: Load from JSON
  - `Find(string? languageTag, bool includeDescription)`: Find record by tag
- **Record Properties:** `Part2B`, `Part2T`, `Part1`, `RefName`

#### Iso6393Data.cs
- ISO 639-3 language codes (comprehensive language codes)
- **Public Methods:**
  - `Create()`: Load embedded data
  - `LoadData(string fileName)`: Load from file
  - `LoadJson(string fileName)`: Load from JSON
  - `Find(string? languageTag, bool includeDescription)`: Find record by tag
- **Record Properties:** `Id`, `Part2B`, `Part2T`, `Part1`, `Scope`, `LanguageType`, `RefName`, `Comment`

#### Rfc5646Data.cs
- RFC 5646 / BCP 47 language subtag registry
- **Public Methods:**
  - `Create()`: Load embedded data
  - `LoadData(string fileName)`: Load from file
  - `LoadJson(string fileName)`: Load from JSON
  - `Find(string? languageTag, bool includeDescription)`: Find record by tag
- **Properties:** `FileDate`, `RecordList`
- **Record Properties:** `Type`, `Tag`, `SubTag`, `Description` (ImmutableArray), `Added`, `SuppressScript`, `Scope`, `MacroLanguage`, `Deprecated`, `Comments` (ImmutableArray), `Prefix` (ImmutableArray), `PreferredValue`, `TagAny`
- **Enums:**
  - `RecordType`: None, Language, ExtLanguage, Script, Variant, Grandfathered, Region, Redundant
  - `RecordScope`: None, MacroLanguage, Collection, Special, PrivateUse

#### Supporting Classes

**ExtensionTag:**
- `Prefix`: Single-character extension prefix (char)
- `Tags`: ImmutableArray of extension values
- `ToString()`: Format as "prefix-tag1-tag2"

**PrivateUseTag:**
- `Prefix`: Constant 'x'
- `Tags`: ImmutableArray of private use values
- `ToString()`: Format as "x-tag1-tag2"

### Language Tag Structure

Per RFC 5646, language tags follow this format:
```
[Language]-[Extended language]-[Script]-[Region]-[Variant]-[Extension]-[Private Use]
```

Examples:
- `zh`: Simple language tag
- `zh-yue-hk`: Language with extended language and region
- `en-latn-gb-boont-r-extended-sequence-x-private`: Full tag with all components

## Development Guidelines

### Code Style

- **C# Version**: 14.0 (latest features)
- **Target Framework**: .NET 10.0
- Use modern C# features and syntax
- Follow .NET naming conventions
- Use collection expressions: `[]` instead of `new List<>()`
- Use `ImmutableArray` for public collections
- Use file-scoped namespaces
- **Required**: Include XML documentation (`///`) for ALL public APIs
- Use `init` accessors for immutable properties where appropriate
- Use `internal set` for properties that need internal mutability
- Use readonly fields where appropriate
- Prefer primary constructors where applicable

### XML Documentation Requirements

All public classes, methods, properties, enums, and operators **must** have XML documentation:

```csharp
/// <summary>
/// Brief description of the member.
/// </summary>
/// <param name="paramName">Description of parameter.</param>
/// <returns>Description of return value.</returns>
/// <exception cref="ExceptionType">When this exception is thrown.</exception>
public ReturnType MethodName(ParamType paramName)
```

### Immutability and Thread Safety

- All data classes (`Iso6392Data`, `Iso6393Data`, `Rfc5646Data`) are immutable
- Records can be safely shared across threads
- Use `ImmutableArray<T>` for collections in public APIs
- Properties expose immutable collections; internal backing stores can be mutable

### Testing Requirements

- **100% coverage** of all public APIs required
- Write unit tests for:
  - All public methods
  - All static factory methods
  - Property accessors
  - Equality members
  - Edge cases (null, empty, invalid inputs)
  - Case-insensitive behavior
  - Roundtrip scenarios (parse → normalize → toString)
- Tests are organized by component:
  - `LanguageTagTests.cs`: 77+ tests for LanguageTag class
  - `LanguageTagBuilderTests.cs`: Builder functionality
  - `LanguageTagParserTests.cs`: Parser and normalization
  - `LanguageLookupTests.cs`: Conversion and matching
  - `Iso6392Tests.cs`, `Iso6393Tests.cs`, `Rfc5646Tests.cs`: Data access
- Use descriptive test method names that explain the scenario
- Leverage AwesomeAssertions for fluent assertions
- Use `[Theory]` with `[InlineData]` for parameterized tests

### Tools and Formatting

Available VS Code tasks:
- `.Net Build`: Build the solution
- `.Net Format`: Format code using dotnet format
- `CSharpier Format`: Format code using CSharpier
- `.Net Tool Update`: Update all .NET tools
- `Husky.Net Run`: Run Husky pre-commit hooks

### Package Management

- Uses Microsoft.SourceLink.GitHub for source linking
- Generates symbols package (.snupkg) for debugging
- Embeds untracked sources for complete debugging experience
- Package ID: `ptr727.LanguageTags`
- License: MIT
- Current version: 1.0.0-pre

### Data Updates

- Language data is updated weekly via GitHub Actions workflow
- The `LanguageTagsCreate` tool downloads data from:
  - ISO 639-2: Library of Congress
  - ISO 639-3: SIL International
  - RFC 5646: IANA Language Subtag Registry
- Generated C# files (`*DataGen.cs`) are committed to the repository
- Data files are in `LanguageData/` directory

## API Design Patterns

### Factory Pattern
Use static factory methods instead of public constructors:
```csharp
// Good
LanguageTag tag = LanguageTag.Parse("en-US");
LanguageTag tag = LanguageTag.FromLanguage("en");

// Avoid - constructors are internal
// var tag = new LanguageTag(); // Not accessible
```

### Builder Pattern
Use fluent builder for complex tag construction:
```csharp
LanguageTag tag = LanguageTag.CreateBuilder()
    .Language("en")
    .Region("US")
    .Build();
```

### Immutability Pattern
- All properties are immutable after construction
- Use `Normalize()` to get modified copies
- Collections are exposed as `ImmutableArray<T>`

### Safe Parsing
Always use safe parsing patterns:
```csharp
// TryParse pattern
if (LanguageTag.TryParse(input, out LanguageTag? tag))
{
    // Use tag
}

// ParseOrDefault pattern
LanguageTag tag = LanguageTag.ParseOrDefault(input); // Falls back to "und"
```

## References and Standards

- **RFC 5646**: Tags for Identifying Languages
- **BCP 47**: Best Current Practice for Language Tags
- **ISO 639-2**: 3-letter language codes
- **ISO 639-3**: Comprehensive language codes
- **ISO 15924**: Script codes
- **ISO 3166-1**: Country codes
- **UN M.49**: Geographic region codes
- **IANA Language Subtag Registry**: Authoritative registry of subtags

## Important Implementation Notes

- The implemented language tag parsing and normalization logic may be incomplete or inaccurate
- Grandfathered tags are automatically converted to their preferred values during parsing
- All tag comparisons are case-insensitive per RFC 5646
- Private use tags start with 'x-' prefix
- Extensions use single-character prefixes (except 'x' which is reserved for private use)
- `LanguageTagParser` is internal; all parsing is done through `LanguageTag` static methods

## Recent API Changes

### Changed (Breaking)
- `LanguageTagParser` is now internal (use `LanguageTag.Parse()` instead)
- Properties changed from `IList<string>` to `ImmutableArray<string>`:
  - `VariantList` → `Variants`
  - `ExtensionList` → `Extensions`
  - `TagList` → `Tags`
- `LoadData()` and `LoadJson()` changed from internal to public in data classes
- Tag construction requires use of factory methods or builder (constructors are internal)

### Added (Non-Breaking)
- `LanguageTag.ParseOrDefault()`: Safe parsing with fallback
- `LanguageTag.ParseAndNormalize()`: Combined parse and normalize
- `LanguageTag.IsValid`: Property for validation
- `LanguageTag.FromLanguage()`, `FromLanguageRegion()`, `FromLanguageScriptRegion()`: Factory methods
- `IEquatable<LanguageTag>` implementation with operators
- Comprehensive XML documentation for all public APIs

## Future Improvements

Consider these areas for enhancement:
- Use a BNF parser or parser generator (ANTLR4, Eto.Parse, etc.) instead of hand-parsing
- Implement comprehensive subtag content validation against registry data
- Add more language lookup and validation features
- Improve error messages and diagnostics
- Consider making `ExtensionTag` and `PrivateUseTag` immutable records

## Contributing

When contributing to this project:
1. Follow the existing code style and patterns
2. Add unit tests for ALL new public functionality (100% coverage required)
3. Add XML documentation for ALL public APIs
4. Run formatting tools before committing
5. Ensure all tests pass (211+ tests should pass)
6. Update the README if adding significant features
7. Do not expose constructors publicly - use factory methods or builder pattern
8. Prefer immutability - use `ImmutableArray` for collections
9. Follow the safe parsing patterns (TryParse, ParseOrDefault)
10. Maintain thread safety for all data structures

## Common Patterns

### Creating Tags
```csharp
// Simple parsing
LanguageTag? tag = LanguageTag.Parse("en-US");

// Safe parsing
if (LanguageTag.TryParse("en-US", out LanguageTag? tag))
{
    Console.WriteLine(tag.ToString());
}

// Parse with default
LanguageTag tag = LanguageTag.ParseOrDefault(input); // "und" if invalid

// Factory methods
LanguageTag tag = LanguageTag.FromLanguage("en");
LanguageTag tag = LanguageTag.FromLanguageRegion("en", "US");

// Builder
LanguageTag tag = LanguageTag.CreateBuilder()
    .Language("en")
    .Region("US")
    .Build();
```

### Normalizing Tags
```csharp
// Parse and normalize separately
LanguageTag? tag = LanguageTag.Parse("en-latn-us");
LanguageTag? normalized = tag?.Normalize(); // "en-US"

// Parse and normalize in one step
LanguageTag? tag = LanguageTag.ParseAndNormalize("en-latn-us"); // "en-US"
```

### Accessing Tag Components
```csharp
LanguageTag tag = LanguageTag.Parse("en-latn-gb-boont-r-extended-x-private")!;

string language = tag.Language; // "en"
string script = tag.Script; // "latn"
string region = tag.Region; // "gb"
ImmutableArray<string> variants = tag.Variants; // ["boont"]
ImmutableArray<ExtensionTag> extensions = tag.Extensions; // [{ Prefix='r', Tags=["extended"] }]
PrivateUseTag privateUse = tag.PrivateUse; // { Tags=["private"] }
```

### Comparing Tags
```csharp
LanguageTag? tag1 = LanguageTag.Parse("en-US");
LanguageTag? tag2 = LanguageTag.Parse("en-us");

bool equal = tag1 == tag2; // true (case-insensitive)
bool equal = tag1.Equals(tag2); // true
int hash = tag1.GetHashCode(); // Same as tag2.GetHashCode()
