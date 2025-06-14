using AwesomeAssertions;
using Xunit;

namespace ptr727.LanguageTags.Tests;

public class LanguageLookupTests(Fixture fixture) : IClassFixture<Fixture>
{
    private readonly Fixture _fixture = fixture;

    [Theory]
    [InlineData("afr", "af")]
    [InlineData("ger", "de")]
    [InlineData("fre", "fr")]
    [InlineData("eng", "en")]
    [InlineData("dan", "da")]
    [InlineData("cpe", "cpe")]
    [InlineData("chi", "zh")]
    [InlineData("zho", "zh")]
    [InlineData("zxx", "zxx")]
    [InlineData("und", "und")]
    [InlineData("", "und")]
    [InlineData("xxx", "und")]
    public void Lookup_Ietf_From_Iso(string iso, string ietf)
    {
        LanguageLookup languageLookup = new();
        _ = languageLookup.GetIetfFromIso(iso).Should().Be(ietf);
    }

    [Theory]
    [InlineData("af", "afr")]
    [InlineData("de", "ger")]
    [InlineData("fr", "fre")]
    [InlineData("en", "eng")]
    [InlineData("cpe", "cpe")]
    [InlineData("zxx", "zxx")]
    [InlineData("zh", "chi")]
    [InlineData("zh-cmn-Hant", "chi")]
    [InlineData("cmn-Hant", "chi")]
    [InlineData("no-NO", "nor")]
    [InlineData("", "und")]
    [InlineData("und", "und")]
    [InlineData("xxx", "und")]
    public void Lookup_Iso_From_Ietf(string ietf, string iso)
    {
        LanguageLookup languageLookup = new();
        _ = languageLookup.GetIsoFromIetf(ietf).Should().Be(iso);
    }

    [Theory]
    [InlineData("en", "en", true)]
    [InlineData("en", "en-US", true)]
    [InlineData("en", "en-GB", true)]
    [InlineData("en-GB", "en-GB", true)]
    [InlineData("zh", "zh-cmn-Hant", true)]
    [InlineData("zh", "cmn-Hant", true)]
    [InlineData("sr-Latn", "sr-Latn-RS", true)]
    [InlineData("zh", "en", false)]
    [InlineData("zha", "zh-Hans", false)]
    [InlineData("zh-Hant", "zh-Hans", false)]
    public void Match_Language_Tags(string prefix, string tag, bool match)
    {
        LanguageLookup languageLookup = new();
        _ = languageLookup.IsMatch(prefix, tag).Should().Be(match);
    }
}
