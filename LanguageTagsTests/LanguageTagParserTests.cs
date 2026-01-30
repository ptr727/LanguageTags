namespace ptr727.LanguageTags.Tests;

public class LanguageTagParserTests
{
    [Theory]
    [InlineData("en-latn-gb-boont-r-extended-sequence-x-private")]
    [InlineData("en-a-bbb-x-a-ccc")]
    [InlineData("zh-cmn-hant")]
    [InlineData("sgn-us")]
    [InlineData("en-latn")]
    [InlineData("x-all-private")]
    [InlineData("x-a-private")]
    public void Parse_Pass(string tag)
    {
        LanguageTag? languageTag = new LanguageTagParser().Parse(tag);
        _ = languageTag.Should().NotBeNull();
        _ = languageTag.Validate().Should().BeTrue();
        _ = languageTag.ToString().Should().Be(tag);
    }

    [Theory]
    [InlineData("en-gb-oed", "en-GB-oxendict")]
    [InlineData("i-navajo", "nv")]
    [InlineData("no-bok", "nb")]
    [InlineData("art-lojban", "jbo")]
    [InlineData("zh-min-nan", "nan")]
    public void Normalize_Grandfathered_Pass(string tag, string parsed)
    {
        LanguageTag? languageTag = new LanguageTagParser().Normalize(tag);
        _ = languageTag.Should().NotBeNull();
        _ = languageTag.Validate().Should().BeTrue();
        _ = languageTag.ToString().Should().Be(parsed);
    }

    [Theory]
    [InlineData("en-gb-oed", "en-GB-oxendict")]
    [InlineData("sl-afb-latn-005-nedis", "sl-afb-005-nedis")] // Suppress script
    [InlineData("ar-arb-latn-de-nedis-foobar", "arb-Latn-DE-foobar-nedis")]
    [InlineData(
        "en-latn-gb-boont-r-extended-sequence-x-private",
        "en-GB-boont-r-extended-sequence-x-private" // Suppress script
    )]
    public void Normalize_Case_Pass(string tag, string parsed)
    {
        LanguageTag? languageTag = new LanguageTagParser().Normalize(tag);
        _ = languageTag.Should().NotBeNull();
        _ = languageTag.Validate().Should().BeTrue();
        _ = languageTag.ToString().Should().Be(parsed);
    }

    [Theory]
    [InlineData("sl-afb-latn-005-nedis", "sl-afb-005-nedis")] // Suppress script
    [InlineData("ar-arb-latn-de-nedis-foobar", "arb-Latn-DE-foobar-nedis")]
    [InlineData(
        "en-latn-gb-boont-r-extended-sequence-x-private",
        "en-GB-boont-r-extended-sequence-x-private" // Suppress script
    )]
    public void Normalize_Object(string tag, string parsed)
    {
        LanguageTag? languageTag = new LanguageTagParser().Parse(tag);
        _ = languageTag.Should().NotBeNull();
        _ = languageTag.Validate().Should().BeTrue();
        _ = languageTag.ToString().Should().NotBe(parsed);

        LanguageTag? normalizeTag = new LanguageTagParser().Normalize(languageTag);
        _ = normalizeTag.Should().NotBeNull();
        _ = normalizeTag.Should().NotBe(languageTag);
        _ = normalizeTag.Validate().Should().BeTrue();
        _ = normalizeTag.ToString().Should().Be(parsed);
    }

    [Theory]
    [InlineData(
        "en-latn-gb-boont-b-bbbbb-aaaaa-a-ccccc-bbbbb-x-ddddd-bbbbb",
        "en-GB-boont-a-bbbbb-ccccc-b-aaaaa-bbbbb-x-bbbbb-ddddd" // Suppress script
    )]
    public void Normalize_Sort_Pass(string tag, string parsed)
    {
        LanguageTag? languageTag = new LanguageTagParser().Normalize(tag);
        _ = languageTag.Should().NotBeNull();
        _ = languageTag.Validate().Should().BeTrue();
        _ = languageTag.ToString().Should().Be(parsed);
    }

