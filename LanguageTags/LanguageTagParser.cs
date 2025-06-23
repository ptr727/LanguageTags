using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ptr727.LanguageTags;

// Parse the tag per RFC 5646 2.1
// https://www.rfc-editor.org/rfc/rfc5646#section-2.1

// Examples
// https://www.rfc-editor.org/rfc/rfc5646#appendix-A

// TODO: Use a BNF parser or parse code generator vs. hand parsing
// https://github.com/antlr/antlr4
// https://github.com/gabriele-tomassetti/antlr-mega-tutorial
// https://github.com/picoe/Eto.Parse
// https://github.com/i-e-b/Gool
// https://www.parse2.com/manual-cs.shtml

// TODO: Implement subtag content validation by comparing values with the registry data

public class LanguageTagParser
{
    private readonly Rfc5646Data _rfc5646 = Rfc5646Data.Create();
    private readonly List<string> _tagList = [];
    private LanguageTag _languageTag;

    private string ParseGrandfathered(string languageTag)
    {
        // Grandfathered and Redundant Registrations
        // https://www.rfc-editor.org/rfc/rfc5646#section-2.2.8

        // Search tag registry
        // Type = Grandfathered, Tag = i-navajo, PreferredValue = nv
        List<Rfc5646Data.Record> recordList =
        [
            .. _rfc5646.RecordList.Where(record =>
                record.Type == Rfc5646Data.RecordType.Grandfathered
                && !string.IsNullOrEmpty(record.Tag)
                && record.Tag.Equals(languageTag, StringComparison.OrdinalIgnoreCase)
            ),
        ];
        if (recordList.Count == 1)
        {
            return recordList[0].PreferredValue;
        }
        Debug.Assert(recordList.Count is 0 or 1);

        // No match
        return languageTag;
    }

    private static void SetCase(LanguageTag languageTag)
    {
        // Language
        if (!string.IsNullOrEmpty(languageTag.Language))
        {
            languageTag.Language = languageTag.Language.ToLowerInvariant();
        }

        // Extended language
        if (!string.IsNullOrEmpty(languageTag.ExtendedLanguage))
        {
            languageTag.ExtendedLanguage = languageTag.ExtendedLanguage.ToLowerInvariant();
        }

        // Script
        if (!string.IsNullOrEmpty(languageTag.Script))
        {
            languageTag.Script =
                System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(
                    languageTag.Script.ToLowerInvariant()
                );
        }

        // Region
        if (!string.IsNullOrEmpty(languageTag.Region))
        {
            languageTag.Region = languageTag.Region.ToUpperInvariant();
        }

        // Variants
        for (int i = 0; i < languageTag.VariantList.Count; i++)
        {
            languageTag.VariantList[i] = languageTag.VariantList[i].ToLowerInvariant();
        }

        // Extensions
        foreach (LanguageTag.ExtensionTag extension in languageTag.ExtensionList)
        {
            extension.Prefix = char.ToLowerInvariant(extension.Prefix);
            for (int i = 0; i < extension.TagList.Count; i++)
            {
                extension.TagList[i] = extension.TagList[i].ToLowerInvariant();
            }
        }

        // Private use
        for (int i = 0; i < languageTag.PrivateUse.TagList.Count; i++)
        {
            languageTag.PrivateUse.TagList[i] = languageTag
                .PrivateUse.TagList[i]
                .ToLowerInvariant();
        }
    }

    private static void Sort(LanguageTag languageTag)
    {
        // Sort variants
        languageTag.VariantList.Sort();

        // Sort extensions by prefix
        languageTag.ExtensionList.Sort((x, y) => x.Prefix.CompareTo(y.Prefix));

        // Sort extensions and private use tags
        languageTag.ExtensionList.ForEach(extension => extension.TagList.Sort());
        languageTag.PrivateUse.TagList.Sort();
    }

    private static bool ValidateLanguage(string tag) =>
        // 2 to 8 chars
        !string.IsNullOrEmpty(tag) && tag.Length is >= 2 and <= 8;

