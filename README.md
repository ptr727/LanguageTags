# LanguageTags

C# .NET library for ISO 639-2, ISO 639-3, RFC 5646 / BCP 47 language tags.

## Build Status

Code and Pipeline is on [GitHub](https://github.com/ptr727/LanguageTags)\
![GitHub Last Commit](https://img.shields.io/github/last-commit/ptr727/LanguageTags?logo=github)\
![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/ptr727/LanguageTags/publish-release.yml?logo=github)

## NuGet Package

Packages published on [NuGet](https://www.nuget.org/packages/ptr727.LanguageTags/)\
![NuGet](https://img.shields.io/nuget/v/ptr727.LanguageTags?logo=nuget)

## Version History

- v1.1:
  - AOT support.
  - Refactored public surfaces to minimize internals exposure.
- v1.0:
  - Initial standalone release.

## Introduction

This project serves two primary purposes:

- Publishing ISO 639-2, ISO 639-3, RFC 5646 language tag records in JSON and C# format.
- Code for IETF BCP 47 language tag construction and parsing per the RFC 5646 semantic rules.

Terminology clarification:

- An IETF BCP 47 language tag is a standardized code that is used to identify human languages on the Internet.
- The tag structure is standardized by the Internet Engineering Task Force (IETF) in Best Current Practice (BCP) 47.
- RFC 5646 defines the BCP 47 language tag syntax and semantic rules.
- The subtags are maintained by Internet Assigned Numbers Authority (IANA) Language Subtag Registry.
- ISO 639 is a standard for classifying languages and language groups, and is maintained by the International Organization for Standardization (ISO).
- RFC 5646 incorporates ISO 639, ISO 15924, ISO 3166, and UN M.49 codes as the foundation for its language tags.

Note that the implemented language tag parsing and normalization logic may be incomplete or inaccurate.

Refer to [Language Tag Libraries](#language-tag-libraries) for other known implementations.\
Refer to [References](#references) for specification details.

## Build Artifacts

The build [tool](./LanguageTagsCreate) downloads language tag data files, converts them into JSON files for easy consumption, and generates C# classes with all the tags for direct use in code.

- ISO 639-2: [Source](https://www.loc.gov/standards/iso639-2/ISO-639-2_utf-8.txt), [Data](./LanguageData/iso6392), [JSON](./LanguageData/iso6392.json), [Code](./LanguageTags/Iso6392DataGen.cs)
- ISO 639-3: [Source](https://iso639-3.sil.org/sites/iso639-3/files/downloads/iso-639-3.tab), [Data](./LanguageData/iso6393), [JSON](./LanguageData/iso6393.json), [Code](./LanguageTags/Iso6393DataGen.cs)
- RFC 5646 : [Source](https://www.iana.org/assignments/language-subtag-registry/language-subtag-registry), [Data](./LanguageData/rfc5646), [JSON](./LanguageData/rfc5646.json), [Code](./LanguageTags/rfc5646DataGen.cs)

The data files are [updated](./LanguageTagsCreate) weekly using a scheduled [actions job](./.github/workflows/update-languagedata.yml).

## Usage

### Tag Format

Refer to [RFC 5646 Section 2.1](https://www.rfc-editor.org/rfc/rfc5646#section-2.1) for complete language tag syntax and rules.

IETF language tags are constructed from sub-tags in the form of:

- Normal tags:
  - `[Language]-[Extended language]-[Script]-[Region]-[Variant]-[Extension]-[Private Use]`
  - Language:
    - See [RFC 5646 Section 2.2.1](https://www.rfc-editor.org/rfc/rfc5646#section-2.2.1)
    - 2 - 3 alpha: Shortest ISO 639 code
    - 4 alpha: Future use
    - 5 - 8 alpha: Registered tag
  - Extended language:
    - See [RFC 5646 Section 2.2.2](https://www.rfc-editor.org/rfc/rfc5646#section-2.2.2)
    - 3 alpha: Reserved ISO 639 code
  - Script:
    - See [RFC 5646 Section 2.2.3](https://www.rfc-editor.org/rfc/rfc5646#section-2.2.3)
    - 4 alpha: [ISO 15924](https://unicode.org/iso15924/iso15924-codes.html) code
  - Region:
    - See [RFC 5646 Section 2.2.4](https://www.rfc-editor.org/rfc/rfc5646#section-2.2.4)
    - 2 alpha: [ISO 3166-1](https://en.wikipedia.org/wiki/ISO_3166-1) code
    - 3 digit: [UN M.49](https://unstats.un.org/unsd/methodology/m49/) code
  - Variant:
    - See [RFC 5646 Section 2.2.5](https://www.rfc-editor.org/rfc/rfc5646#section-2.2.5)
    - 5 - 8 alphanumeric: Registered tag
  - Extension: (`[singleton]-[extension]`)
    - See [RFC 5646 Section 2.2.6](https://www.rfc-editor.org/rfc/rfc5646#section-2.2.6)
    - 1 alphanumeric: Singleton
    - 2 - 8 alphanumeric: Extension
  - Private Use: (`x-[private]`)
    - See [RFC 5646 Section 2.2.7](https://www.rfc-editor.org/rfc/rfc5646#section-2.2.7)
    - `x`: Singleton
    - 1 - 8 alphanumeric: Private use
- Grandfathered tags:
  - See [RFC 5646 Section 2.2.8](https://www.rfc-editor.org/rfc/rfc5646#section-2.2.8)
  - Grandfathered tags are converted to current form tags
  - E.g. `en-gb-oed` -> `en-GB-oxendict`, `i-klingon` -> `tlh`.
- Private use:
  - All tags are private use
  - `x-[private]-[private]`

Examples:

- `zh` : `[Language]`
- `zh-yue` : `[Language]-[Extended language]`
- `zh-yue-hk`: `[Language]-[Extended language]-[Region]`
- `hy-latn-it-arevela`: `[Language]-[Script]-[Region]-[Variant]`
- `en-a-bbb-x-a-ccc` : `[Language]-[Extension]-[Private Use]`
- `en-latn-gb-boont-r-extended-sequence-x-private` : `[Language]-[Script]-[Region]-[Variant]-[Extension]-[Private Use]`

### Tag Lookup

Tag records can be constructed by calling `Create()`, or loaded from data `LoadData()`, or loaded from JSON `LoadJson()`. The records and record collections are immutable and can safely be reused and shared across threads.

Each class implements a `Find(string languageTag, bool includeDescription)` method that will search all tags in all records for a matching tag.\
This is mostly a convenience function and specific use cases should use specific tags.

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
Iso6393Data iso6393 = Iso6393Data.LoadData("iso6393");
Iso6393Record? record = iso6393.Find("zh", false);
// record.Id = "zho"
// record.Part1 = "zh"
// record.RefName = "Chinese"
record = iso6393.Find("yue chinese", true);
// record.Id = "yue"
// record.RefName = "Yue Chinese"
```

```csharp
Rfc5646Data rfc5646 = Rfc5646Data.LoadJson("rfc5646.json");
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

Tag matching can be used to select content based on preferred vs. available languages.\
E.g. in HTTP [`Accept-Language`](https://www.rfc-editor.org/rfc/rfc9110.html#name-accept-language) and [`Content-Language`](https://www.rfc-editor.org/rfc/rfc9110.html#name-content-language), or Matroska media stream [`LanguageIETF Element`](https://datatracker.ietf.org/doc/html/draft-ietf-cellar-matroska-07#name-language-codes).

IETF language tags are in the form of `[Language]-[Extended language]-[Script]-[Region]-[Variant]-[Extension]-[Private Use]`, and sub-tag matching happens left to right until a match is found.

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

The `Normalize()` method will build the tag and perform validation and normalization. See [Tag Normalization](#tag-normalization) for details.

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

The `LanguageTag` class static `Parse()` method will parse the text form language tag and return a constructed `LanguageTag` object, or `null` in case of parsing failure.

Parsing will validate all subtags for correctness in type, length, and position, but not value, and case will not be modified.

Grandfathered tags will be converted to their current preferred form and parsed as such.\
E.g. `en-gb-oed` -> `en-GB-oxendict`, `i-klingon` -> `tlh`.

The `Normalize()` method will parse the text tag, and perform validation and normalization. See [Tag Normalization](#tag-normalization) for details.

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

The `LanguageTag` instance `Normalize()` method will convert tags to their canonical form.\
See [RFC 5646 Section 4.5 for details](https://www.rfc-editor.org/rfc/rfc5646#section-4.5)

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

The `LanguageTag` class `Validate()` method will verify subtags for correctness.\
See [RFC 5646 Section 2.1](https://www.rfc-editor.org/rfc/rfc5646#section-2.1) and [RFC 5646 Section 2.2.9](https://www.rfc-editor.org/rfc/rfc5646#section-2.2.9) for details. Refer to [Tag Format](#tag-format) for a summary.

Note that `LanguageTag` objects created by `Parse()` or `Normalize()` are already verified for form correctness during parsing, and `Validate()` is primarily of use when using the `LanguageTagBuilder` `Build()` method directly.

Validation includes the following:

- Subtag shape correctness, see [Tag Format](#tag-format) for a summary.
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

## Testing

The [BCP47 language subtag lookup](https://r12a.github.io/app-subtags/) site offers convenient tag parsing and validation capabilities.

Refer to [unit tests](./LanguageTagsTests) for code validation.\
Note that testing attests to the desired behavior in code, but the implemented functionality may not be complete or accurate per the RFC 5646 specification.

## References

- [Wikipedia : Codes for constructed languages](https://en.wikipedia.org/wiki/Codes_for_constructed_languages)
- [Wikipedia : IETF language tag](https://en.wikipedia.org/wiki/IETF_language_tag)
- [W3C : Choosing a Language Tag](https://www.w3.org/International/questions/qa-choosing-language-tags)
- [W3C : Language tags in HTML and XML](https://www.w3.org/International/articles/language-tags/)
- [W3C : BCP47 language subtag lookup](https://r12a.github.io/app-subtags/)
- [IANA : Language Subtags, Tag Extensions, and Tags](https://www.iana.org/assignments/language-subtags-tags-extensions/language-subtags-tags-extensions.xhtml)
- [RFC : BCP47](https://www.rfc-editor.org/info/bcp47)
- [RFC : 4647 : Matching of Language Tags](https://www.rfc-editor.org/info/rfc4647)
- [RFC : 5646 : Tags for Identifying Languages](https://www.rfc-editor.org/info/rfc5646)
- [Unicode Consortium : Unicode Common Locale Data Repository (CLDR) Project](https://cldr.unicode.org/)
- [Library of Congress : ISO 639-2 Language Coding Agency](https://www.loc.gov/standards/iso639-2/)
- [SIL International : ISO 639-3 Language Coding Agency](https://iso639-3.sil.org/)

## Language Tag Libraries

- [github.com/rspeer/langcodes](https://github.com/rspeer/langcodes)
- [github.com/oxigraph/oxilangtag](https://github.com/oxigraph/oxilangtag)
- [github.com/pyfisch/rust-language-tags/](https://github.com/pyfisch/rust-language-tags/)
- [github.com/DanSmith/languagetags-sharp](https://github.com/DanSmith/languagetags-sharp)
- [github.com/jkporter/bcp47](https://github.com/jkporter/bcp47)
- [github.com/mattcg/language-subtag-registry](https://github.com/mattcg/language-subtag-registry)

## 3rd Party Tools

- [AwesomeAssertions](https://awesomeassertions.org/)
- [Bring Your Own Badge](https://github.com/marketplace/actions/bring-your-own-badge)
- [CSharpier](https://csharpier.com/)
- [Create Pull Request](https://github.com/marketplace/actions/create-pull-request)
- [GH Release](https://github.com/marketplace/actions/gh-release)
- [Git Auto Commit](https://github.com/marketplace/actions/git-auto-commit)
- [GitHub Actions](https://github.com/actions)
- [GitHub Dependabot](https://github.com/dependabot)
- [Husky.Net](https://alirezanet.github.io/Husky.Net/)
- [Nerdbank.GitVersioning](https://github.com/marketplace/actions/nerdbank-gitversioning)
- [Serilog](https://serilog.net/)
- [xUnit.Net](https://xunit.net/)

## License

Licensed under the [MIT License](./LICENSE)\
![GitHub](https://img.shields.io/github/license/ptr727/LanguageTags)
