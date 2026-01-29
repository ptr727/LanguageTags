using System;
using AwesomeAssertions;
using Xunit;

namespace ptr727.LanguageTags.Tests;

public sealed class LanguageLookupTests
{
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
    [InlineData("custom_iso", "custom_ietf")]
    public void GetIetfFromIso(string iso, string ietf)
    {
        LanguageLookup languageLookup = new();
        languageLookup.Overrides.Add(("custom_ietf", "custom_iso"));
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
    [InlineData("custom_ietf", "custom_iso")]
    public void GetIsoFromIetf(string ietf, string iso)
    {
        LanguageLookup languageLookup = new();
        languageLookup.Overrides.Add(("custom_ietf", "custom_iso"));
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
    public void IsMatch(string prefix, string tag, bool match)
    {
        LanguageLookup languageLookup = new();
        _ = languageLookup.IsMatch(prefix, tag).Should().Be(match);
    }

    [Theory]
    [InlineData("en-US", "en-us", true)]
    [InlineData("en-US", "EN-US", true)]
    [InlineData("zh-Hans", "zh-hans", true)]
    [InlineData("en-US", "en-GB", false)]
    [InlineData("en", "fr", false)]
    public void AreEquivalent_ComparesTagsCaseInsensitive(
        string tag1,
        string tag2,
        bool expected
    ) => _ = LanguageLookup.AreEquivalent(tag1, tag2).Should().Be(expected);

    [Theory]
    [InlineData("en-latn-us", "en-US", true)] // Normalized tags match
    [InlineData("zh-cmn-Hans-CN", "cmn-Hans-CN", true)] // Normalized tags match
    [InlineData("en-US", "en-GB", false)] // Different regions
    [InlineData("en", "fr", false)] // Different languages
    public void AreEquivalentNormalized_NormalizesAndCompares(
        string tag1,
        string tag2,
        bool expected
    ) => _ = LanguageLookup.AreEquivalentNormalized(tag1, tag2).Should().Be(expected);

    [Fact]
    public void Overrides_CanBeModified()
    {
        LanguageLookup languageLookup = new();
        _ = languageLookup.Overrides.Should().NotBeNull();
        _ = languageLookup.Overrides.Count.Should().Be(0);

        languageLookup.Overrides.Add(("custom_ietf", "custom_iso"));
        _ = languageLookup.Overrides.Count.Should().Be(1);
    }

    [Fact]
    public void Undetermined_ConstantIsCorrect() =>
        _ = LanguageLookup.Undetermined.Should().Be("und");

    [Fact]
    public void IsMatch_ThrowsOnNullPrefix()
    {
        LanguageLookup languageLookup = new();
        _ = Assert
            .Throws<ArgumentNullException>(() => languageLookup.IsMatch(null!, "en-US"))
            .Should()
            .NotBeNull();
    }

    [Fact]
    public void IsMatch_ThrowsOnNullTag()
    {
        LanguageLookup languageLookup = new();
        _ = Assert
            .Throws<ArgumentNullException>(() => languageLookup.IsMatch("en", null!))
            .Should()
            .NotBeNull();
    }
}
