# LanguageTags

C# .NET library for ISO 639-2, ISO 639-3, RFC 5646 / BCP 47 language tags.

## License

Licensed under the [MIT License](./LICENSE)\
![GitHub](https://img.shields.io/github/license/ptr727/LanguageTags)

## Build Status

Code and Pipeline is on [GitHub](https://github.com/ptr727/LanguageTags)\
![GitHub Last Commit](https://img.shields.io/github/last-commit/ptr727/LanguageTags?logo=github)\
![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/ptr727/LanguageTags/BuildPublishPipeline.yml?logo=github)

## NuGet Package

Packages published on [NuGet](https://www.nuget.org/packages/ptr727.LanguageTags/)\
![NuGet](https://img.shields.io/nuget/v/ptr727.LanguageTags?logo=nuget)

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
- [Unicode Consortium : Unicode CLDR Project](https://cldr.unicode.org/)
- [Library of Congress : ISO 639-2 Language Coding Agency](https://www.loc.gov/standards/iso639-2/)
- [SIL International : ISO 639-3 Language Coding Agency](https://iso639-3.sil.org/)

## Build Process

The build process downloads the latest language tag data files, converts them into JSON files for easy consumption, and generates C# classes with all the tags for direct use in code.

- ISO 639-2: [Source](https://www.loc.gov/standards/iso639-2/ISO-639-2_utf-8.txt), [Data](./LanguageData/iso6392), [JSON](./LanguageData/iso6392.json), [Code](./LanguageTags/Iso6392Gen.cs)
- ISO 639-3: [Source](https://iso639-3.sil.org/sites/iso639-3/files/downloads/iso-639-3.tab), [Data](./LanguageData/iso6393), [JSON](./LanguageData/iso6393.json), [Code](./LanguageTags/Iso6393Gen.cs)
- RFC 5646: [Source](https://www.iana.org/assignments/language-subtag-registry/language-subtag-registry), [Data](./LanguageData/rfc5646), [JSON](./LanguageData/rfc5646.json), [Code](./LanguageTags/Rfc5646Gen.cs)

The data files are [updated](./LanguageTagsCreate/) on a weekly bases using a scheduled [actions job](./.github/workflows/update-languagedata.yml).

## Usage

### Tag Lookup

```csharp
Iso6392 iso6392 = Iso6392.Create();
Iso6393 iso6393 = Iso6393.LoadData("Iso6393");
Rfc5646 rfc5646 = Rfc5646.LoadJson("Rfc5646.json");
```

```csharp
public Record Find(string languageTag, bool includeDescription);
```

```csharp
Iso6392.Record record = iso6392.Find("afr", false)
// record.Part2B = afr
// record.RefName = Afrikaans
record = iso6392.Find("zulu", true)
// record.Part2B = zul
// record.RefName = Zulu
```

```csharp
Iso6393.Record record = iso6393.Find("zh", false)
// record.Id = zho
// record.Part1 = zh
// record.RefName = Chinese
record = iso6392.Find("yue chinese", true)
// record.Id = yue
// record.RefName = Yue Chinese
```

```csharp
Rfc5646.Record record = rfc5646.Find("de", false)
// record.SubTag = de
// record.Description = German
record = iso6392.Find("zh-cmn-Hant", false)
// record.Tag = zh-cmn-Hant
// record.Description = Mandarin Chinese (Traditional)
record = iso6392.Find("Inuktitut in Canadian", true)
// record.Tag = iu-Cans
// record.Description = Inuktitut in Canadian Aboriginal Syllabic script
```

### Tag Conversion

```csharp
LanguageLookup languageLookup = new();
languageLookup.GetIetfFromIso("afr"); // af
languageLookup.GetIetfFromIso("zho"); // zh
```

```csharp
LanguageLookup languageLookup = new();
languageLookup.GetIsoFromIetf("af"); // afr
languageLookup.GetIsoFromIetf("zh-cmn-Hant"); // chi
languageLookup.GetIsoFromIetf("cmn-Hant"); // chi
```

### Tag Matching

RFC 5646 / BCP 47 language tags are in the form of `language-extlang-script-region-variant-extension-privateuse`, and matching happens left to right.\
E.g. `pt` will match `pt` Portuguese, or `pt-BR` Brazilian Portuguese, or `pt-PT` European Portuguese.\
E.g. `pt-BR` will only match only `pt-BR` Brazilian Portuguese.\
E.g. `zh` will match `zh` Chinese, or `zh-Hans` simplified Chinese, or `zh-Hant` for traditional Chinese, and other variants.\
E.g. `zh-Hans` will only match `zh-Hans` simplified Chinese.

```csharp
LanguageLookup languageLookup = new();
languageLookup.IsMatch("en", "en-US"); // true
languageLookup.IsMatch("zh", "zh-cmn-Hant"); // true
languageLookup.IsMatch("sr-Latn", "sr-Latn-RS"); // true
languageLookup.IsMatch("zha", "zh-Hans"); // false
languageLookup.IsMatch("zh-Hant", "zh-Hans"); // false
```

## 3rd Party Tools

- [AwesomeAssertions](https://awesomeassertions.org/)
- [Bring Your Own Badge](https://github.com/marketplace/actions/bring-your-own-badge)
- [Create Pull Request](https://github.com/marketplace/actions/create-pull-request)
- [GH Release](https://github.com/marketplace/actions/gh-release)
- [Git Auto Commit](https://github.com/marketplace/actions/git-auto-commit)
- [GitHub Actions](https://github.com/actions)
- [GitHub Dependabot](https://github.com/dependabot)
- [Nerdbank.GitVersioning](https://github.com/marketplace/actions/nerdbank-gitversioning)
- [Serilog](https://serilog.net/)
- [xUnit.Net](https://xunit.net/)

## Other Language Tag Libraries

- [github.com/DanSmith/languagetags-sharp](https://github.com/DanSmith/languagetags-sharp)
- [github.com/jkporter/bcp47](https://github.com/jkporter/bcp47)
- [github.com/mattcg/language-subtag-registry](https://github.com/mattcg/language-subtag-registry)
- [github.com/rspeer/langcodes](https://github.com/rspeer/langcodes)
