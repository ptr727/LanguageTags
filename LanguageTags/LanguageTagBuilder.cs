namespace ptr727.LanguageTags;

/// <summary>
/// Provides a fluent API for building RFC 5646 / BCP 47 language tags.
/// </summary>
public sealed class LanguageTagBuilder
{
    private readonly LanguageTag _languageTag = new();

    /// <summary>
    /// Sets the primary language subtag.
    /// </summary>
    /// <param name="value">The language code (e.g., "en", "zh").</param>
    /// <returns>The builder instance for method chaining.</returns>
    public LanguageTagBuilder Language(string value)
    {
        _languageTag.Language = value;
        return this;
    }

    /// <summary>
    /// Sets the extended language subtag.
    /// </summary>
    /// <param name="value">The extended language code (e.g., "yue" for zh-yue).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public LanguageTagBuilder ExtendedLanguage(string value)
    {
        _languageTag.ExtendedLanguage = value;
        return this;
    }

    /// <summary>
    /// Sets the script subtag.
    /// </summary>
    /// <param name="value">The ISO 15924 script code (e.g., "Hans", "Latn").</param>
    /// <returns>The builder instance for method chaining.</returns>
    public LanguageTagBuilder Script(string value)
    {
        _languageTag.Script = value;
        return this;
    }

    /// <summary>
    /// Sets the region subtag.
    /// </summary>
    /// <param name="value">The ISO 3166-1 country code or UN M.49 region code (e.g., "US", "CN").</param>
    /// <returns>The builder instance for method chaining.</returns>
    public LanguageTagBuilder Region(string value)
    {
        _languageTag.Region = value;
        return this;
    }

    /// <summary>
    /// Adds a variant subtag.
    /// </summary>
    /// <param name="value">The variant code to add.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public LanguageTagBuilder VariantAdd(string value)
    {
        _languageTag._variants.Add(value);
        return this;
    }

    /// <summary>
    /// Adds multiple variant subtags.
    /// </summary>
    /// <param name="values">The variant codes to add.</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values"/> is null.</exception>
    public LanguageTagBuilder VariantAddRange(IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        _languageTag._variants.AddRange(values);
        return this;
    }

    /// <summary>
    /// Adds an extension subtag with the specified prefix and values.
    /// </summary>
    /// <param name="prefix">The single-character extension prefix (e.g., 'u' for Unicode extensions).</param>
    /// <param name="values">The extension values.</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values"/> is empty.</exception>
    public LanguageTagBuilder ExtensionAdd(char prefix, IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        ImmutableArray<string> tags = [.. values];
        if (tags.IsEmpty)
        {
            throw new ArgumentException("Extension tags cannot be empty.", nameof(values));
        }

        _languageTag._extensions.Add(new ExtensionTag(prefix, tags));
        return this;
    }

    /// <summary>
    /// Adds a private use subtag.
    /// </summary>
    /// <param name="value">The private use value to add.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public LanguageTagBuilder PrivateUseAdd(string value)
    {
        List<string> tags = [.. _languageTag.PrivateUse.Tags, value];
        _languageTag.PrivateUse = new PrivateUseTag(tags);
        return this;
    }

    /// <summary>
    /// Adds multiple private use subtags.
    /// </summary>
    /// <param name="values">The private use values to add.</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values"/> is null.</exception>
    public LanguageTagBuilder PrivateUseAddRange(IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        List<string> tags = [.. _languageTag.PrivateUse.Tags, .. values];
        _languageTag.PrivateUse = new PrivateUseTag(tags);
        return this;
    }

    /// <summary>
    /// Builds and returns the constructed language tag.
    /// </summary>
    /// <returns>The constructed <see cref="LanguageTag"/>.</returns>
    public LanguageTag Build() => _languageTag;

    /// <summary>
    /// Builds and normalizes the constructed language tag according to RFC 5646 rules.
    /// </summary>
    /// <returns>A normalized <see cref="LanguageTag"/> or null if normalization fails.</returns>
    public LanguageTag? Normalize() => new LanguageTagParser().Normalize(_languageTag);

    /// <summary>
    /// Builds and normalizes the constructed language tag according to RFC 5646 rules using the specified options.
    /// </summary>
    /// <param name="options">The options used to configure logging.</param>
    /// <returns>A normalized <see cref="LanguageTag"/> or null if normalization fails.</returns>
    public LanguageTag? Normalize(Options? options) =>
        new LanguageTagParser(options).Normalize(_languageTag);
}
