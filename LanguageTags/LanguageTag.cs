using System.Diagnostics.CodeAnalysis;

namespace ptr727.LanguageTags;

/// <summary>
/// Represents a language tag conforming to RFC 5646 / BCP 47.
/// </summary>
public sealed class LanguageTag : IEquatable<LanguageTag>
{
    internal LanguageTag()
    {
        Language = string.Empty;
        ExtendedLanguage = string.Empty;
        Script = string.Empty;
        Region = string.Empty;
        _variants = [];
        _extensions = [];
        PrivateUse = new PrivateUseTag();
    }

    internal LanguageTag(LanguageTag languageTag)
    {
        ArgumentNullException.ThrowIfNull(languageTag);
        Language = languageTag.Language;
        ExtendedLanguage = languageTag.ExtendedLanguage;
        Script = languageTag.Script;
        Region = languageTag.Region;
        _variants = [.. languageTag._variants];
        _extensions = [.. languageTag._extensions];
        PrivateUse = languageTag.PrivateUse;
    }

    /// <summary>
    /// Gets or sets the primary language subtag (ISO 639 language code).
    /// </summary>
    public string Language { get; internal set; }

    /// <summary>
    /// Gets or sets the extended language subtag.
    /// </summary>
    public string ExtendedLanguage { get; internal set; }

    /// <summary>
    /// Gets or sets the script subtag (ISO 15924 script code).
    /// </summary>
    public string Script { get; internal set; }

    /// <summary>
    /// Gets or sets the region subtag (ISO 3166-1 country code or UN M.49 region code).
    /// </summary>
    public string Region { get; internal set; }

    /// <summary>
    /// Gets the list of variant subtags.
    /// </summary>
    public ImmutableArray<string> Variants => [.. _variants];
    internal List<string> _variants { get; init; }

    /// <summary>
    /// Gets the list of extension subtags.
    /// </summary>
    public ImmutableArray<ExtensionTag> Extensions => [.. _extensions];
    internal List<ExtensionTag> _extensions { get; init; }

    /// <summary>
    /// Gets the private use subtag.
    /// </summary>
    public PrivateUseTag PrivateUse { get; internal set; }

    /// <summary>
    /// Parses a language tag string into a LanguageTag object.
    /// </summary>
    /// <param name="tag">The language tag string to parse (e.g., "en-US", "zh-Hans-CN").</param>
    /// <returns>A parsed and normalized LanguageTag object, or null if parsing fails.</returns>
    public static LanguageTag? Parse(string tag) => new LanguageTagParser().Parse(tag);

    /// <summary>
    /// Parses a language tag string into a LanguageTag object using the specified options.
    /// </summary>
    /// <param name="tag">The language tag string to parse (e.g., "en-US", "zh-Hans-CN").</param>
    /// <param name="options">The options used to configure logging.</param>
    /// <returns>A parsed and normalized LanguageTag object, or null if parsing fails.</returns>
    public static LanguageTag? Parse(string tag, Options? options) =>
        new LanguageTagParser(options).Parse(tag);

    /// <summary>
    /// Parses a language tag string, returning a default tag if parsing fails.
    /// </summary>
    /// <param name="tag">The language tag string to parse.</param>
    /// <param name="defaultTag">The default tag to return if parsing fails (defaults to "und").</param>
    /// <returns>The parsed tag or the default tag.</returns>
    public static LanguageTag ParseOrDefault(string tag, LanguageTag? defaultTag = null)
    {
        LanguageTag? parsed = Parse(tag);
        return parsed ?? defaultTag ?? Parse(LanguageLookup.Undetermined)!;
    }

    /// <summary>
    /// Parses and normalizes a language tag string.
    /// </summary>
    /// <param name="tag">The language tag string.</param>
    /// <returns>A normalized language tag or null if parsing/normalization fails.</returns>
    public static LanguageTag? ParseAndNormalize(string tag) => Parse(tag)?.Normalize();

