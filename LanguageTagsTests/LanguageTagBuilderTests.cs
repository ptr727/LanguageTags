namespace ptr727.LanguageTags.Tests;

public sealed class LanguageTagBuilderTests : SingleInstanceFixture
{
    [Fact]
    public void Build_Pass()
    {
        // en-latn-gb-boont-r-extended-sequence-x-private
        LanguageTag languageTag = new LanguageTagBuilder()
            .Language("en")
            .Script("latn")
            .Region("gb")
            .VariantAdd("boont")
            .ExtensionAdd('r', ["extended", "sequence"])
            .PrivateUseAdd("private")
            .Build();
        _ = languageTag.Validate().Should().BeTrue();
        _ = languageTag.ToString().Should().Be("en-latn-gb-boont-r-extended-sequence-x-private");

        // en-a-bbb-x-a-ccc
        languageTag = new LanguageTagBuilder()
            .Language("en")
            .ExtensionAdd('a', ["bbb"])
            .PrivateUseAddRange(["a", "ccc"])
            .Build();
        _ = languageTag.Validate().Should().BeTrue();
        _ = languageTag.ToString().Should().Be("en-a-bbb-x-a-ccc");

        // en-b-ccc-a-bbb-aaa-x-ccc-a
        languageTag = new LanguageTagBuilder()
            .Language("en")
            .ExtensionAdd('b', ["ccc"])
            .ExtensionAdd('a', ["bbb", "aaa"])
            .PrivateUseAddRange(["ccc", "a"])
            .Build();
        _ = languageTag.Validate().Should().BeTrue();
        _ = languageTag.ToString().Should().Be("en-b-ccc-a-bbb-aaa-x-ccc-a");

        // zh
        languageTag = new LanguageTagBuilder().Language("zh").Build();
        _ = languageTag.Validate().Should().BeTrue();
        _ = languageTag.ToString().Should().Be("zh");

        // zh-yue
        languageTag = new LanguageTagBuilder().Language("zh").ExtendedLanguage("yue").Build();
        _ = languageTag.Validate().Should().BeTrue();
        _ = languageTag.ToString().Should().Be("zh-yue");

        // sr-latn
        languageTag = new LanguageTagBuilder().Language("sr").Script("latn").Build();
        _ = languageTag.Validate().Should().BeTrue();
        _ = languageTag.ToString().Should().Be("sr-latn");

        // x-all-private
        languageTag = new LanguageTagBuilder().PrivateUseAddRange(["all", "private"]).Build();
        _ = languageTag.Validate().Should().BeTrue();
        _ = languageTag.ToString().Should().Be("x-all-private");
    }

    [Fact]
    public void Normalize_Pass()
    {
        // en-Latn-GB-boont-r-extended-sequence-x-private
        LanguageTag? languageTag = new LanguageTagBuilder()
            .Language("en")
            .Script("latn")
            .Region("gb")
            .VariantAdd("boont")
            .ExtensionAdd('r', ["extended", "sequence"])
            .PrivateUseAdd("private")
            .Normalize();
        _ = languageTag.Should().NotBeNull();
        _ = languageTag.Validate().Should().BeTrue();
        _ = languageTag.ToString().Should().Be("en-GB-boont-r-extended-sequence-x-private");

        // sr-Latn
        languageTag = new LanguageTagBuilder().Language("sr").Script("latn").Normalize();
        _ = languageTag.Should().NotBeNull();
        _ = languageTag.Validate().Should().BeTrue();
        _ = languageTag.ToString().Should().Be("sr-Latn");

        // en-a-aaa-bbb-b-ccc-x-a-ccc
        languageTag = new LanguageTagBuilder()
            .Language("en")
            .ExtensionAdd('b', ["ccc"]) // Add b before a to force a sort
            .ExtensionAdd('a', ["bbb", "aaa"]) // Add bbb before aaa to force a sort
            .PrivateUseAddRange(["ccc", "a"]) // Add ccc before a to force a sort
            .Normalize();
        _ = languageTag.Should().NotBeNull();
        _ = languageTag.Validate().Should().BeTrue();
        _ = languageTag.ToString().Should().Be("en-a-aaa-bbb-b-ccc-x-a-ccc");
    }

