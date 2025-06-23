using System.Collections.Generic;

namespace ptr727.LanguageTags;

public class LanguageTagBuilder
{
    private readonly LanguageTag _languageTag = new();

    public LanguageTagBuilder Language(string value)
    {
        _languageTag.Language = value;
        return this;
    }

    public LanguageTagBuilder ExtendedLanguage(string value)
    {
        _languageTag.ExtendedLanguage = value;
        return this;
    }

    public LanguageTagBuilder Script(string value)
    {
        _languageTag.Script = value;
        return this;
    }

    public LanguageTagBuilder Region(string value)
    {
        _languageTag.Region = value;
        return this;
    }

    public LanguageTagBuilder VariantAdd(string value)
    {
        _languageTag.VariantList.Add(value);
        return this;
    }

    public LanguageTagBuilder VariantAddRange(List<string> values)
    {
        _languageTag.VariantList.AddRange(values);
        return this;
    }

    // TODO: Create ExtensionsBuilder
    public LanguageTagBuilder ExtensionAdd(char prefix, List<string> values)
    {
        _languageTag.ExtensionList.Add(new() { Prefix = prefix, TagList = values });
        return this;
    }

    public LanguageTagBuilder PrivateUseAdd(string value)
    {
        _languageTag.PrivateUse.TagList.Add(value);
        return this;
    }

    public LanguageTagBuilder PrivateUseAddRange(List<string> values)
    {
        _languageTag.PrivateUse.TagList.AddRange(values);
        return this;
    }

    public LanguageTag Build() => _languageTag;

    public LanguageTag Normalize() => new LanguageTagParser().Normalize(_languageTag);
}