    /// <summary>
    /// Parses and normalizes a language tag string using the specified options.
    /// </summary>
    /// <param name="tag">The language tag string.</param>
    /// <param name="options">The options used to configure logging.</param>
    /// <returns>A normalized language tag or null if parsing/normalization fails.</returns>
    public static LanguageTag? ParseAndNormalize(string tag, Options? options) =>
        Parse(tag, options)?.Normalize(options);

    /// <summary>
    /// Tries to parse a language tag string into a LanguageTag object.
    /// </summary>
    /// <param name="tag">The language tag string to parse (e.g., "en-US", "zh-Hans-CN").</param>
    /// <param name="result">When this method returns, contains the parsed LanguageTag if successful, or null if parsing fails.</param>
    /// <returns>true if the tag was successfully parsed; otherwise, false.</returns>
    public static bool TryParse(string tag, [NotNullWhen(true)] out LanguageTag? result)
    {
        result = Parse(tag);
        return result != null;
    }

    /// <summary>
    /// Tries to parse a language tag string into a LanguageTag object using the specified options.
    /// </summary>
    /// <param name="tag">The language tag string to parse (e.g., "en-US", "zh-Hans-CN").</param>
    /// <param name="result">When this method returns, contains the parsed LanguageTag if successful, or null if parsing fails.</param>
    /// <param name="options">The options used to configure logging.</param>
    /// <returns>true if the tag was successfully parsed; otherwise, false.</returns>
    public static bool TryParse(
        string tag,
        [NotNullWhen(true)] out LanguageTag? result,
        Options? options
    )
    {
        result = Parse(tag, options);
        return result != null;
    }

    /// <summary>
    /// Creates a new LanguageTagBuilder for fluent construction of language tags.
    /// </summary>
    /// <returns>A new LanguageTagBuilder instance.</returns>
    public static LanguageTagBuilder CreateBuilder() => new();

    /// <summary>
    /// Validates this language tag.
    /// </summary>
    /// <returns>true if the tag is valid; otherwise, false.</returns>
    public bool Validate() => LanguageTagParser.Validate(this);

    /// <summary>
    /// Gets whether this language tag is valid according to RFC 5646 rules.
    /// </summary>
    public bool IsValid => Validate();

    /// <summary>
    /// Normalizes this language tag according to RFC 5646 rules.
    /// </summary>
    /// <returns>A normalized copy of this language tag.</returns>
    public LanguageTag? Normalize() => new LanguageTagParser().Normalize(this);

    /// <summary>
    /// Normalizes this language tag according to RFC 5646 rules using the specified options.
    /// </summary>
    /// <param name="options">The options used to configure logging.</param>
    /// <returns>A normalized copy of this language tag.</returns>
    public LanguageTag? Normalize(Options? options) =>
        new LanguageTagParser(options).Normalize(this);

    /// <summary>
    /// Converts this language tag to its string representation.
    /// </summary>
    /// <returns>A string representation of the language tag (e.g., "en-US", "zh-Hans-CN").</returns>
    public override string ToString()
    {
        StringBuilder stringBuilder = new();
        if (!string.IsNullOrEmpty(Language))
        {
            _ = stringBuilder.Append(Language);
        }
        if (!string.IsNullOrEmpty(ExtendedLanguage))
        {
            _ = stringBuilder.Append('-').Append(ExtendedLanguage);
        }
        if (!string.IsNullOrEmpty(Script))
        {
            _ = stringBuilder.Append('-').Append(Script);
        }
        if (!string.IsNullOrEmpty(Region))
        {
            _ = stringBuilder.Append('-').Append(Region);
        }
        if (_variants.Count > 0)
        {
            _ = stringBuilder.Append(
                CultureInfo.InvariantCulture,
                $"-{string.Join('-', _variants)}"
            );
        }
        if (_extensions.Count > 0)
        {
            _ = stringBuilder.Append(
                CultureInfo.InvariantCulture,
                $"-{string.Join('-', _extensions.Select(item => item.ToString()))}"
            );
        }
        if (!PrivateUse.Tags.IsEmpty)
        {
            if (stringBuilder.Length > 0)
            {
                _ = stringBuilder.Append('-');
            }
            _ = stringBuilder.Append(PrivateUse.ToString());
        }
        return stringBuilder.ToString();
    }

