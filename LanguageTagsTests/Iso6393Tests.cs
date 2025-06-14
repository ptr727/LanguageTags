using AwesomeAssertions;
using Xunit;

namespace ptr727.LanguageTags.Tests;

public class Iso6393Tests(Fixture fixture) : IClassFixture<Fixture>
{
    private readonly Fixture _fixture = fixture;

    [Fact]
    public void Create()
    {
        // Create full list of languages
        Iso6393 iso6393 = Iso6393.Create();
        _ = iso6393.RecordList.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void LoadData()
    {
        Iso6393 iso6393 = Iso6393.LoadData(Fixture.GetDataFilePath(Iso6393.DataFileName));
        _ = iso6393.Should().NotBeNull();
        _ = iso6393.RecordList.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void LoadJson()
    {
        Iso6393 iso6393 = Iso6393.LoadJson(Fixture.GetDataFilePath(Iso6393.DataFileName + ".json"));
        _ = iso6393.Should().NotBeNull();
    }

    [Theory]
    [InlineData("afr", false, "Afrikaans")]
    [InlineData("af", false, "Afrikaans")]
    [InlineData("zh", false, "Chinese")]
    [InlineData("zho", false, "Chinese")]
    [InlineData("chi", false, "Chinese")]
    [InlineData("ger", false, "German")]
    [InlineData("deu", false, "German")]
    [InlineData("de", false, "German")]
    [InlineData("yue", false, "Yue Chinese")]
    [InlineData("zulu", true, "Zulu")]
    [InlineData("chamí", true, "Emberá-Chamí")]
    public void Succeed_Find(string input, bool description, string output)
    {
        // Create full list of languages
        Iso6393 iso6393 = Iso6393.Create();
        _ = iso6393.RecordList.Count.Should().BeGreaterThan(0);

        // Find matching language
        Iso6393.Record record = iso6393.Find(input, description);
        _ = record.Should().NotBeNull();
        _ = record.RefName.Should().BeEquivalentTo(output);
    }

    [Theory]
    [InlineData("xxx")]
    public void Fail_Find(string input)
    {
        // Create full list of languages
        Iso6393 iso6393 = Iso6393.Create();
        _ = iso6393.RecordList.Count.Should().BeGreaterThan(0);

        // Fail to find matching language
        Iso6393.Record record = iso6393.Find(input, false);
        _ = record.Should().BeNull();
    }
}
