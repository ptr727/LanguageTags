using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ptr727.LanguageTags;

public class LanguageLookup
{
    // Undetermined
    public const string Undetermined = "und";

    private readonly Iso6392 _iso6392 = Iso6392.Create();
    private readonly Iso6393 _iso6393 = Iso6393.Create();
    private readonly Rfc5646 _rfc5646 = Rfc5646.Create();

    private static CultureInfo CreateCultureInfo(string languageTag)
    {
        // Cultures are created on the fly in .NET, we can't rely on an exception
        // RFC 5646 defines Zzzz as "Code for uncoded script"
        // .NET uses "zzz" for a similar purpose
        // https://stackoverflow.com/questions/35074033/invalid-cultureinfo-no-longer-throws-culturenotfoundexception/
        const string Missing = "zzz";

        try
        {
            // Get a CultureInfo representation
            CultureInfo cultureInfo = CultureInfo.GetCultureInfo(languageTag, true);
            if (cultureInfo == null)
            {
                return null;
            }

            // Make sure the culture was not custom created
            return
                cultureInfo.ThreeLetterWindowsLanguageName.Equals(
                    Missing,
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

    // Language lookup substitutions
    public List<(string ietf, string iso)> Substitutions { get; private set; } = [];

    /// <summary>
    /// Converts an ISO-639 language tag to its corresponding IETF / RFC-5646 language tag.
    /// </summary>
    /// <param name="languageTag">
    /// The ISO language tag to convert. This can be an ISO-639-1, ISO-639-2, or ISO-639-3 code, or a language tag recognized by the system CultureInfo.
    /// </param>
    /// <returns>
    /// The corresponding RFC-5646 language tag if a match is found; otherwise undetermined language.
    /// </returns>
    public string GetIetfFromIso(string languageTag)
    {
        // Undetermined
        if (string.IsNullOrEmpty(languageTag))
        {
            return Undetermined;
        }

        // Substitutions
        (string ietf, string iso) match = Substitutions.FirstOrDefault(item =>
            item.iso.Equals(languageTag, StringComparison.OrdinalIgnoreCase)
        );
        if (match != default)
        {
            return match.ietf;
        }

        // Find a matching RFC 5646 record
        Rfc5646.Record rfc5646 = _rfc5646.Find(languageTag, false);
        if (rfc5646 != null)
        {
            return rfc5646.TagAny;
        }

        // Find a matching ISO-639-3 record
        Iso6393.Record iso6393 = _iso6393.Find(languageTag, false);
        if (iso6393 != null)
        {
            // Find a matching RFC 5646 record from the ISO-639-3 or ISO-639-1 tag
            rfc5646 = _rfc5646.Find(iso6393.Id, false);
            rfc5646 ??= _rfc5646.Find(iso6393.Part1, false);
            if (rfc5646 != null)
            {
                return rfc5646.TagAny;
            }
        }

        // Find a matching ISO-639-2 record
        Iso6392.Record iso6392 = _iso6392.Find(languageTag, false);
        if (iso6392 != null)
        {
            // Find a matching RFC 5646 record from the ISO-639-2 or ISO-639-1 tag
            rfc5646 = _rfc5646.Find(iso6392.Id, false);
            rfc5646 ??= _rfc5646.Find(iso6392.Part1, false);
            if (rfc5646 != null)
            {
                return rfc5646.TagAny;
            }
        }

        // Try CultureInfo
        CultureInfo cultureInfo = CreateCultureInfo(languageTag);
        return cultureInfo != null ? cultureInfo.IetfLanguageTag : Undetermined;
    }

    /// <summary>
    /// Converts an IETF / RFC-5646 language tag to its corresponding ISO-639 language tag.
    /// </summary>
    /// <param name="languageTag">
    /// The RFC-5646 language tag to convert. This can be an IETF / RFC-5646 code, or a language tag recognized by the system CultureInfo.
    /// </param>
    /// <returns>
    /// The corresponding ISO-639-3 language tag if a match is found; otherwise undetermined language.
    /// </returns>
    public string GetIsoFromIetf(string languageTag)
    {
        // Undetermined
        if (string.IsNullOrEmpty(languageTag))
        {
            return Undetermined;
        }

        // Substitutions
        (string ietf, string iso) match = Substitutions.FirstOrDefault(item =>
            item.ietf.Equals(languageTag, StringComparison.OrdinalIgnoreCase)
        );
        if (match != default)
        {
            return match.iso;
        }

        // Find a matching RFC-5646 record
        Rfc5646.Record rfc5646 = _rfc5646.Find(languageTag, false);
        if (rfc5646 != null)
        {
            // Use expanded form if Redundant, or just use TagAny
            // E.g. cmn-Hant -> zh-cmn-Hant
            languageTag = rfc5646.TagAny;
        }

        // TODO: Split complex tags and resolve in parts
        // language-extlang-script-region-variant-extension-privateuse-...
        // zh-cmn-Hant-Foo-Bar -> zh-cmn-Hant-Foo -> zh-cmn-Hant -> zh-cmn -> zh
        // Private tags -x- is not expected to resolve
        // E.g. zh-cmn-Hans-CN, sr-Latn, zh-yue-HK, sl-IT-nedis, hy-Latn-IT-arevela, az-Arab-x-AZE-derbend

        // Split the parts and use the first part
        // zh-cmn-Hant -> zh
        string[] parts = languageTag.Split('-');
        languageTag = parts[0];

        // Get ISO-639-3 record
        Iso6393.Record iso6393 = _iso6393.Find(languageTag, false);
        if (iso6393 != null)
        {
            // Return the Part 2B code
            return iso6393.Part2B;
        }

        // Get ISO-639-2 record
        Iso6392.Record iso6392 = _iso6392.Find(languageTag, false);
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

        // Get ISO-639-3 record from cultureInfo ISO code
        iso6393 = _iso6393.Find(cultureInfo.ThreeLetterISOLanguageName, false);
        if (iso6393 != null)
        {
            // Return the Part 2B code
            return iso6393.Part2B;
        }
        return Undetermined;
    }

    /// <summary>
    /// Determines whether the specified <paramref name="languageTag"/> matches the given <paramref name="prefix"/> according to language tag matching rules.
    /// </summary>
    /// <param name="prefix">
    /// The language tag prefix to match against (e.g., "zh", "zh-Hant").
    /// </param>
    /// <param name="languageTag">
    /// The language tag to test for a match (e.g., "zh-Hant-foo").
    /// </param>
    /// <returns>
    /// <c>true</c> if <paramref name="languageTag"/> matches the <paramref name="prefix"/>; otherwise, <c>false</c>.
    /// </returns>
    public bool IsMatch(string prefix, string languageTag)
    {
        // https://r12a.github.io/app-subtags/
        // zh match: zh: zh, zh-Hant, zh-Hans, zh-cmn-Hant
        // zho not: zh
        // zho match: zho
        // zh-Hant match: zh-Hant, zh-Hant-foo
        while (true)
        {
            // The language matches the prefix exactly
            if (languageTag.Equals(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // The language start with the prefix, and the the next character is a -
            if (
                languageTag.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && languageTag[prefix.Length..].StartsWith('-')
            )
            {
                return true;
            }

            // Get the extended format of the language
            // E.g. cmn-Hant should be expanded to zh-cmn-Hant else zh will not match

            // Find a matching RFC 5646 record
            Rfc5646.Record rfc5646 = _rfc5646.Find(languageTag, false);
            if (rfc5646 != null)
            {
                // If the lookup is different then rematch
                if (!string.Equals(languageTag, rfc5646.TagAny, StringComparison.OrdinalIgnoreCase))
                {
                    // Reiterate
                    languageTag = rfc5646.TagAny;
                    continue;
                }
            }

            // No match
            return false;
        }
    }
}