    /// <summary>
    /// Determines whether this instance is equal to another <see cref="LanguageTag"/>.
    /// </summary>
    /// <param name="other">The <see cref="LanguageTag"/> to compare with.</param>
    /// <returns>true if the tags are equal (case-insensitive); otherwise, false.</returns>
    public bool Equals(LanguageTag? other) =>
        other is not null
        && (
            ReferenceEquals(this, other)
            || string.Equals(ToString(), other.ToString(), StringComparison.OrdinalIgnoreCase)
        );

    /// <summary>
    /// Determines whether this instance is equal to another object.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>true if the objects are equal; otherwise, false.</returns>
    public override bool Equals(object? obj) => Equals(obj as LanguageTag);

    /// <summary>
    /// Returns the hash code for this language tag.
    /// </summary>
    /// <returns>A hash code for the current language tag.</returns>
    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(ToString());

    /// <summary>
    /// Determines whether two language tags are equal.
    /// </summary>
    /// <param name="left">The first language tag to compare.</param>
    /// <param name="right">The second language tag to compare.</param>
    /// <returns>true if the tags are equal; otherwise, false.</returns>
    public static bool operator ==(LanguageTag? left, LanguageTag? right) =>
        left?.Equals(right) ?? (right is null);

    /// <summary>
    /// Determines whether two language tags are not equal.
    /// </summary>
    /// <param name="left">The first language tag to compare.</param>
    /// <param name="right">The second language tag to compare.</param>
    /// <returns>true if the tags are not equal; otherwise, false.</returns>
    public static bool operator !=(LanguageTag? left, LanguageTag? right) => !(left == right);

    /// <summary>
    /// Creates a simple language tag with just a language code.
    /// </summary>
    /// <param name="language">The ISO 639 language code.</param>
    /// <returns>A new <see cref="LanguageTag"/> with the specified language.</returns>
    public static LanguageTag FromLanguage(string language) =>
        CreateBuilder().Language(language).Build();

    /// <summary>
    /// Creates a language tag with language and region.
    /// </summary>
    /// <param name="language">The ISO 639 language code.</param>
    /// <param name="region">The ISO 3166-1 country code or UN M.49 region code.</param>
    /// <returns>A new <see cref="LanguageTag"/> with the specified language and region.</returns>
    public static LanguageTag FromLanguageRegion(string language, string region) =>
        CreateBuilder().Language(language).Region(region).Build();

    /// <summary>
    /// Creates a language tag with language, script, and region.
    /// </summary>
    /// <param name="language">The ISO 639 language code.</param>
    /// <param name="script">The ISO 15924 script code.</param>
    /// <param name="region">The ISO 3166-1 country code or UN M.49 region code.</param>
    /// <returns>A new <see cref="LanguageTag"/> with the specified language, script, and region.</returns>
    public static LanguageTag FromLanguageScriptRegion(
        string language,
        string script,
        string region
    ) => CreateBuilder().Language(language).Script(script).Region(region).Build();
}

/// <summary>
/// Represents an extension subtag in a language tag.
/// </summary>
/// <param name="Prefix">The single-character prefix for the extension (e.g., 'u' for Unicode extensions).</param>
/// <param name="Tags">The list of extension subtag values.</param>
public sealed record ExtensionTag(char Prefix, ImmutableArray<string> Tags)
{
    /// <summary>
    /// Creates an extension tag with a prefix and tags from an enumerable collection.
    /// </summary>
    /// <param name="prefix">The single-character prefix.</param>
    /// <param name="tags">The extension subtag values.</param>
    public ExtensionTag(char prefix, IEnumerable<string> tags)
        : this(prefix, [.. tags]) { }

