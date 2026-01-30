# LanguageTags

C# .NET library for ISO 639-2, ISO 639-3, RFC 5646 / BCP 47 language tags.

## Build and Distribution

- **Source Code**: [GitHub][github-link] - Source code, issues, discussions, and CI/CD pipelines.
- **Versioned Releases**: [GitHub Releases][releases-link] - Version tagged source code and build artifacts.
- **NuGet Packages** [NuGet Packages][nuget-link] - .NET libraries published to NuGet.org.

### Build Status

[![Release Status][releasebuildstatus-shield]][actions-link]\
[![Last Commit][lastcommit-shield]][commits-link]\
[![Last Build][lastbuild-shield]][actions-link]

### Releases

[![GitHub Release][releaseversion-shield]][releases-link]\
[![GitHub Pre-Release][prereleaseversion-shield]][releases-link]\
[![NuGet Release][nugetreleaseversion-shield]][nuget-link]\
[![NuGet Pre-Release][nugetprereleaseversion-shield]][nuget-link]

### Release Notes

**Version: 1.2**:

**Summary**:

- Refactored the project to follow standard patterns across other projects.
- IO APIs are now async-only (`LoadDataAsync`, `LoadJsonAsync`, `SaveJsonAsync`, `GenCodeAsync`).
- Added logging support for `ILogger` or `ILoggerFactory` per class instance or statically.

See [Release History](./HISTORY.md) for complete release notes and older versions.

## Getting Started

Get started with LanguageTags in two easy steps:

1. **Add LanguageTags to your project**:

    ```shell
    # Add the package to your project
    dotnet add package ptr727.LanguageTags
    ```

2. **Write some code**:

   ```csharp
   LanguageLookup languageLookup = new();
   string iso = languageLookup.GetIsoFromIetf("af"); // "afr"
   iso = languageLookup.GetIsoFromIetf("zh-cmn-Hant"); // "chi"
   iso = languageLookup.GetIsoFromIetf("cmn-Hant"); // "chi"
   ```

   ```csharp
   LanguageTag languageTag = LanguageTag.CreateBuilder()
       .Language("en")
       .Script("latn")
       .Region("gb")
       .VariantAdd("boont")
       .ExtensionAdd('r', ["extended", "sequence"])
       .PrivateUseAdd("private")
       .Build();
   string tag = languageTag.ToString(); // "en-latn-gb-boont-r-extended-sequence-x-private"
   ```