    private bool ParseLanguage()
    {
        // Primary Language Subtag
        // https://www.rfc-editor.org/rfc/rfc5646#section-2.2.1

        /*
        language      = 2*3ALPHA            ; shortest ISO 639 code
                        ["-" extlang]       ; sometimes followed by
                                            ; extended language subtags
                      / 4ALPHA              ; or reserved for future use
                      / 5*8ALPHA            ; or registered language subtag
        */

        // 2 chars ISO 639-1
        // 3 chars ISO 639-2/3/5
        // qaa - qtz reserved private use
        // 4 chars reserved future use
        // 5 - 8 chars registered use
        if (!ValidateLanguage(_tagList[0]))
        {
            return false;
        }

        _languageTag.Language = _tagList[0];
        _tagList.RemoveAt(0);

        // Done
        return true;
    }

    private static bool ValidateExtendedLanguage(string tag) =>
        // 3 chars
        !string.IsNullOrEmpty(tag) && tag.Length == 3;

    private bool ParseExtendedLanguage()
    {
        // Extended Language Subtags
        // https://www.rfc-editor.org/rfc/rfc5646#section-2.2.2

        /*
        extlang       = 3ALPHA              ; selected ISO 639 codes
                        *2("-" 3ALPHA)      ; permanently reserved
        */

        // Extended language
        // 3 chars ISO 639-3/5
        // Language is 2 or 3 chars
        if (!ValidateExtendedLanguage(_tagList[0]) || _languageTag.Language.Length is < 2 or > 3)
        {
            // Done
            return true;
        }

        _languageTag.ExtendedLanguage = _tagList[0];
        _tagList.RemoveAt(0);

        // Done
        return true;
    }

    private static bool ValidateScript(string tag) =>
        // 4 chars
        !string.IsNullOrEmpty(tag) && tag.Length == 4;

    private bool ParseScript()
    {
        // Script Subtag
        // https://www.rfc-editor.org/rfc/rfc5646#section-2.2.3

        /*
        script        = 4ALPHA              ; ISO 15924 code
        */

        // Script
        // 4 chars ISO 15924
        // Qaaa - Qabx reserved private use
        if (!ValidateScript(_tagList[0]))
        {
            // Done
            return true;
        }

        _languageTag.Script = _tagList[0];
        _tagList.RemoveAt(0);

        // Done
        return true;
    }

    private static bool ValidateRegion(string tag) =>
        // 2 alpha or 3 digit
        !string.IsNullOrEmpty(tag)
        && (
            (tag.Length == 2 && tag.All(char.IsAsciiLetter))
            || (tag.Length == 3 && tag.All(char.IsAsciiDigit))
        );

    private bool ParseRegion()
    {
        // Region Subtag
        // https://www.rfc-editor.org/rfc/rfc5646#section-2.2.4

        /*
        region        = 2ALPHA              ; ISO 3166-1 code
                      / 3DIGIT              ; UN M.49 code
        */

        // TODO: RFC says theoretically 3 regions but practically only 1, e.g. sgn-BE-FR

        // Region
        // 2 alpha ISO 3166-1
        // AA, QM - QZ, XA - XZ, ZZ reserved private use
        // 3 digit UN M.49
        if (!ValidateRegion(_tagList[0]))
        {
            // Done
            return true;
        }

        _languageTag.Region = _tagList[0];
        _tagList.RemoveAt(0);

        // Done
        return true;
    }

    private static bool ValidateVariant(string tag) =>
        // 5 - 8 chars start with alpha
        // 4 - 8 chars start with digit
        !string.IsNullOrEmpty(tag)
        && (
            (tag.Length is >= 5 and <= 8 && char.IsAsciiLetter(tag[0]))
            || (tag.Length is >= 4 and <= 8 && char.IsAsciiDigit(tag[0]))
        );

