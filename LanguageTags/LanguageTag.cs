using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ptr727.LanguageTags;

public class LanguageTag
{
    public LanguageTag()
    {
        VariantList = [];
        ExtensionList = [];
        PrivateUse = new PrivateUseTag();
    }

    public LanguageTag(LanguageTag languageTag)
    {
        Language = languageTag.Language;
        ExtendedLanguage = languageTag.ExtendedLanguage;
        Script = languageTag.Script;
        Region = languageTag.Region;
        VariantList = [.. languageTag.VariantList];
        ExtensionList = [];
        languageTag.ExtensionList.ForEach(extension =>
        {
            ExtensionList.Add(new ExtensionTag(extension));
        });
        PrivateUse = new PrivateUseTag(languageTag.PrivateUse);
    }

    public class ExtensionTag
    {
        public ExtensionTag() => TagList = [];

        public ExtensionTag(ExtensionTag extensionTag)
        {
            Prefix = extensionTag.Prefix;
            TagList = [.. extensionTag.TagList];
        }

        public char Prefix { get; set; }
        public List<string> TagList { get; init; }

        public override string ToString() => $"{Prefix}-{string.Join('-', TagList)}";
    }

    public class PrivateUseTag
    {
        public PrivateUseTag() => TagList = [];

        public PrivateUseTag(PrivateUseTag privateUseTag) => TagList = [.. privateUseTag.TagList];

        public const char Prefix = 'x';
        public List<string> TagList { get; }

        public override string ToString() => $"{Prefix}-{string.Join('-', TagList)}";
    }

    public string Language { get; set; }
    public string ExtendedLanguage { get; set; }
    public string Script { get; set; }
    public string Region { get; set; }
    public List<string> VariantList { get; }
    public List<ExtensionTag> ExtensionList { get; }
    public PrivateUseTag PrivateUse { get; }

    public bool Validate() => LanguageTagParser.Validate(this);

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
        if (VariantList.Count > 0)
        {
            _ = stringBuilder.Append(
                CultureInfo.InvariantCulture,
                $"-{string.Join('-', VariantList)}"
            );
        }
        if (ExtensionList.Count > 0)
        {
            _ = stringBuilder.Append(
                CultureInfo.InvariantCulture,
                $"-{string.Join('-', ExtensionList.Select(item => item.ToString()))}"
            );
        }
        if (PrivateUse.TagList.Count > 0)
        {
            if (stringBuilder.Length > 0)
            {
                _ = stringBuilder.Append('-');
            }
            _ = stringBuilder.Append(PrivateUse.ToString());
        }
        return stringBuilder.ToString();
    }
}
