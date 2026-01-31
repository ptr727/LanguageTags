namespace ptr727.LanguageTags;

/// <summary>
/// Provides language code lookup and conversion functionality between IETF and ISO standards.
/// </summary>
/// <param name="options">The options used to configure logging.</param>
public sealed class LanguageLookup(Options? options = null)
{
    /// <summary>
    /// The language code for undetermined languages ("und").
    /// </summary>
    public const string Undetermined = "und";

    private readonly ILogger _logger = LogOptions.CreateLogger<LanguageLookup>(options);
    private readonly Iso6392Data _iso6392 = Iso6392Data.Create();
    private readonly Iso6393Data _iso6393 = Iso6393Data.Create();
    private readonly Rfc5646Data _rfc5646 = Rfc5646Data.Create();
    private readonly List<(string ietf, string iso)> _overrides = [];

    private static CultureInfo? CreateCultureInfo(string languageTag)
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

    /// <summary>
    /// Gets the list of manual override mappings between IETF and ISO language codes.
    /// </summary>
    public IList<(string ietf, string iso)> Overrides => _overrides;

    /// <summary>
    /// Converts an ISO language code to its IETF BCP 47 equivalent.
    /// </summary>
    /// <param name="languageTag">The ISO language code to convert.</param>
    /// <returns>The IETF BCP 47 language tag, or "und" if the conversion fails.</returns>
    public string GetIetfFromIso(string languageTag)
    {
        // Undetermined
        if (string.IsNullOrEmpty(languageTag))
        {
            return Undetermined;
        }

        // Manual overrides
        (string ietf, string iso) match = _overrides.FirstOrDefault(item =>
            item.iso.Equals(languageTag, StringComparison.OrdinalIgnoreCase)
        );
        if (match != default)
        {
            return match.ietf;
        }

        // Find a matching subtag record
        Rfc5646Record? subtag = _rfc5646.Find(languageTag, false);
        if (subtag != null)
        {
            return subtag.TagValue;
        }

        // Find a matching ISO 639-3 record
        Iso6393Record? iso6393 = _iso6393.Find(languageTag, false);
        if (iso6393 != null)
        {
            // Find a matching subtag record from the ISO 639-3 or ISO 639-1 tag
            subtag = _rfc5646.Find(iso6393.Id, false);
            subtag ??= _rfc5646.Find(iso6393.Part1, false);
            if (subtag != null)
            {
                return subtag.TagValue;
            }
        }

        // Find a matching ISO 639-2 record
        Iso6392Record? iso6392 = _iso6392.Find(languageTag, false);
        if (iso6392 != null)
        {
            // Find a matching RFC 5646 record from the ISO 639-2 or ISO 639-1 tag
            subtag = _rfc5646.Find(iso6392.Part2B, false);
            subtag ??= _rfc5646.Find(iso6392.Part1, false);
            if (subtag != null)
            {
                return subtag.TagValue;
            }
        }

        // Try CultureInfo
        CultureInfo? cultureInfo = CreateCultureInfo(languageTag);
        if (cultureInfo != null)
        {
            return cultureInfo.IetfLanguageTag;
        }

        _logger.LogUndeterminedFallback(languageTag, nameof(GetIetfFromIso));
        return Undetermined;
    }