    [Theory]
    [InlineData("")] // Empty string
    [InlineData("i")] // Too short
    [InlineData("abcdefghi")] // Too long
    [InlineData("en--gb")] // Empty tag
    [InlineData("en-€-extension")] // Non-ASCII
    [InlineData("a-extension")] // Only start with x or grandfathered
    [InlineData("en-gb-x")] // Private must have parts
    [InlineData("x")] // Private missing
    [InlineData("x-abcdefghi")] // Private too long
    [InlineData("en-gb-abcde-abcde")] // Variant repeats
    [InlineData("en-gb-a-abcd-a-abcde")] // Extension prefix repeats
    [InlineData("en-gb-a-abcd-abcd")] // Extension tag repeats
    [InlineData("en-a- ")] // Extension tag whitespace
    [InlineData("en-gb-x-abcd-x-abcd")] // Private prefix repeats
    [InlineData("en-gb-x-abcd-abcd")] // Private tag repeats
    public void Parse_Fail(string tag) => _ = new LanguageTagParser().Parse(tag).Should().BeNull();

    [Theory]
    [InlineData("iw", "he")] // Type = Language, SubTag = iw, PreferredValue = he
    [InlineData("in", "id")] // Type = Language, SubTag = in, PreferredValue = id
    public void Normalize_Language(string tag, string normalized)
    {
        LanguageTag? languageTag = new LanguageTagParser().Normalize(tag);
        _ = languageTag.Should().NotBeNull();
        _ = languageTag.Validate().Should().BeTrue();
        _ = languageTag.ToString().Should().Be(normalized);
    }

    [Theory]
    [InlineData("ar-afb", "afb")] // Extended language, SubTag = afb, Prefix = ar, PreferredValue = afb
    [InlineData("zh-yue", "yue")] // Extended language, SubTag = yue, Prefix = zh, PreferredValue = yue
    public void Normalize_Extended_Language(string tag, string normalized)
    {
        LanguageTag? languageTag = new LanguageTagParser().Normalize(tag);
        _ = languageTag.Should().NotBeNull();
        _ = languageTag.Validate().Should().BeTrue();
        _ = languageTag.ToString().Should().Be(normalized);
    }

    [Theory]
    [InlineData("zh-cmn-hant", "cmn-Hant")] // Type = Redundant, Tag = zh-cmn-Hant, PreferredValue = cmn-Hant
    [InlineData("sgn-us", "ase")] // Type = Redundant, Tag = sgn-US, PreferredValue = ase
    [InlineData("sgn-br", "bzs")] // Type = Redundant, Tag = sgn-BR, PreferredValue = bzs
    [InlineData("de-ch-1901", "de-CH-1901")] // Type = Redundant, Tag = de-CH-1901, PreferredValue = ?
    [InlineData("iu-latn", "iu-Latn")] // Type = Redundant, Tag = iu-Latn, PreferredValue = ?
    public void Normalize_Redundant(string tag, string normalized)
    {
        LanguageTag? languageTag = new LanguageTagParser().Normalize(tag);
        _ = languageTag.Should().NotBeNull();
        _ = languageTag.Validate().Should().BeTrue();
        _ = languageTag.ToString().Should().Be(normalized);
    }

    [Theory]
    [InlineData("af-latn", "af")] // Type = Language, SubTag = af, SuppressScript = Latn
    [InlineData("en-latn", "en")] // Type = Language, SubTag = en, SuppressScript = Latn
    public void Normalize_Suppress_Script(string tag, string normalized)
    {
        LanguageTag? languageTag = new LanguageTagParser().Normalize(tag);
        _ = languageTag.Should().NotBeNull();
        _ = languageTag.Validate().Should().BeTrue();
        _ = languageTag.ToString().Should().Be(normalized);
    }
}