    private bool ParseVariant()
    {
        // Variant Subtags
        // https://www.rfc-editor.org/rfc/rfc5646#section-2.2.5

        /*
        variant       = 5*8alphanum         ; registered variants
                      / (DIGIT 3alphanum)
        */

        // Multiple variants
        while (_tagList.Count > 0)
        {
            // Variant
            // begin with alpha 5 - 8 chars
            // begin with digit 4 = 8 chars
            if (!ValidateVariant(_tagList[0]))
            {
                // Done
                return true;
            }

            // Variant may not repeat
            if (_languageTag.VariantList.Contains(_tagList[0], StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            // Add variant tag
            _languageTag.VariantList.Add(_tagList[0]);
            _tagList.RemoveAt(0);
        }

        // Done
        return true;
    }

    private static bool ValidateExtensionPrefix(string tag) =>
        // 1 char not x
        !string.IsNullOrEmpty(tag)
        && tag.Length == 1
        && tag[0] != LanguageTag.PrivateUseTag.Prefix;

    private static bool ValidateExtension(string tag) =>
        // 2 - 8 chars
        !string.IsNullOrEmpty(tag) && tag.Length is >= 2 and <= 8;

    private bool ParseExtension()
    {
        // Extension Subtags
        // https://www.rfc-editor.org/rfc/rfc5646#section-2.2.6

        /*
        extension     = singleton 1*("-" (2*8alphanum))
                                            ; Single alphanumerics
                                            ; "x" reserved for private use
        */

        // Multiple extensions
        while (_tagList.Count > 0)
        {
            // Extension
            // 1 char (not x)
            if (!ValidateExtensionPrefix(_tagList[0]))
            {
                // Done
                return true;
            }

            // Prefix may not repeat
            if (_languageTag.ExtensionList.Any(item => item.Prefix == _tagList[0][0]))
            {
                return false;
            }

            LanguageTag.ExtensionTag extensionTag = new() { Prefix = _tagList[0][0] };
            _tagList.RemoveAt(0);

            // 1 or more tags remaining
            if (_tagList.Count == 0)
            {
                return false;
            }

            // 2 to 8 chars
            // Stop when no more tags match
            while (_tagList.Count > 0 && ValidateExtension(_tagList[0]))
            {
                // Tag may not repeat
                if (extensionTag.TagList.Contains(_tagList[0], StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }

                // Add extension tag
                extensionTag.TagList.Add(_tagList[0]);
                _tagList.RemoveAt(0);
            }

            // Must have some matches
            if (extensionTag.TagList.Count == 0)
            {
                return false;
            }

            // Add extension tag
            _languageTag.ExtensionList.Add(extensionTag);
        }

        // Done
        return true;
    }

    private static bool ValidatePrivateUsePrefix(string tag) =>
        // x
        !string.IsNullOrEmpty(tag)
        && tag.Length == 1
        && tag[0] == LanguageTag.PrivateUseTag.Prefix;

    private static bool ValidatePrivateUse(string tag) =>
        // 1 to 8 chars
        !string.IsNullOrEmpty(tag) && tag.Length is >= 1 and <= 8;

    private bool ParsePrivateUse()
    {
        // Private Use Subtags
        // https://www.rfc-editor.org/rfc/rfc5646#section-2.2.7

        /*
        privateuse    = "x" 1*("-" (1*8alphanum))
        */

        // x-[private]-[private]
        if (!ValidatePrivateUsePrefix(_tagList[0]))
        {
            // Done
            return true;
        }

        // Prefix may not repeat
        if (_languageTag.PrivateUse.TagList.Count > 0)
        {
            return false;
        }

        // x
        _tagList.RemoveAt(0);

        // 1 or more tags remaining
        if (_tagList.Count == 0)
        {
            return false;
        }

        // Read all tags
        while (_tagList.Count > 0)
        {
            // 1 to 8 chars
            // All remaining tags must be valid
            if (!ValidatePrivateUse(_tagList[0]))
            {
                return false;
            }

            // Tag may not repeat
            if (
                _languageTag.PrivateUse.TagList.Contains(
                    _tagList[0],
                    StringComparer.OrdinalIgnoreCase
                )
            )
            {
                return false;
            }

            // Add private use tag
            _languageTag.PrivateUse.TagList.Add(_tagList[0]);
            _tagList.RemoveAt(0);
        }

        // Must have some matches
        if (_languageTag.PrivateUse.TagList.Count == 0)
        {
            return false;
        }

        // Done
        return true;
    }

    public LanguageTag Parse(string languageTag)
    {
        // Parse the tag per RFC 5646 2.1
        // https://www.rfc-editor.org/rfc/rfc5646#section-2.1

        /*
        The syntax of the language tag in ABNF [RFC5234] is:
        Language-Tag  = langtag             ; normal language tags
                      / privateuse          ; private use tag
                      / grandfathered       ; grandfathered tags

        langtag       = language
                        ["-" script]
                        ["-" region]
                        *("-" variant)
                        *("-" extension)
                        ["-" privateuse]
        */

        // Init
        _languageTag = new();
        _tagList.Clear();

        // Must be non-empty
        if (string.IsNullOrEmpty(languageTag))
        {
            return null;
        }

        // Must be all ASCII
        if (languageTag.Any(c => !char.IsAscii(c)))
        {
            return null;
        }

        // Grandfathered
        languageTag = ParseGrandfathered(languageTag);

        // Split by -
        _tagList.AddRange([.. languageTag.Split('-')]);
        if (_tagList.Count == 0)
        {
            return null;
        }

        // All parts must be non-empty
        if (_tagList.Any(string.IsNullOrEmpty))
        {
            return null;
        }

        // Private use
        if (!ParsePrivateUse())
        {
            return null;
        }
        if (_tagList.Count == 0)
        {
            return _languageTag;
        }

        // Language
        if (!ParseLanguage())
        {
            return null;
        }
        if (_tagList.Count == 0)
        {
            return _languageTag;
        }

        // Extended language
        if (!ParseExtendedLanguage())
        {
            return null;
        }
        if (_tagList.Count == 0)
        {
            return _languageTag;
        }

        // Script
        if (!ParseScript())
        {
            return null;
        }
        if (_tagList.Count == 0)
        {
            return _languageTag;
        }

        // Region
        if (!ParseRegion())
        {
            return null;
        }
        if (_tagList.Count == 0)
        {
            return _languageTag;
        }

        // Variant
        if (!ParseVariant())
        {
            return null;
        }
        if (_tagList.Count == 0)
        {
            return _languageTag;
        }

        // Extension
        if (!ParseExtension())
        {
            return null;
        }
        if (_tagList.Count == 0)
        {
            return _languageTag;
        }

        // Private use
        if (!ParsePrivateUse())
        {
            return null;
        }
        if (_tagList.Count == 0)
        {
            return _languageTag;
        }

        // Should be done
        return null;
    }

    public LanguageTag Normalize(string languageTag)
    {
        LanguageTag parsedTag = Parse(languageTag);
        return parsedTag == null ? null : Normalize(parsedTag);
    }

    public LanguageTag Normalize(LanguageTag languageTag)
    {
        // Canonicalization of Language Tags
        // https://www.rfc-editor.org/rfc/rfc5646#section-4.5

        if (languageTag == null)
        {
            return null;
        }

        // Create a copy and do not modify the original
        LanguageTag normalizeTag = new(languageTag);

        // Language with preferred value
        // iw -> he
        // in -> id
        if (!string.IsNullOrEmpty(normalizeTag.Language))
        {
            // Type = Language, SubTag = iw, PreferredValue = he
            List<Rfc5646Data.Record> recordList =
            [
                .. _rfc5646.RecordList.Where(record =>
                    record.Type == Rfc5646Data.RecordType.Language
                    && !string.IsNullOrEmpty(record.PreferredValue)
                    && normalizeTag.Language.Equals(
                        record.SubTag,
                        StringComparison.OrdinalIgnoreCase
                    )
                ),
            ];
            Debug.Assert(recordList.Count is 0 or 1);
            if (recordList.Count == 1)
            {
                normalizeTag.Language = recordList[0].PreferredValue;
            }
        }

        // Extended language with preferred value
        // ar-afb -> afb
        // zh-yue -> yue
        if (
            !string.IsNullOrEmpty(normalizeTag.Language)
            && !string.IsNullOrEmpty(normalizeTag.ExtendedLanguage)
        )
        {
            // Type = ExtLanguage, Prefix = ar, SubTag = afb, PreferredValue = afb
            List<Rfc5646Data.Record> recordList =
            [
                .. _rfc5646.RecordList.Where(record =>
                    record.Type == Rfc5646Data.RecordType.ExtLanguage
                    && string.Equals(
                        record.SubTag,
                        normalizeTag.ExtendedLanguage,
                        StringComparison.OrdinalIgnoreCase
                    )
                    && record.Prefix.Any(prefix =>
                        prefix.Equals(normalizeTag.Language, StringComparison.OrdinalIgnoreCase)
                    )
                ),
            ];
            Debug.Assert(recordList.Count is 0 or 1);
            if (recordList.Count == 1)
            {
                normalizeTag.Language = recordList[0].PreferredValue;
                normalizeTag.ExtendedLanguage = null;
            }
        }

        // Redundant tags
        // zh-cmn-Hant -> cmn-Hant
        // zh-gan -> gan
        // sgn-CO -> csn

        // TODO: What to do with tags with no preferred value?
        // de-CH-1901 -> ?
        // iu-Latn -> ?
        string tagString = normalizeTag.ToString();
        if (tagString.Contains('-'))
        {
            // Type = Redundant, Tag = zh-cmn-Hant, PreferredValue = cmn-Hant
            List<Rfc5646Data.Record> recordList =
            [
                .. _rfc5646.RecordList.Where(record =>
                    record.Type == Rfc5646Data.RecordType.Redundant
                    && !string.IsNullOrEmpty(record.PreferredValue)
                    && tagString.StartsWith(record.Tag, StringComparison.OrdinalIgnoreCase)
                ),
            ];
            Debug.Assert(recordList.Count is 0 or 1);
            if (recordList.Count == 1)
            {
                // Replace the tag with the preferred value and re-parse the full tag
                tagString = tagString.Replace(
                    recordList[0].Tag,
                    recordList[0].PreferredValue,
                    StringComparison.OrdinalIgnoreCase
                );
                LanguageTag preferredTag = Parse(tagString);
                normalizeTag.Language = preferredTag.Language;
                normalizeTag.ExtendedLanguage = preferredTag.ExtendedLanguage;
                normalizeTag.Script = preferredTag.Script;
                normalizeTag.Region = preferredTag.Region;
            }
        }

        // Redundant script
        // af-Latn -> af
        // en-Latn -> en
        if (
            !string.IsNullOrEmpty(normalizeTag.Language)
            && !string.IsNullOrEmpty(normalizeTag.Script)
        )
        {
            // Type = Language, SubTag = en, SuppressScript = Latn
            List<Rfc5646Data.Record> recordList =
            [
                .. _rfc5646.RecordList.Where(record =>
                    record.Type == Rfc5646Data.RecordType.Language
                    && !string.IsNullOrEmpty(record.SuppressScript)
                    && normalizeTag.Language.Equals(
                        record.SubTag,
                        StringComparison.OrdinalIgnoreCase
                    )
                ),
            ];
            Debug.Assert(recordList.Count is 0 or 1);
            if (recordList.Count == 1)
            {
                normalizeTag.Script = null;
            }
        }

        // TODO: Any tags that have macrolanguage and not yet normalized?
        // "Scope": "macrolanguage", -> "zh-cmn-Hant" -> "cmn-Hant"

        // Set case and sort
        SetCase(normalizeTag);
        Sort(normalizeTag);

        // Done
        return normalizeTag;
    }

    public static bool Validate(LanguageTag languageTag)
    {
        // Classes of Conformance
        // https://www.rfc-editor.org/rfc/rfc5646#section-2.2.9

        if (languageTag == null)
        {
            return false;
        }

        // Validate tags
        if (!string.IsNullOrEmpty(languageTag.Language) && !ValidateLanguage(languageTag.Language))
        {
            return false;
        }
        if (
            !string.IsNullOrEmpty(languageTag.ExtendedLanguage)
            && !ValidateExtendedLanguage(languageTag.ExtendedLanguage)
        )
        {
            return false;
        }
        if (!string.IsNullOrEmpty(languageTag.Script) && !ValidateScript(languageTag.Script))
        {
            return false;
        }
        if (!string.IsNullOrEmpty(languageTag.Region) && !ValidateRegion(languageTag.Region))
        {
            return false;
        }
        if (languageTag.VariantList.Any(tag => !ValidateVariant(tag)))
        {
            return false;
        }
        if (
            languageTag.ExtensionList.Any(extension =>
                !ValidateExtensionPrefix(extension.Prefix.ToString())
                || extension.TagList.Any(tag => !ValidateExtension(tag))
            )
        )
        {
            return false;
        }
        if (languageTag.PrivateUse.TagList.Any(tag => !ValidatePrivateUse(tag)))
        {
            return false;
        }

        // No duplicate variants
        if (languageTag.VariantList.GroupBy(tag => tag).Any(group => group.Count() > 1))
        {
            return false;
        }

        // No duplicate extension prefixes
        if (
            languageTag
                .ExtensionList.GroupBy(extension => extension.Prefix)
                .Any(group => group.Count() > 1)
        )
        {
            return false;
        }

        // No duplicate extensions per prefix
        if (
            languageTag.ExtensionList.Any(extension =>
                extension.TagList.GroupBy(tag => tag).Any(group => group.Count() > 1)
            )
        )
        {
            return false;
        }

        // No duplicate private use tags
        if (languageTag.PrivateUse.TagList.GroupBy(tag => tag).Any(group => group.Count() > 1))
        {
            return false;
        }

        // No empty tags
        if (string.IsNullOrEmpty(languageTag.ToString()))
        {
            return false;
        }

        // Done
        return true;
    }
}
