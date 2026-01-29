# Instructions for AI Coding Agents

**LanguageTags** is a C# .NET library for handling ISO 639-2, ISO 639-3, and RFC 5646 / BCP 47 language tags.

The project serves two primary purposes:

1. **Data Publishing**: Provides ISO 639-2, ISO 639-3, and RFC 5646 language tag records in JSON and C# formats
2. **Tag Processing**: Implements IETF BCP 47 language tag construction and parsing per RFC 5646 semantic rules

For comprehensive coding standards and detailed conventions, refer to [`.github/copilot-instructions.md`](./.github/copilot-instructions.md) and [`CODESTYLE.md`](./CODESTYLE.md).

## Solution Structure

### Projects

- **LanguageTags** (`LanguageTags/LanguageTags.csproj`)
  - Core library project
  - NuGet package: `ptr727.LanguageTags`
  - Target framework: .NET 10.0
  - AOT compatible (`<IsAotCompatible>true</IsAotCompatible>`)

- **LanguageTagsCreate** (`LanguageTagsCreate/LanguageTagsCreate.csproj`)
  - CLI utility for downloading and generating language data
  - Downloads from official sources (Library of Congress, SIL, IANA)
  - Converts to JSON and generates C# code files

- **LanguageTagsTests** (`LanguageTagsTests/LanguageTagsTests.csproj`)
  - xUnit test suite with comprehensive test coverage
  - Uses AwesomeAssertions for test assertions

### Key Components

**Public API Classes:**

- `LanguageTag`: Main class for working with language tags (parse, build, normalize, validate)
- `LanguageTagBuilder`: Fluent builder for constructing language tags
- `LanguageLookup`: Language code conversion and matching (IETF ↔ ISO)
- `Iso6392Data`: ISO 639-2 language code data
- `Iso6393Data`: ISO 639-3 language code data
- `Rfc5646Data`: RFC 5646 / BCP 47 language subtag registry data
- `ExtensionTag`: Represents extension subtags
- `PrivateUseTag`: Represents private use subtags

**Internal Classes:**

- `LanguageTagParser`: Internal parser (use `LanguageTag.Parse()` instead)

## Authoritative References

For detailed specifications, see:

- [`.github/copilot-instructions.md`](./.github/copilot-instructions.md) - Complete coding conventions and style guide
- [`CODESTYLE.md`](./CODESTYLE.md) - Code style and formatting rules
- [`.editorconfig`](./.editorconfig) - Automated style enforcement
- Project task definitions - `CSharpier Format`, `.Net Build`, `.Net Format`, `Husky.Net Run`