See [Usage](#usage) for detailed usage instructions.

## Table of Contents

- [LanguageTags](#languagetags)
  - [Build and Distribution](#build-and-distribution)
    - [Build Status](#build-status)
    - [Releases](#releases)
    - [Release Notes](#release-notes)
  - [Getting Started](#getting-started)
  - [Table of Contents](#table-of-contents)
  - [Use Cases](#use-cases)
  - [Usage](#usage)
    - [Tag Lookup](#tag-lookup)
    - [Tag Conversion](#tag-conversion)
    - [Tag Matching](#tag-matching)
    - [Tag Builder](#tag-builder)
    - [Tag Parser](#tag-parser)
    - [Tag Normalization](#tag-normalization)
    - [Tag Validation](#tag-validation)
  - [Installation](#installation)
  - [Questions or Issues](#questions-or-issues)
  - [Build Artifacts](#build-artifacts)
  - [Tag Theory](#tag-theory)
    - [Terminology](#terminology)
    - [Format](#format)
    - [References](#references)
    - [Libraries](#libraries)
  - [3rd Party Tools](#3rd-party-tools)
  - [License](#license)

## Use Cases

> **ℹ️ TL;DR**:
>
> - Catalog of ISO 639-2, ISO 639-3, RFC 5646 language tags in JSON and C# record format.
> - Code for IETF BCP 47 language tag construction and parsing per the RFC 5646 semantic rules.
>
> **⚠️ Note**: The implemented language tag parsing and normalization logic may be incomplete or inaccurate.
>
> - Verify the results for your specific usage.
> - Refer to [Libraries](#libraries) for other known implementations.
> - Refer to [References](#references) for specification details.

## Usage

> **ℹ️ Note**: Refer to the [Tag Theory](#tag-theory) section for an overview of terms and theory of operation.

### Tag Lookup

Tag records can be constructed by calling `Create()`, or loaded from data `LoadDataAsync()`, or loaded from JSON `LoadJsonAsync()`.\
The records and record collections are immutable and can safely be reused and shared across threads.

Each class implements a `Find(string languageTag, bool includeDescription)` method that will search all tags in all records for a matching tag.\
This is mostly a convenience function, and specific use cases should use specific tags.

```csharp
Iso6392Data iso6392 = Iso6392Data.Create();
Iso6392Record? record = iso6392.Find("afr", false);
// record.Part2B = "afr"
// record.RefName = "Afrikaans"
record = iso6392.Find("zulu", true);
// record.Part2B = "zul"
// record.RefName = "Zulu"
```

```csharp
Iso6393Data iso6393 = await Iso6393Data.LoadDataAsync("iso6393");
Iso6393Record? record = iso6393.Find("zh", false);
// record.Id = "zho"
// record.Part1 = "zh"
// record.RefName = "Chinese"
record = iso6393.Find("yue chinese", true);
// record.Id = "yue"
// record.RefName = "Yue Chinese"
```

```csharp
Rfc5646Data rfc5646 = await Rfc5646Data.LoadJsonAsync("rfc5646.json");
Rfc5646Record? record = rfc5646.Find("de", false);
// record.SubTag = "de"
// record.Description[0] = "German"
record = rfc5646.Find("zh-cmn-Hant", false);
// record.Tag = "zh-cmn-Hant"
// record.Description[0] = "Mandarin Chinese (Traditional)"
record = rfc5646.Find("Inuktitut in Canadian", true);
// record.Tag = "iu-Cans"
// record.Description[0] = "Inuktitut in Canadian Aboriginal Syllabic script"
```

### Tag Conversion

Tags can be converted between ISO 639 and IETF forms using `GetIetfFromIso()` and `GetIsoFromIetf()`.\
Tag lookup will use the user defined `Overrides` map, or the tag record lists, or the local system `CultureInfo`.\
If a match is not found the undetermined `und` tag will be returned.

```csharp
LanguageLookup languageLookup = new();
string ietf = languageLookup.GetIetfFromIso("afr"); // "af"
ietf = languageLookup.GetIetfFromIso("zho"); // "zh"
```

```csharp
LanguageLookup languageLookup = new();
string iso = languageLookup.GetIsoFromIetf("af"); // "afr"
iso = languageLookup.GetIsoFromIetf("zh-cmn-Hant"); // "chi"
iso = languageLookup.GetIsoFromIetf("cmn-Hant"); // "chi"
```

### Tag Matching

Tag matching can be used to select content based on preferred vs. available languages.

> **ℹ️ Examples**:
>
> - HTTP [`Accept-Language`][acceptlanguage-link] and [`Content-Language`](https://www.rfc-editor.org/rfc/rfc9110.html#name-content-language).
> - Matroska media stream [`LanguageIETF Element`][matroskalanguage-link].

IETF language tags are in the form of:

> [Language]-[Extended language]-[Script]-[Region]-[Variant]-[Extension]-[Private Use]

Sub-tag matching happens left to right until a match is found.

Examples:

- `pt` will match `pt` Portuguese, or `pt-BR` Brazilian Portuguese, or `pt-PT` European Portuguese.
- `pt-BR` will only match `pt-BR` Brazilian Portuguese\
- `zh` will match `zh` Chinese, or `zh-Hans` simplified Chinese, or `zh-Hant` for traditional Chinese, and other variants.
- `zh-Hans` will only match `zh-Hans` simplified Chinese.

```csharp
LanguageLookup languageLookup = new();
bool match = languageLookup.IsMatch("en", "en-US"); // true
match = languageLookup.IsMatch("zh", "zh-cmn-Hant"); // true
match = languageLookup.IsMatch("sr-Latn", "sr-Latn-RS"); // true
match = languageLookup.IsMatch("zha", "zh-Hans"); // false
match = languageLookup.IsMatch("zh-Hant", "zh-Hans"); // false
```

### Tag Builder

The `LanguageTagBuilder` class supports fluent builder style tag construction, and will return a constructed `LanguageTag` class through the final `Build()` or `Normalize()` methods.

The `Build()` method will construct the tag, but will not perform any correctness validation or normalization.\
Use the `Validate()` method to test for shape correctness. See [Tag Validation](#tag-validation) for details.

The `Normalize()` method will build the tag and perform validation and normalization.\
See [Tag Normalization](#tag-normalization) for details.

```csharp
LanguageTag languageTag = LanguageTag.CreateBuilder()
    .Language("en")
    .Script("latn")
    .Region("gb")
    .VariantAdd("boont")
    .ExtensionAdd('r', ["extended", "sequence"])
    .PrivateUseAdd("private")
    .Build();
string tag = languageTag.ToString(); // "en-latn-gb-boont-r-extended-sequence-x-private"
```

```csharp
LanguageTag languageTag = LanguageTag.CreateBuilder()
    .PrivateUseAddRange(["private", "use"])
    .Build();
string tag = languageTag.ToString(); // "x-private-use"
```

```csharp
LanguageTag? languageTag = LanguageTag.CreateBuilder()
    .Language("ar")
    .ExtendedLanguage("arb")
    .Script("latn")
    .Region("de")
    .VariantAdd("nedis")
    .VariantAdd("foobar")
    .Normalize();
string tag = languageTag?.ToString(); // "arb-Latn-DE-foobar-nedis"
```

### Tag Parser

The `LanguageTag` class static `Parse()` method will parse the text form language tag and return a constructed `LanguageTag` object, or `null` in case of a parsing failure.

Parsing will validate all subtags for correctness in type, length, and position, but not value, and case will not be modified.

Grandfathered tags will be converted to their current preferred form and parsed as such.\
E.g. `en-gb-oed` -> `en-GB-oxendict`, `i-klingon` -> `tlh`.

The `Normalize()` method will parse the text tag, and perform validation and normalization.\
See [Tag Normalization](#tag-normalization) for details.

```csharp
LanguageTag? languageTag = LanguageTag.Parse("en-latn-gb-boont-r-extended-sequence-x-private");
// languageTag.Language = "en"
// languageTag.Script = "latn"
// languageTag.Region = "gb"
// languageTag.Variants[0] = "boont"
// languageTag.Extensions[0].Prefix = 'r'
// languageTag.Extensions[0].Tags[0] = "extended"
// languageTag.Extensions[0].Tags[1] = "sequence"
// languageTag.PrivateUse.Tags[0] = "private"
string tag = languageTag?.ToString(); // "en-latn-gb-boont-r-extended-sequence-x-private"
```

```csharp
LanguageTag? languageTag = LanguageTag.Parse("en-gb-oed"); // Grandfathered
// languageTag.Language = "en"
// languageTag.Region = "GB"
// languageTag.Variants[0] = "oxendict"
string tag = languageTag?.ToString(); // "en-GB-oxendict"
```

### Tag Normalization

The `Normalize()` method will convert tags to their canonical form.\
See [RFC 5646 Section 4.5][rfc5646section45-link] for details.

Normalization includes the following:

- Replace the language subtag with their preferred values.
  - E.g. `iw` -> `he`, `in` -> `id`
- Replace extended language subtags with their preferred language subtag values.
  - E.g. `ar-afb` -> `afb`, `zh-yue` -> `yue`
- Remove or replace redundant subtags their preferred values.
  - E.g. `zh-cmn-Hant` -> `cmn-Hant`, `zh-gan` -> `gan`, `sgn-CO` -> `csn`
- Remove redundant script subtags.
  - E.g. `af-Latn` -> `af`, `en-Latn` -> `en`
- Normalize case.
  - All subtags lowercase.
  - Script title case, e.g. `Latn`.
  - Region uppercase, e.g. `GB`.
- Sort sub tags.
  - Sort variant subtags by value.
  - Sort extension subtags by prefix and subtag values.
  - Sort private use subtags by value.

```csharp
LanguageTag? languageTag = LanguageTag.CreateBuilder()
    .Language("en")
    .ExtensionAdd('b', ["ccc"]) // Add b before a to force a sort
    .ExtensionAdd('a', ["bbb", "aaa"]) // Add bbb before aaa to force a sort
    .PrivateUseAddRange(["ccc", "a"]) // Add ccc before a to force a sort
    .Normalize();
string tag = languageTag?.ToString(); // "en-a-aaa-bbb-b-ccc-x-a-ccc"
```

```csharp
LanguageTag? languageTag = LanguageTag.ParseAndNormalize("en-latn-gb-boont-r-sequence-extended-x-private");
string tag = languageTag?.ToString(); // "en-GB-boont-r-extended-sequence-x-private"
```

```csharp
LanguageTag? languageTag = LanguageTag.Parse("ar-arb-latn-de-nedis-foobar");
string tag = languageTag?.ToString(); // "ar-arb-latn-de-nedis-foobar"

LanguageTag? normalizedTag = languageTag?.Normalize();
string normalizedString = normalizedTag?.ToString(); // "arb-Latn-DE-foobar-nedis"
```

### Tag Validation

The `Validate()` method will verify subtags for correctness.\
See [RFC 5646 Section 2.1][rfc5646section21-link] and [RFC 5646 Section 2.2.9][rfc5646section229-link] for details.

Note that `LanguageTag` objects created by `Parse()` or `Normalize()` are already verified for form correctness during parsing, and `Validate()` is primarily of use when using the `LanguageTagBuilder.Build()` method directly.

Validation includes the following:

- Subtag shape correctness, see [Format](#format) for a summary.
- No duplicate variants, extension prefixes, extension tags, or private tags.
- No missing subtags.

```csharp
LanguageTag languageTag = LanguageTag.CreateBuilder()
    .Language("en")
    .Region("US")
    .Build();
bool isValid = languageTag.Validate(); // true
// Or use the IsValid property
isValid = languageTag.IsValid; // true
```

## Installation

**Project integration**:

```shell
# Add the package to your project
dotnet add package ptr727.LanguageTags
```

```csharp
// Include the namespace
using ptr727.LanguageTags;
```

**Debug log configuration**:

```csharp
// Configure global logging (static fallback)
using Microsoft.Extensions.Logging;
using ptr727.LanguageTags;
using Serilog;
using Serilog.Extensions.Logging;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Debug()
    .CreateLogger();

ILoggerFactory loggerFactory = new SerilogLoggerFactory(Log.Logger, dispose: true);
LogOptions.SetFactory(loggerFactory);
```

```csharp
// Configure per-call logging (instance logger or factory)
using Microsoft.Extensions.Logging;
using ptr727.LanguageTags;
using Serilog;
using Serilog.Extensions.Logging;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Debug()
    .CreateLogger();

ILoggerFactory loggerFactory = new SerilogLoggerFactory(Log.Logger, dispose: true);
Options options = new() { LoggerFactory = loggerFactory };

LanguageTag? tag = LanguageTag.Parse("en-US", options);
LanguageLookup lookup = new(options);
```

## Questions or Issues

**Tag testing**:

- The [BCP47 language subtag lookup][r12asubtags-link] site offers convenient tag parsing and validation capabilities.
- Refer to the [unit tests](./LanguageTagsTests) for examples, do note that tests may pass but not be complete or accurate per the RFC spec.

**General questions**:

- Use the [Discussions][discussions-link] forum for general questions.

**Bug reports**:

- Ask in the [Discussions][discussions-link] forum if you are not sure if it is a bug.
- Check the existing [Issues][issues-link] tracker for known problems.
- If the issue is unique and a bug, file it in [Issues][issues-link], and include all pertinent steps to reproduce the issue.

## Build Artifacts

**Build process and artifacts**:

- **[`LanguageTagsCreate`](./LanguageTagsCreate) project**:
  - Downloads language tag data files.
  - Converts the tag data into JSON files.
  - Generates C# records of the tags.
- **[`LanguageData`](./LanguageData/) directory**:
  - ISO 639-2: [Source](https://www.loc.gov/standards/iso639-2/ISO-639-2_utf-8.txt), [Data](./LanguageData/iso6392), [JSON](./LanguageData/iso6392.json), [Code](./LanguageTags/Iso6392DataGen.cs)
  - ISO 639-3: [Source](https://iso639-3.sil.org/sites/iso639-3/files/downloads/iso-639-3.tab), [Data](./LanguageData/iso6393), [JSON](./LanguageData/iso6393.json), [Code](./LanguageTags/Iso6393DataGen.cs)
  - RFC 5646 : [Source](https://www.iana.org/assignments/language-subtag-registry/language-subtag-registry), [Data](./LanguageData/rfc5646), [JSON](./LanguageData/rfc5646.json), [Code](./LanguageTags/rfc5646DataGen.cs)
- A weekly [GitHub Actions](./.github/workflows/run-periodic-codegen-pull-request.yml) job keeps the data files up to date and automatically publishes new releases.

## Tag Theory

> **ℹ️ Note**: Refer to [References](#references) for complete specification details.

### Terminology

**Brief overview of tag terms**:

- An IETF BCP 47 language tag is a standardized code that is used to identify human languages on the Internet.
- The tag structure is standardized by the Internet Engineering Task Force (IETF) in Best Current Practice (BCP) 47.
- RFC 5646 defines the BCP 47 language tag syntax and semantic rules.
- The subtags are maintained by Internet Assigned Numbers Authority (IANA) Language Subtag Registry.
- ISO 639 is a standard for classifying languages and language groups, and is maintained by the International Organization for Standardization (ISO).
- RFC 5646 incorporates ISO 639, ISO 15924, ISO 3166, and UN M.49 codes as the foundation for its language tags.

### Format

> **ℹ️ TL;DR**: IETF language tags are constructed from sub-tags with specific rules.
>
> **ℹ️ Note**: Refer to [RFC 5646 Section 2.1][rfc5646section21-link] for complete language tag syntax and rules.

**Normal tags**:

> [Language]-[Extended language]-[Script]-[Region]-[Variant]-[Extension]-[Private Use]

- Language:
  - 2 - 3 alpha: Shortest ISO 639 code
  - 4 alpha: Future use
  - 5 - 8 alpha: Registered tag
  - See [RFC 5646 Section 2.2.1][rfc5646section221-link]
- Extended language:
  - 3 alpha: Reserved ISO 639 code
  - See [RFC 5646 Section 2.2.2][rfc5646section222-link]
- Script:
  - 4 alpha: [ISO 15924][iso15924-link] code
  - See [RFC 5646 Section 2.2.3][rfc5646section223-link]
- Region:
  - 2 alpha: [ISO 3166-1][iso31661-link] code
  - 3 digit: [UN M.49][unm49-link] code
  - See [RFC 5646 Section 2.2.4][rfc5646section224-link]
- Variant:
  - 5 - 8 alphanumeric starting with letter: Registered tag
  - 4 - 8 alphanumeric starting with digit: Registered tag
  - See [RFC 5646 Section 2.2.5][rfc5646section225-link]
- Extension: (`[singleton]-[extension]`)
  - 1 alphanumeric: Singleton
  - 2 - 8 alphanumeric: Extension
  - See [RFC 5646 Section 2.2.6][rfc5646section226-link]

**Private use tags**:

> x-[private]

- `x`: Singleton
- 1 - 8 alphanumeric: Private use
- See [RFC 5646 Section 2.2.7][rfc5646section227-link]

**Grandfathered tags**:

  > [grandfathered]

- Grandfathered tags are converted to current form tags.
- E.g. `en-gb-oed` -> `en-GB-oxendict`
- E.g. `i-klingon` -> `tlh`.
- See [RFC 5646 Section 2.2.8][rfc5646section228-link]

**Examples**:

- `zh` : `[Language]`
- `zh-yue` : `[Language]-[Extended language]`
- `zh-yue-hk`: `[Language]-[Extended language]-[Region]`
- `hy-latn-it-arevela`: `[Language]-[Script]-[Region]-[Variant]`
- `en-a-bbb-x-a-ccc` : `[Language]-[Extension]-[Private Use]`
- `en-latn-gb-boont-r-extended-sequence-x-private` : `[Language]-[Script]-[Region]-[Variant]-[Extension]-[Private Use]`

### References

**References and documentation**:

- [Wikipedia : Codes for constructed languages][wikipediacodes-link]
- [Wikipedia : IETF language tag][ietflanguagetag-link]
- [W3C : Choosing a Language Tag][w3cchoosingtag-link]
- [W3C : Language tags in HTML and XML][w3ctags-link]
- [W3C : BCP47 language subtag lookup][r12asubtags-link]
- [IANA : Language Subtags, Tag Extensions, and Tags][ianatags-link]
- [RFC : BCP47][bcp47-link]
- [RFC : 4647 : Matching of Language Tags][rfc4647-link]
- [RFC : 5646 : Tags for Identifying Languages][rfc5646-link]
- [Unicode Consortium : Unicode Common Locale Data Repository (CLDR) Project][cldr-link]
- [Library of Congress : ISO 639-2 Language Coding Agency][iso6392-link]
- [SIL International : ISO 639-3 Language Coding Agency][iso6393-link]

### Libraries

**Other known language tag libraries**:

- [github.com/rspeer/langcodes][rspeerlangcodes-link]
- [github.com/oxigraph/oxilangtag][oxigraphoxilangtag-link]
- [github.com/pyfisch/rust-language-tags/][pyfischrustlanguagetags-link]
- [github.com/DanSmith/languagetags-sharp][dansmithlanguagetagssharp-link]
- [github.com/jkporter/bcp47][jkporterbcp47-link]
- [github.com/mattcg/language-subtag-registry][mattcglanguagesubtagregistry-link]

## 3rd Party Tools

**3rd party tools used in this project**:

- [AwesomeAssertions][awesomeassertions-link]
- [Bring Your Own Badge][byob-link]
- [Create Pull Request][createpr-link]
- [CSharpier][csharpier-link]
- [GH Release][ghrelease-link]
- [Git Auto Commit][ghautocommit-link]
- [GitHub Actions][ghactions-link]
- [GitHub Dependabot][ghdependabot-link]
- [Husky.Net][huskynet-link]
- [Nerdbank.GitVersioning][nerbankgitversion-link]
- [Serilog][serilog-link]
- [xUnit.Net][xunit-link]

## License

Licensed under the [MIT License][license-link]\
![GitHub License][license-shield]

<!--- Shields links --->

[github-link]: https://github.com/ptr727/LanguageTags
[actions-link]: https://github.com/ptr727/LanguageTags/actions
[discussions-link]: https://github.com/ptr727/LanguageTags/discussions
[commits-link]: https://github.com/ptr727/LanguageTags/commits/main
[issues-link]: https://github.com/ptr727/LanguageTags/issues
[releases-link]: https://github.com/ptr727/LanguageTags/releases

[license-link]: ./LICENSE
[license-shield]: https://img.shields.io/github/license/ptr727/LanguageTags?label=License

[lastbuild-shield]: https://byob.yarr.is/ptr727/LanguageTags/lastbuild
[lastcommit-shield]: https://img.shields.io/github/last-commit/ptr727/LanguageTags?logo=github&label=Last%20Commit

[releaseversion-shield]: https://img.shields.io/github/v/release/ptr727/LanguageTags?logo=github&label=GitHub%20Release
[prereleaseversion-shield]: https://img.shields.io/github/v/release/ptr727/LanguageTags?include_prereleases&label=GitHub%20Pre-Release&logo=github
[releasebuildstatus-shield]: https://img.shields.io/github/actions/workflow/status/ptr727/LanguageTags/publish-release.yml?logo=github&label=Releases%20Build

[nuget-link]: https://www.nuget.org/packages/ptr727.LanguageTags/
[nugetreleaseversion-shield]: https://img.shields.io/nuget/v/ptr727.LanguageTags?logo=nuget&label=NuGet%20Release
[nugetprereleaseversion-shield]: https://img.shields.io/nuget/vpre/ptr727.LanguageTags?logo=nuget&&label=NuGet%20Pre-Release&color=orange

<!-- 3rd Party tool links -->

[awesomeassertions-link]: https://awesomeassertions.org/
[byob-link]: https://github.com/marketplace/actions/bring-your-own-badge
[createpr-link]: https://github.com/marketplace/actions/create-pull-request
[csharpier-link]: https://csharpier.com/
[ghactions-link]: https://github.com/actions
[ghautocommit-link]: https://github.com/marketplace/actions/git-auto-commit
[ghdependabot-link]: https://github.com/dependabot
[ghrelease-link]: https://github.com/marketplace/actions/gh-release
[huskynet-link]: https://alirezanet.github.io/Husky.Net/
[nerbankgitversion-link]: https://github.com/marketplace/actions/nerdbank-gitversioning
[serilog-link]: https://serilog.net/
[xunit-link]: https://xunit.net/

<!-- Other links -->

[rfc5646section21-link]: https://www.rfc-editor.org/rfc/rfc5646#section-2.1
[rfc5646section221-link]: https://www.rfc-editor.org/rfc/rfc5646#section-2.2.1
[rfc5646section222-link]: https://www.rfc-editor.org/rfc/rfc5646#section-2.2.2
[rfc5646section223-link]: https://www.rfc-editor.org/rfc/rfc5646#section-2.2.3
[iso15924-link]: https://unicode.org/iso15924/iso15924-codes.html
[rfc5646section224-link]: https://www.rfc-editor.org/rfc/rfc5646#section-2.2.4
[iso31661-link]: https://en.wikipedia.org/wiki/ISO_3166-1
[unm49-link]: https://unstats.un.org/unsd/methodology/m49/
[rfc5646section225-link]: https://www.rfc-editor.org/rfc/rfc5646#section-2.2.5
[rfc5646section226-link]: https://www.rfc-editor.org/rfc/rfc5646#section-2.2.6
[rfc5646section227-link]: https://www.rfc-editor.org/rfc/rfc5646#section-2.2.7
[rfc5646section228-link]: https://www.rfc-editor.org/rfc/rfc5646#section-2.2.8
[r12asubtags-link]: https://r12a.github.io/app-subtags/
[wikipediacodes-link]: https://en.wikipedia.org/wiki/Codes_for_constructed_languages
[ietflanguagetag-link]: https://en.wikipedia.org/wiki/IETF_language_tag
[w3cchoosingtag-link]: https://www.w3.org/International/questions/qa-choosing-language-tags
[w3ctags-link]: https://www.w3.org/International/articles/language-tags/
[ianatags-link]: https://www.iana.org/assignments/language-subtags-tags-extensions/language-subtags-tags-extensions.xhtml
[rfc4647-link]: https://www.rfc-editor.org/info/rfc4647
[rfc5646-link]: https://www.rfc-editor.org/info/rfc5646
[iso6392-link]: https://www.loc.gov/standards/iso639-2/
[cldr-link]: https://cldr.unicode.org/
[iso6393-link]: https://iso639-3.sil.org/
[bcp47-link]: https://www.rfc-editor.org/info/bcp47
[rspeerlangcodes-link]: https://github.com/rspeer/langcodes
[oxigraphoxilangtag-link]: https://github.com/oxigraph/oxilangtag
[pyfischrustlanguagetags-link]: https://github.com/pyfisch/rust-language-tags/
[dansmithlanguagetagssharp-link]: https://github.com/DanSmith/languagetags-sharp
[jkporterbcp47-link]: https://github.com/jkporter/bcp47
[mattcglanguagesubtagregistry-link]: https://github.com/mattcg/language-subtag-registry
[rfc5646section229-link]: https://www.rfc-editor.org/rfc/rfc5646#section-2.2.9
[acceptlanguage-link]: https://www.rfc-editor.org/rfc/rfc9110.html#name-accept-language
[matroskalanguage-link]: https://datatracker.ietf.org/doc/html/draft-ietf-cellar-matroska-07#name-language-codes
[rfc5646section45-link]: https://www.rfc-editor.org/rfc/rfc5646#section-4.5
