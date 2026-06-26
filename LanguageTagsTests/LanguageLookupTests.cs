namespace ptr727.LanguageTags.Tests;

public sealed class LanguageLookupTests : SingleInstanceFixture
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
    [InlineData("es-419", "es-MX", true)] // Mexico is in Latin America
    [InlineData("es-MX", "es-419", false)] // Directional, not the reverse
    [InlineData("es-419", "es-ES", false)] // Spain is not in Latin America
    [InlineData("es-005", "es-AR", true)] // Argentina is in South America
    [InlineData("es-013", "es-MX", true)] // Mexico is in Central America
    [InlineData("es-419", "es-013", true)] // Central America is within Latin America
    [InlineData("es-419", "es-419", true)] // Identity still matches
    [InlineData("es-419", "fr-MX", false)] // Language must match
    [InlineData("es-001", "es-MX", true)] // World contains every region
    [InlineData("en", "en-US", true)] // Plain matching still works
    [InlineData("es-Latn-419", "es-Latn-MX", true)] // Script is preserved
    [InlineData("es-419", "es-MX-nedis", true)] // Broad group matches a more specific variant
    [InlineData("es-419-nedis", "es-MX", false)] // Prefix variant must still match, no false positive
    public void IsMatch_RegionContainment(string prefix, string tag, bool match)
    {
        LanguageLookup languageLookup = new();
        _ = languageLookup.IsMatch(prefix, tag, true).Should().Be(match);
    }

    [Theory]
    [InlineData("es-419", "es-MX")] // Containment is opt-in, plain matching does not expand regions
    [InlineData("es-005", "es-AR")]
    public void IsMatch_RegionContainment_Disabled_DoesNotMatch(string prefix, string tag)
    {
        LanguageLookup languageLookup = new();
        _ = languageLookup.IsMatch(prefix, tag).Should().BeFalse();
        _ = languageLookup.IsMatch(prefix, tag, false).Should().BeFalse();
    }

    [Fact]
    public void ExpandRegion_Country_ReturnsContainmentChain()
    {
        LanguageLookup languageLookup = new();
        List<string> expanded = [.. languageLookup.ExpandRegion("es-MX")];

        // The original tag is always first, followed by the containing UN M.49 groups
        _ = expanded[0].Should().Be("es-MX");
        _ = expanded.Should().Contain(["es-MX", "es-013", "es-419", "es-019", "es-001"]);
    }

    [Theory]
    [InlineData("es")] // No region to expand
    [InlineData("es-MX-x-foo")] // Region present, private use preserved
    public void ExpandRegion_AlwaysIncludesOriginal(string tag)
    {
        LanguageLookup languageLookup = new();
        _ = languageLookup.ExpandRegion(tag).Should().Contain(tag);
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
