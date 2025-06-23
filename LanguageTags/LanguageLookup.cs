using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ptr727.LanguageTags;

public class LanguageLookup
{
    // Undetermined
    public const string Undetermined = "und";

    private readonly Iso6392Data _iso6392 = Iso6392Data.Create();
    private readonly Iso6393Data _iso6393 = Iso6393Data.Create();
    private readonly Rfc5646Data _rfc5646 = Rfc5646Data.Create();

    private static CultureInfo CreateCultureInfo(string languageTag)
    {
        // Cultures are created on the fly in .NET, we can't rely on an exception
        // RFC 5646 defines Zzzz as "Code for uncoded script"
        // .NET uses "zzz" for a similar purpose
        // https://stackoverflow.com/questions/35074033/invalid-cultureinfo-no-longer-throws-culturenotfoundexception/
        const string missing = "zzz";

        try
        {
            // Get a CultureInfo representation
            CultureInfo cultureInfo = CultureInfo.GetCultureInfo(languageTag, true);

            // Make sure the culture was not custom created
            return
                cultureInfo.ThreeLetterWindowsLanguageName.Equals(
                    missing,
                    StringComparison.OrdinalIgnoreCase
                )
                || (cultureInfo.CultureTypes & CultureTypes.UserCustomCulture)
                    == CultureTypes.UserCustomCulture
                ? null
                : cultureInfo;
        }
        catch (CultureNotFoundException) { }
        return null;
    }

    public List<(string ietf, string iso)> Overrides { get; } = [];

    public string GetIetfFromIso(string languageTag)
    {
        // Undetermined
        if (string.IsNullOrEmpty(languageTag))
        {
            return Undetermined;
        }

        // Manual overrides
        (string ietf, string iso) match = Overrides.FirstOrDefault(item =>
            item.iso.Equals(languageTag, StringComparison.OrdinalIgnoreCase)
        );
        if (match != default)
        {
            return match.ietf;
        }

        // Find a matching subtag record
        Rfc5646Data.Record subtag = _rfc5646.Find(languageTag, false);
        if (subtag != null)
        {
            return subtag.TagAny;
        }

        // Find a matching ISO 639-3 record
        Iso6393Data.Record iso6393 = _iso6393.Find(languageTag, false);
        if (iso6393 != null)
        {
            // Find a matching subtag record from the ISO 639-3 or ISO 639-1 tag
            subtag = _rfc5646.Find(iso6393.Id, false);
            subtag ??= _rfc5646.Find(iso6393.Part1, false);
            if (subtag != null)
            {
                return subtag.TagAny;
            }
        }

        // Find a matching ISO 639-2 record
        Iso6392Data.Record iso6392 = _iso6392.Find(languageTag, false);
        if (iso6392 != null)
        {
            // Find a matching RFC 5646 record from the ISO 639-2 or ISO 639-1 tag
            subtag = _rfc5646.Find(iso6392.Part2B, false);
            subtag ??= _rfc5646.Find(iso6392.Part1, false);
            if (subtag != null)
            {
                return subtag.TagAny;
            }
        }

        // Try CultureInfo
        CultureInfo cultureInfo = CreateCultureInfo(languageTag);
        return cultureInfo != null ? cultureInfo.IetfLanguageTag : Undetermined;
    }

    public string GetIsoFromIetf(string languageTag)
    {
        // Undetermined
        if (string.IsNullOrEmpty(languageTag))
        {
            return Undetermined;
        }

        // Manual overrides
        (string ietf, string iso) match = Overrides.FirstOrDefault(item =>
            item.ietf.Equals(languageTag, StringComparison.OrdinalIgnoreCase)
        );
        if (match != default)
        {
            return match.iso;
        }

        // Find a matching subtag record
        Rfc5646Data.Record subtag = _rfc5646.Find(languageTag, false);
        if (subtag != null)
        {
            // Use expanded form if Redundant, or just use TagAny
            // E.g. cmn-Hant -> zh-cmn-Hant
            languageTag = subtag.TagAny;
        }

        // TODO: Convert to use Parse()

        // Split language tags and resolve in parts
        // language-extlang-script-region-variant-extension-privateuse
        // E.g. zh-cmn-Hant-x-foo -> zh-cmn-Hant -> zh-cmn -> zh
        string[] parts = languageTag.Split('-');
        languageTag = parts[0];

        // Get ISO 639-3 record
        Iso6393Data.Record iso6393 = _iso6393.Find(languageTag, false);
        if (iso6393 != null)
        {
            // Return the Part 2B code
            return iso6393.Part2B;
        }

        // Get ISO 639-2 record
        Iso6392Data.Record iso6392 = _iso6392.Find(languageTag, false);
        if (iso6392 != null)
        {
            // Return the Part 2B code
            return iso6392.Part2B;
        }

        // Try cultureInfo
        CultureInfo cultureInfo = CreateCultureInfo(languageTag);
        if (cultureInfo == null)
        {
            return Undetermined;
        }

        // Get ISO 639-3 record from cultureInfo ISO code
        iso6393 = _iso6393.Find(cultureInfo.ThreeLetterISOLanguageName, false);
        if (iso6393 != null)
        {
            // Return the Part 2B code
            return iso6393.Part2B;
        }
        return Undetermined;
    }

    public bool IsMatch(string prefix, string languageTag)
    {
        // https://r12a.github.io/app-subtags/
        // zh match: zh: zh, zh-Hant, zh-Hans, zh-cmn-Hant
        // zho not: zh
        // zho match: zho
        // zh-Hant match: zh-Hant, zh-Hant-foo
        while (true)
        {
            // The tag matches the prefix exactly
            if (languageTag.Equals(prefix, StringComparison.OrdinalIgnoreCase))
            {
                // Exact match
                return true;
            }

            // The tag starts with the prefix, and the next character is a -
            if (
                languageTag.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && languageTag[prefix.Length..].StartsWith('-')
            )
            {
                // Prefix match
                return true;
            }

            // Get the extended format of the language
            // E.g. cmn-Hant should be expanded to zh-cmn-Hant else zh will not match

            // Find a matching subtag record
            Rfc5646Data.Record subtag = _rfc5646.Find(languageTag, false);
            if (subtag != null)
            {
                // If the subtag is different then rematch
                if (!string.Equals(languageTag, subtag.TagAny, StringComparison.OrdinalIgnoreCase))
                {
                    // rematch
                    languageTag = subtag.TagAny;
                    continue;
                }
            }

            // No match
            return false;
        }
    }
}