    [Fact]
    public void Normalize_WithOptions_Pass()
    {
        Options options = new();

        // en-Latn-GB-boont-r-extended-sequence-x-private
        LanguageTag? languageTag = new LanguageTagBuilder()
            .Language("en")
            .Script("latn")
            .Region("gb")
            .VariantAdd("boont")
            .ExtensionAdd('r', ["extended", "sequence"])
            .PrivateUseAdd("private")
            .Normalize(options);
        _ = languageTag.Should().NotBeNull();
        _ = languageTag.Validate().Should().BeTrue();
        _ = languageTag.ToString().Should().Be("en-GB-boont-r-extended-sequence-x-private");
    }

    [Fact]
    public void Build_Fail()
    {
        // Must have something
        LanguageTag languageTag = new LanguageTagBuilder().Build();
        _ = languageTag.Validate().Should().BeFalse();

        // Language 2-4 chars
        languageTag = new LanguageTagBuilder().Language("a").Build();
        _ = languageTag.Validate().Should().BeFalse();
        languageTag = new LanguageTagBuilder().Language("abcdefghi").Build();
        _ = languageTag.Validate().Should().BeFalse();

        // Extended Language 3 chars
        languageTag = new LanguageTagBuilder().Language("en").ExtendedLanguage("ab").Build();
        _ = languageTag.Validate().Should().BeFalse();

        // Script 4 chars
        languageTag = new LanguageTagBuilder().Language("en").Script("abc").Build();
        _ = languageTag.Validate().Should().BeFalse();

        // Region 2-3 chars
        languageTag = new LanguageTagBuilder().Language("en").Region("a").Build();
        _ = languageTag.Validate().Should().BeFalse();

        // Variant, 4+ chars start digit, 5+ chars start alpha
        languageTag = new LanguageTagBuilder().Language("en").VariantAdd("abcd").Build();
        _ = languageTag.Validate().Should().BeFalse();
        languageTag = new LanguageTagBuilder().Language("en").VariantAdd("012").Build();
        _ = languageTag.Validate().Should().BeFalse();

        // Extension prefix 1 char, not x
        languageTag = new LanguageTagBuilder().Language("en").ExtensionAdd('x', ["abcd"]).Build();
        _ = languageTag.Validate().Should().BeFalse();

        // Extension tags must not be whitespace
        languageTag = new LanguageTagBuilder().Language("en").ExtensionAdd('a', [" "]).Build();
        _ = languageTag.Validate().Should().BeFalse();
    }

    [Fact]
    public void VariantAddRange_AddsMultipleVariants()
    {
        LanguageTag languageTag = new LanguageTagBuilder()
            .Language("en")
            .VariantAddRange(["variant1", "variant2", "variant3"])
            .Build();

        _ = languageTag.Variants.Length.Should().Be(3);
        _ = languageTag.Variants[0].Should().Be("variant1");
        _ = languageTag.Variants[1].Should().Be("variant2");
        _ = languageTag.Variants[2].Should().Be("variant3");
    }

    [Fact]
    public void VariantAddRange_ThrowsOnNull()
    {
        LanguageTagBuilder builder = new();
        _ = Assert
            .Throws<ArgumentNullException>(() => builder.VariantAddRange(null!))
            .Should()
            .NotBeNull();
    }

    [Fact]
    public void ExtensionAdd_ThrowsOnNull()
    {
        LanguageTagBuilder builder = new();
        _ = Assert
            .Throws<ArgumentNullException>(() => builder.ExtensionAdd('u', null!))
            .Should()
            .NotBeNull();
    }

    [Fact]
    public void ExtensionAdd_ThrowsOnEmpty()
    {
        LanguageTagBuilder builder = new();
        _ = Assert
            .Throws<ArgumentException>(() => builder.ExtensionAdd('u', []))
            .Should()
            .NotBeNull();
    }

    [Fact]
    public void PrivateUseAddRange_ThrowsOnNull()
    {
        LanguageTagBuilder builder = new();
        _ = Assert
            .Throws<ArgumentNullException>(() => builder.PrivateUseAddRange(null!))
            .Should()
            .NotBeNull();
    }
}
