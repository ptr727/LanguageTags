using System.Collections.Immutable;

namespace ptr727.LanguageTags.Tests;

public class LanguageTagTests : SingleInstanceFixture
{
    [Theory]
    [InlineData("en-US")]
    [InlineData("zh-Hans-CN")]
    [InlineData("en-latn-gb-boont-r-extended-sequence-x-private")]
    [InlineData("x-all-private")]
    public void Parse_Static_Pass(string tag)
    {
        LanguageTag? languageTag = LanguageTag.Parse(tag);
        _ = languageTag.Should().NotBeNull();
        _ = languageTag.Validate().Should().BeTrue();
        _ = languageTag.ToString().Should().Be(tag);
    }

    [Theory]
    [InlineData("")] // Empty string
    [InlineData("i")] // Too short
    [InlineData("abcdefghi")] // Too long
    [InlineData("en--gb")] // Empty tag
    [InlineData("en-€-extension")] // Non-ASCII
    [InlineData("a-extension")] // Only start with x or grandfathered
    [InlineData("en-gb-x")] // Private must have parts
    public void Parse_Static_ReturnsNull(string tag)
    {
        LanguageTag? languageTag = LanguageTag.Parse(tag);
        _ = languageTag.Should().BeNull();
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("zh-Hans-CN")]
    [InlineData("en-latn-gb-boont-r-extended-sequence-x-private")]
    public void TryParse_Success(string tag)
    {
        bool result = LanguageTag.TryParse(tag, out LanguageTag? languageTag);
        _ = result.Should().BeTrue();
        _ = languageTag.Should().NotBeNull();
        _ = languageTag.Validate().Should().BeTrue();
        _ = languageTag.ToString().Should().Be(tag);
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
    public void TryParse_Failure(string tag)
    {
        bool result = LanguageTag.TryParse(tag, out LanguageTag? languageTag);
        _ = result.Should().BeFalse();
        _ = languageTag.Should().BeNull();
    }

    [Fact]
    public void CreateBuilder_Pass()
    {
        LanguageTagBuilder builder = LanguageTag.CreateBuilder();
        _ = builder.Should().NotBeNull();

        LanguageTag languageTag = builder.Language("en").Region("US").Build();

        _ = languageTag.Should().NotBeNull();
        _ = languageTag.Validate().Should().BeTrue();
        _ = languageTag.ToString().Should().Be("en-US");
    }

    [Theory]
    [InlineData(
        "en-latn-gb-boont-r-extended-sequence-x-private",
        "en-GB-boont-r-extended-sequence-x-private"
    )]
    [InlineData("ar-arb-latn-de-nedis-foobar", "arb-Latn-DE-foobar-nedis")]
    public void Normalize_Instance_Pass(string tag, string normalized)
    {
        LanguageTag? languageTag = LanguageTag.Parse(tag);
        _ = languageTag.Should().NotBeNull();

        LanguageTag? normalizedTag = languageTag.Normalize();
        _ = normalizedTag.Should().NotBeNull();
        _ = normalizedTag.Validate().Should().BeTrue();
        _ = normalizedTag.ToString().Should().Be(normalized);
    }

    [Fact]
    public void CreateBuilder_Fluent_Pass()
    {
        LanguageTag languageTag = LanguageTag
            .CreateBuilder()
            .Language("en")
            .Script("latn")
            .Region("gb")
            .VariantAdd("boont")
            .ExtensionAdd('r', ["extended", "sequence"])
            .PrivateUseAdd("private")
            .Build();

        _ = languageTag.Should().NotBeNull();
        _ = languageTag.Validate().Should().BeTrue();
        _ = languageTag.ToString().Should().Be("en-latn-gb-boont-r-extended-sequence-x-private");
    }

    [Fact]
    public void Parse_Normalize_Roundtrip()
    {
        string original = "en-latn-gb-boont";

        LanguageTag? parsed = LanguageTag.Parse(original);
        _ = parsed.Should().NotBeNull();

        LanguageTag? normalized = parsed.Normalize();
        _ = normalized.Should().NotBeNull();
        _ = normalized.ToString().Should().Be("en-GB-boont");

        bool success = LanguageTag.TryParse(normalized.ToString(), out LanguageTag? reparsed);
        _ = success.Should().BeTrue();
        _ = reparsed!.ToString().Should().Be(normalized.ToString());
    }

    [Theory]
    [InlineData("en-US", "en-US")]
    [InlineData("invalid-tag", "und")]
    [InlineData("", "und")]
    public void ParseOrDefault_WithNoDefaultTag_ReturnsExpected(string tag, string expected)
    {
        LanguageTag result = LanguageTag.ParseOrDefault(tag);
        _ = result.Should().NotBeNull();
        _ = result.ToString().Should().Be(expected);
    }

    [Fact]
    public void ParseOrDefault_WithCustomDefaultTag_ReturnsCustomDefault()
    {
        LanguageTag? customDefault = LanguageTag.Parse("en-US");
        LanguageTag result = LanguageTag.ParseOrDefault("invalid-tag", customDefault);
        _ = result.Should().NotBeNull();
        _ = result.ToString().Should().Be("en-US");
    }

    [Theory]
    [InlineData("en-latn-us", "en-US")]
    [InlineData("zh-cmn-Hans-CN", "cmn-Hans-CN")]
    public void ParseAndNormalize_ValidTag_ReturnsNormalized(string tag, string expected)
    {
        LanguageTag? result = LanguageTag.ParseAndNormalize(tag);
        _ = result.Should().NotBeNull();
        _ = result.ToString().Should().Be(expected);
    }

    [Fact]
    public void ParseAndNormalize_InvalidTag_ReturnsNull()
    {
        LanguageTag? result = LanguageTag.ParseAndNormalize("invalid-tag");
        _ = result.Should().BeNull();
    }

    [Fact]
    public void IsValid_Property_ValidTag_ReturnsTrue()
    {
        LanguageTag? tag = LanguageTag.Parse("en-US");
        _ = tag.Should().NotBeNull();
        _ = tag.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("en")]
    [InlineData("zh")]
    [InlineData("cmn")]
    public void FromLanguage_CreatesSimpleTag(string language)
    {
        LanguageTag tag = LanguageTag.FromLanguage(language);
        _ = tag.Should().NotBeNull();
        _ = tag.Language.Should().Be(language);
        _ = tag.ToString().Should().Be(language);
        _ = tag.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("en", "US", "en-US")]
    [InlineData("zh", "CN", "zh-CN")]
    [InlineData("fr", "FR", "fr-FR")]
    public void FromLanguageRegion_CreatesLanguageRegionTag(
        string language,
        string region,
        string expected
    )
    {
        LanguageTag tag = LanguageTag.FromLanguageRegion(language, region);
        _ = tag.Should().NotBeNull();
        _ = tag.Language.Should().Be(language);
        _ = tag.Region.Should().Be(region);
        _ = tag.ToString().Should().Be(expected);
        _ = tag.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("zh", "Hans", "CN", "zh-Hans-CN")]
    [InlineData("zh", "Hant", "TW", "zh-Hant-TW")]
    [InlineData("en", "Latn", "GB", "en-Latn-GB")]
    public void FromLanguageScriptRegion_CreatesFullTag(
        string language,
        string script,
        string region,
        string expected
    )
    {
        LanguageTag tag = LanguageTag.FromLanguageScriptRegion(language, script, region);
        _ = tag.Should().NotBeNull();
        _ = tag.Language.Should().Be(language);
        _ = tag.Script.Should().Be(script);
        _ = tag.Region.Should().Be(region);
        _ = tag.ToString().Should().Be(expected);
        _ = tag.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Equals_SameTags_ReturnsTrue()
    {
        LanguageTag? tag1 = LanguageTag.Parse("en-US");
        LanguageTag? tag2 = LanguageTag.Parse("en-us"); // Case insensitive

        _ = tag1.Should().NotBeNull();
        _ = tag2.Should().NotBeNull();
        _ = tag1.Equals(tag2).Should().BeTrue();
        _ = tag1.Equals((object?)tag2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentTags_ReturnsFalse()
    {
        LanguageTag? tag1 = LanguageTag.Parse("en-US");
        LanguageTag? tag2 = LanguageTag.Parse("en-GB");

        _ = tag1.Should().NotBeNull();
        _ = tag2.Should().NotBeNull();
        _ = tag1.Equals(tag2).Should().BeFalse();
    }

    [Fact]
    public void Equals_NullTag_ReturnsFalse()
    {
        LanguageTag tag = LanguageTag.Parse("en-US")!;
        _ = tag.Equals(null).Should().BeFalse();
#pragma warning disable CS8602
        _ = tag.Equals((object?)null).Should().BeFalse();
#pragma warning restore CS8602
    }

    [Fact]
    public void GetHashCode_SameTags_ReturnsSameHashCode()
    {
        LanguageTag tag1 = LanguageTag.Parse("en-US")!;
        LanguageTag tag2 = LanguageTag.Parse("en-us")!;

        _ = tag1.GetHashCode().Should().Be(tag2.GetHashCode());
    }

    [Fact]
    public void OperatorEquals_SameTags_ReturnsTrue()
    {
        LanguageTag tag1 = LanguageTag.Parse("en-US")!;
        LanguageTag tag2 = LanguageTag.Parse("en-us")!;

        _ = (tag1 == tag2).Should().BeTrue();
    }

    [Fact]
    public void OperatorEquals_DifferentTags_ReturnsFalse()
    {
        LanguageTag tag1 = LanguageTag.Parse("en-US")!;
        LanguageTag tag2 = LanguageTag.Parse("en-GB")!;

        _ = (tag1 == tag2).Should().BeFalse();
    }

    [Fact]
    public void OperatorEquals_BothNull_ReturnsTrue()
    {
        LanguageTag? tag1 = null;
        LanguageTag? tag2 = null;

#pragma warning disable CA1508 // Avoid dead conditional code
        _ = (tag1 == tag2).Should().BeTrue();
#pragma warning restore CA1508
    }

    [Fact]
    public void OperatorEquals_OneNull_ReturnsFalse()
    {
        LanguageTag? tag1 = LanguageTag.Parse("en-US");
        LanguageTag? tag2 = null;

        _ = (tag1 == tag2).Should().BeFalse();
        _ = (tag2 == tag1).Should().BeFalse();
    }

    [Fact]
    public void OperatorNotEquals_SameTags_ReturnsFalse()
    {
        LanguageTag? tag1 = LanguageTag.Parse("en-US");
        LanguageTag? tag2 = LanguageTag.Parse("en-us");

        _ = (tag1 != tag2).Should().BeFalse();
    }

    [Fact]
    public void OperatorNotEquals_DifferentTags_ReturnsTrue()
    {
        LanguageTag? tag1 = LanguageTag.Parse("en-US");
        LanguageTag? tag2 = LanguageTag.Parse("en-GB");

        _ = (tag1 != tag2).Should().BeTrue();
    }

    [Fact]
    public void Variants_Property_ReturnsImmutableArray()
    {
        LanguageTag tag = LanguageTag
            .CreateBuilder()
            .Language("en")
            .VariantAdd("variant1")
            .VariantAdd("variant2")
            .Build();

        ImmutableArray<string> variants = tag.Variants;
        _ = variants.Length.Should().Be(2);
        _ = variants[0].Should().Be("variant1");
        _ = variants[1].Should().Be("variant2");
    }

    [Fact]
    public void Extensions_Property_ReturnsImmutableArray()
    {
        LanguageTag tag = LanguageTag
            .CreateBuilder()
            .Language("en")
            .ExtensionAdd('u', ["ca", "buddhist"])
            .ExtensionAdd('r', ["extended"])
            .Build();

        ImmutableArray<ExtensionTag> extensions = tag.Extensions;
        _ = extensions.Length.Should().Be(2);
        _ = extensions[0].Prefix.Should().Be('u');
        _ = extensions[1].Prefix.Should().Be('r');
    }

    [Fact]
    public void PrivateUse_Property_ReturnsPrivateUseTag()
    {
        LanguageTag tag = LanguageTag
            .CreateBuilder()
            .Language("en")
            .PrivateUseAdd("private1")
            .PrivateUseAdd("private2")
            .Build();

        _ = tag.PrivateUse.Should().NotBeNull();
        _ = tag.PrivateUse.Tags.Length.Should().Be(2);
        _ = tag.PrivateUse.Tags[0].Should().Be("private1");
        _ = tag.PrivateUse.Tags[1].Should().Be("private2");
    }

    [Fact]
    public void ExtensionTag_ToString_FormatsCorrectly()
    {
        LanguageTag tag = LanguageTag
            .CreateBuilder()
            .Language("en")
            .ExtensionAdd('u', ["ca", "buddhist"])
            .Build();

        ExtensionTag extension = tag.Extensions[0];
        _ = extension.ToString().Should().Be("u-ca-buddhist");
    }

    [Fact]
    public void PrivateUseTag_ToString_FormatsCorrectly()
    {
        LanguageTag tag = LanguageTag
            .CreateBuilder()
            .Language("en")
            .PrivateUseAdd("private1")
            .PrivateUseAdd("private2")
            .Build();

        _ = tag.PrivateUse.ToString().Should().Be("x-private1-private2");
    }

    [Fact]
    public void PrivateUseTag_Prefix_IsX() => _ = PrivateUseTag.Prefix.Should().Be('x');

    // Additional edge case tests for comprehensive coverage

    [Fact]
    public void Parse_NullInput_ReturnsNull()
    {
        LanguageTag? result = LanguageTag.Parse(null!);
        _ = result.Should().BeNull();
    }

    [Theory]
    [InlineData("en")]
    [InlineData("en-US")]
    [InlineData("zh-Hans-CN")]
    public void LanguageTag_PropertiesAreReadable(string tag)
    {
        LanguageTag? languageTag = LanguageTag.Parse(tag);
        _ = languageTag.Should().NotBeNull();

        // Verify all properties are accessible
        _ = languageTag.Language.Should().NotBeNull();
        _ = languageTag.ExtendedLanguage.Should().NotBeNull();
        _ = languageTag.Script.Should().NotBeNull();
        _ = languageTag.Region.Should().NotBeNull();
        _ = languageTag.Variants.Should().NotBeNull();
        _ = languageTag.Extensions.Should().NotBeNull();
        _ = languageTag.PrivateUse.Should().NotBeNull();
    }

    [Fact]
    public void ExtensionTag_Tags_ReturnsImmutableArray()
    {
        LanguageTag tag = LanguageTag
            .CreateBuilder()
            .Language("en")
            .ExtensionAdd('u', ["ca", "buddhist", "nu", "thai"])
            .Build();

        ExtensionTag extension = tag.Extensions[0];
        _ = extension.Tags.Length.Should().Be(4);
        _ = extension.Tags[0].Should().Be("ca");
        _ = extension.Tags[1].Should().Be("buddhist");
        _ = extension.Tags[2].Should().Be("nu");
        _ = extension.Tags[3].Should().Be("thai");
    }

    [Fact]
    public void Equals_SameInstance_ReturnsTrue()
    {
        LanguageTag tag = LanguageTag.Parse("en-US")!;
        _ = tag.Equals(tag).Should().BeTrue();
    }

    [Theory]
    [InlineData("en-US", "en-us")]
    [InlineData("zh-Hans-CN", "zh-hans-cn")]
    [InlineData("en-GB-boont", "EN-gb-BOONT")]
    public void Equals_CaseInsensitive_ReturnsTrue(string tag1Str, string tag2Str)
    {
        LanguageTag tag1 = LanguageTag.Parse(tag1Str)!;
        LanguageTag tag2 = LanguageTag.Parse(tag2Str)!;

        _ = tag1.Equals(tag2).Should().BeTrue();
        _ = (tag1 == tag2).Should().BeTrue();
        _ = tag1.GetHashCode().Should().Be(tag2.GetHashCode());
    }

    [Fact]
    public void ParseOrDefault_NullTag_ReturnsUndetermined()
    {
        LanguageTag result = LanguageTag.ParseOrDefault(null!);
        _ = result.ToString().Should().Be("und");
    }

    [Fact]
    public void ParseAndNormalize_NullTag_ReturnsNull()
    {
        LanguageTag? result = LanguageTag.ParseAndNormalize(null!);
        _ = result.Should().BeNull();
    }

    [Fact]
    public void TryParse_NullTag_ReturnsFalse()
    {
        bool result = LanguageTag.TryParse(null!, out LanguageTag? languageTag);
        _ = result.Should().BeFalse();
        _ = languageTag.Should().BeNull();
    }

    [Theory]
    [InlineData("en", 0)]
    [InlineData("en-US", 0)]
    [InlineData("zh-Hans", 0)]
    public void LanguageTag_EmptyCollections_HaveZeroLength(string tag, int emptyCount)
    {
        LanguageTag languageTag = LanguageTag.Parse(tag)!;

        if (
            string.IsNullOrEmpty(languageTag.Script)
            && string.IsNullOrEmpty(languageTag.ExtendedLanguage)
        )
        {
            _ = languageTag.Variants.Length.Should().Be(emptyCount);
            _ = languageTag.Extensions.Length.Should().Be(emptyCount);
        }
    }

    [Fact]
    public void FromLanguage_EmptyString_CreatesTag()
    {
        LanguageTag tag = LanguageTag.FromLanguage(string.Empty);
        _ = tag.Should().NotBeNull();
        // Note: This creates an invalid tag, but the factory method doesn't validate
    }

    [Fact]
    public void Validate_ComplexValidTag_ReturnsTrue()
    {
        LanguageTag tag = LanguageTag
            .CreateBuilder()
            .Language("en")
            .ExtendedLanguage("esl")
            .Script("Latn")
            .Region("US")
            .VariantAdd("variant1")
            .VariantAdd("variant2")
            .ExtensionAdd('u', ["ca", "buddhist"])
            .ExtensionAdd('t', ["en", "us"])
            .PrivateUseAdd("private1")
            .Build();

        _ = tag.IsValid.Should().BeTrue();
        _ = tag.Validate().Should().BeTrue();
    }

    [Fact]
    public void ExtensionTag_DefaultConstructor_CreatesEmptyTag()
    {
        ExtensionTag extension = new();
        _ = extension.Prefix.Should().Be('\0');
        _ = extension.Tags.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void ExtensionTag_EmptyTags_ToStringReturnsEmpty()
    {
        ExtensionTag extension = new('u', []);
        _ = extension.ToString().Should().Be(string.Empty);
    }

    [Fact]
    public void ExtensionTag_EnumerableConstructor_CreatesTag()
    {
        List<string> tags = ["tag1", "tag2", "tag3"];
        ExtensionTag extension = new('u', tags);

        _ = extension.Prefix.Should().Be('u');
        _ = extension.Tags.Length.Should().Be(3);
        _ = extension.Tags[0].Should().Be("tag1");
        _ = extension.Tags[1].Should().Be("tag2");
        _ = extension.Tags[2].Should().Be("tag3");
    }

    [Fact]
    public void ExtensionTag_RecordEquality_WorksCorrectly()
    {
        ExtensionTag ext1 = new('u', ["ca", "buddhist"]);
        ExtensionTag ext2 = new('U', ["CA", "BUDDHIST"]);
        ExtensionTag ext3 = new('t', ["ca", "buddhist"]);

        _ = ext1.Equals(ext2).Should().BeTrue();
        _ = (ext1 == ext2).Should().BeTrue();
        _ = ext1.GetHashCode().Should().Be(ext2.GetHashCode());

        _ = ext1.Equals(ext3).Should().BeFalse();
        _ = (ext1 != ext3).Should().BeTrue();
    }

    [Fact]
    public void PrivateUseTag_DefaultConstructor_CreatesEmptyTag()
    {
        PrivateUseTag privateUse = new();
        _ = privateUse.Tags.IsEmpty.Should().BeTrue();
        _ = privateUse.ToString().Should().Be(string.Empty);
    }

    [Fact]
    public void PrivateUseTag_EnumerableConstructor_CreatesTag()
    {
        List<string> tags = ["private1", "private2"];
        PrivateUseTag privateUse = new(tags);

        _ = privateUse.Tags.Length.Should().Be(2);
        _ = privateUse.Tags[0].Should().Be("private1");
        _ = privateUse.Tags[1].Should().Be("private2");
    }

    [Fact]
    public void PrivateUseTag_RecordEquality_WorksCorrectly()
    {
        PrivateUseTag priv1 = new(["private1", "private2"]);
        PrivateUseTag priv2 = new(["PRIVATE1", "PRIVATE2"]);
        PrivateUseTag priv3 = new(["other"]);

        _ = priv1.Equals(priv2).Should().BeTrue();
        _ = (priv1 == priv2).Should().BeTrue();
        _ = priv1.GetHashCode().Should().Be(priv2.GetHashCode());

        _ = priv1.Equals(priv3).Should().BeFalse();
        _ = (priv1 != priv3).Should().BeTrue();
    }

    [Fact]
    public void PrivateUseTag_EmptyTag_ToStringReturnsEmpty()
    {
        PrivateUseTag privateUse = new();
        _ = privateUse.ToString().Should().Be(string.Empty);
    }
}
