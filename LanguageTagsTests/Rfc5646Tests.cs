using System;
using AwesomeAssertions;
using Xunit;

namespace ptr727.LanguageTags.Tests;

public class Rfc5646Tests(Fixture fixture) : IClassFixture<Fixture>
{
    private readonly Fixture _fixture = fixture;

    [Fact]
    public void Create()
    {
        // Create full list of languages
        Rfc5646 rfc5646 = Rfc5646.Create();
        _ = rfc5646.Should().NotBeNull();
        _ = rfc5646.RecordList.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void LoadData()
    {
        Rfc5646 rfc5646 = Rfc5646.LoadData(Fixture.GetDataFilePath(Rfc5646.DataFileName));
        _ = rfc5646.Should().NotBeNull();
        _ = rfc5646.RecordList.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void LoadJson()
    {
        Rfc5646 rfc5646 = Rfc5646.LoadJson(Fixture.GetDataFilePath(Rfc5646.DataFileName + ".json"));
        _ = rfc5646.Should().NotBeNull();
        _ = rfc5646.RecordList.Count.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData("af", false, "Afrikaans")]
    [InlineData("zh", false, "Chinese")]
    [InlineData("de", false, "German")]
    [InlineData("yue", false, "Yue Chinese")]
    [InlineData("zh-cmn-Hant", false, "Mandarin Chinese (Traditional)")]
    [InlineData("cmn-Hant", false, "Mandarin Chinese (Traditional)")]
    [InlineData("zulu", true, "Zulu")]
    [InlineData(
        "language association",
        true,
        "Interlingua (International Auxiliary Language Association)"
    )]
    public void Succeed_Find(string input, bool description, string output)
    {
        // Create full list of languages
        Rfc5646 rfc5646 = Rfc5646.Create();

        _ = rfc5646.Should().NotBeNull();
        _ = rfc5646.RecordList.Count.Should().BeGreaterThan(0);

        // Find matching language
        Rfc5646.Record record = rfc5646.Find(input, description);
        _ = record.Should().NotBeNull();
        _ = record
            .Description.Should()
            .Contain(item => item.Equals(output, StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("xxx")]
    public void Fail_Find(string input)
    {
        // Create full list of languages
        Rfc5646 rfc5646 = Rfc5646.Create();
        _ = rfc5646.Should().NotBeNull();
        _ = rfc5646.RecordList.Count.Should().BeGreaterThan(0);

        // Fail to find matching language
        Rfc5646.Record record = rfc5646.Find(input, false);
        _ = record.Should().BeNull();
    }
}