    /// <summary>
    /// Converts an IETF BCP 47 language tag to its ISO equivalent.
    /// </summary>
    /// <param name="languageTag">The IETF BCP 47 language tag to convert.</param>
    /// <returns>The ISO 639-2/B language code, or "und" if the conversion fails.</returns>
    public string GetIsoFromIetf(string languageTag)
    {
        // Undetermined
        if (string.IsNullOrEmpty(languageTag))
        {
            return Undetermined;
        }

        // Manual overrides
        (string ietf, string iso) match = _overrides.FirstOrDefault(item =>
            item.ietf.Equals(languageTag, StringComparison.OrdinalIgnoreCase)
        );
        if (match != default)
        {
            return match.iso;
        }

        // TODO: Conditional parse and normalize before processing

        // Find a matching subtag record
        Rfc5646Record? subtag = _rfc5646.Find(languageTag, false);
        if (subtag != null)
        {
            // Use expanded form if Redundant, or just use TagAny
            // E.g. cmn-Hant -> zh-cmn-Hant
            languageTag = subtag.TagValue;
        }

        // TODO: Convert to use Parse()

        // Split language tags and resolve in parts
        // language-extlang-script-region-variant-extension-privateuse
        // E.g. zh-cmn-Hant-x-foo -> zh-cmn-Hant -> zh-cmn -> zh
        string[] parts = languageTag.Split('-');
        languageTag = parts[0];

        // Get ISO 639-3 record
        Iso6393Record? iso6393 = _iso6393.Find(languageTag, false);
        if (iso6393 != null)
        {
            // Return the Part 2B code
            return iso6393.Part2B!;
        }

        // Get ISO 639-2 record
        Iso6392Record? iso6392 = _iso6392.Find(languageTag, false);
        if (iso6392 != null)
        {
            // Return the Part 2B code
            return iso6392.Part2B!;
        }

        // Try cultureInfo
        CultureInfo? cultureInfo = CreateCultureInfo(languageTag);
        if (cultureInfo == null)
        {
            _logger.LogUndeterminedFallback(languageTag, nameof(GetIsoFromIetf));
            return Undetermined;
        }

        // Get ISO 639-3 record from cultureInfo ISO code
        iso6393 = _iso6393.Find(cultureInfo.ThreeLetterISOLanguageName, false);
        if (iso6393 != null)
        {
            // Return the Part 2B code
            return iso6393.Part2B!;
        }
        _logger.LogUndeterminedFallback(languageTag, nameof(GetIsoFromIetf));
        return Undetermined;
    }

    /// <summary>
    /// Determines whether a language tag matches or starts with the specified prefix.
    /// </summary>
    /// <param name="prefix">The prefix to match against.</param>
    /// <param name="languageTag">The language tag to test.</param>
    /// <returns>true if the language tag matches or starts with the prefix; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="prefix"/> or <paramref name="languageTag"/> is null.</exception>
    public bool IsMatch(string prefix, string languageTag)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        ArgumentNullException.ThrowIfNull(languageTag);

        string originalPrefix = prefix;
        string originalTag = languageTag;

        // TODO: Conditional parse and normalize before processing

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
            Rfc5646Record? subtag = _rfc5646.Find(languageTag, false);
            if (subtag != null)
            {
                // If the subtag is different then rematch
                if (
                    !string.Equals(languageTag, subtag.TagValue, StringComparison.OrdinalIgnoreCase)
                )
                {
                    // Rematch
                    languageTag = subtag.TagValue;
                    continue;
                }
            }

            // No match
            _logger.LogPrefixMatchFailed(originalPrefix, originalTag);
            return false;
        }
    }

    /// <summary>
    /// Determines if two language tags are equivalent (case-insensitive).
    /// </summary>
    /// <param name="tag1">The first language tag.</param>
    /// <param name="tag2">The second language tag.</param>
    /// <returns>true when the tags are equal ignoring case; otherwise, false.</returns>
    public static bool AreEquivalent(string tag1, string tag2) =>
        string.Equals(tag1, tag2, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Normalizes and compares two language tags for equivalence.
    /// </summary>
    /// <param name="tag1">The first language tag.</param>
    /// <param name="tag2">The second language tag.</param>
    /// <returns>true when both tags can be parsed and normalize to the same value; otherwise, false.</returns>
    public static bool AreEquivalentNormalized(string tag1, string tag2)
    {
        LanguageTag? parsed1 = LanguageTag.Parse(tag1)?.Normalize();
        LanguageTag? parsed2 = LanguageTag.Parse(tag2)?.Normalize();
        return parsed1?.ToString().Equals(parsed2?.ToString(), StringComparison.OrdinalIgnoreCase)
            ?? false;
    }
}