    /// <summary>
    /// Creates an empty extension tag.
    /// </summary>
    public ExtensionTag()
        : this('\0', []) { }

    /// <summary>
    /// Converts this extension tag to its string representation.
    /// </summary>
    /// <returns>A string representation of the extension tag (e.g., "u-ca-buddhist").</returns>
    public override string ToString() =>
        Tags.IsEmpty ? string.Empty : $"{Prefix}-{string.Join('-', Tags)}";

    internal ExtensionTag Normalize() =>
        this with
        {
            Prefix = char.ToLowerInvariant(Prefix),
            Tags = [.. Tags.Select(t => t.ToLowerInvariant()).OrderBy(t => t)],
        };

    /// <summary>
    /// Determines whether this instance is equal to another <see cref="ExtensionTag"/>.
    /// </summary>
    /// <param name="other">The <see cref="ExtensionTag"/> to compare with.</param>
    /// <returns>true if the extension tags are equal; otherwise, false.</returns>
    public bool Equals(ExtensionTag? other) =>
        ReferenceEquals(this, other)
        || (
            other is not null
            && char.ToLowerInvariant(Prefix) == char.ToLowerInvariant(other.Prefix)
            && Tags.SequenceEqual(other.Tags, StringComparer.OrdinalIgnoreCase)
        );

    /// <summary>
    /// Returns the hash code for this extension tag.
    /// </summary>
    /// <returns>A hash code for the current extension tag.</returns>
    public override int GetHashCode()
    {
        HashCode hashCode = new();
        hashCode.Add(char.ToLowerInvariant(Prefix));
        foreach (string tag in Tags)
        {
            hashCode.Add(tag, StringComparer.OrdinalIgnoreCase);
        }

        return hashCode.ToHashCode();
    }
}

/// <summary>
/// Represents a private use subtag in a language tag.
/// </summary>
/// <param name="Tags">The list of private use subtag values.</param>
public sealed record PrivateUseTag(ImmutableArray<string> Tags)
{
    /// <summary>
    /// The prefix character for private use subtags ('x').
    /// </summary>
    public const char Prefix = 'x';

    /// <summary>
    /// Creates a private use tag from an enumerable collection.
    /// </summary>
    /// <param name="tags">The private use subtag values.</param>
    public PrivateUseTag(IEnumerable<string> tags)
        : this([.. tags]) { }

    /// <summary>
    /// Creates an empty private use tag.
    /// </summary>
    public PrivateUseTag()
        : this([]) { }

    /// <summary>
    /// Converts this private use tag to its string representation.
    /// </summary>
    /// <returns>A string representation of the private use tag (e.g., "x-private").</returns>
    public override string ToString() =>
        Tags.IsEmpty ? string.Empty : $"{Prefix}-{string.Join('-', Tags)}";

    internal PrivateUseTag Normalize() =>
        this with
        {
            Tags = [.. Tags.Select(t => t.ToLowerInvariant()).OrderBy(t => t)],
        };

    /// <summary>
    /// Determines whether this instance is equal to another <see cref="PrivateUseTag"/>.
    /// </summary>
    /// <param name="other">The <see cref="PrivateUseTag"/> to compare with.</param>
    /// <returns>true if the private use tags are equal; otherwise, false.</returns>
    public bool Equals(PrivateUseTag? other) =>
        ReferenceEquals(this, other)
        || (other is not null && Tags.SequenceEqual(other.Tags, StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Returns the hash code for this private use tag.
    /// </summary>
    /// <returns>A hash code for the current private use tag.</returns>
    public override int GetHashCode()
    {
        HashCode hashCode = new();
        foreach (string tag in Tags)
        {
            hashCode.Add(tag, StringComparer.OrdinalIgnoreCase);
        }

        return hashCode.ToHashCode();
    }
}
