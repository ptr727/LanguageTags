using AwesomeAssertions;
using Xunit;

namespace ptr727.LanguageTags.Tests;

public class Iso6392Tests(Fixture fixture) : IClassFixture<Fixture>
{
    private readonly Fixture _fixture = fixture;

    [Fact]
    public void Create()
    {
        // Create full list of languages
        Iso6392Data iso6392 = Iso6392Data.Create();
        _ = iso6392.RecordList.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void LoadData()
    {
        Iso6392Data iso6392 = Iso6392Data.LoadData(
            Fixture.GetDataFilePath(Iso6392Data.DataFileName)
        );
        _ = iso6392.Should().NotBeNull();
        _ = iso6392.RecordList.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void LoadJson()
    {
        Iso6392Data iso6392 = Iso6392Data.LoadJson(
            Fixture.GetDataFilePath(Iso6392Data.DataFileName + ".json")
        );
        _ = iso6392.Should().NotBeNull();
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
    [InlineData("cpe", false, "Creoles and pidgins, English based")]
    [InlineData("zulu", true, "Zulu")]
    [InlineData("700-300", true, "Official Aramaic (700-300 BCE); Imperial Aramaic (700-300 BCE)")]
    public void Find_Pass(string input, bool description, string output)
    {
        // Create full list of languages
        Iso6392Data iso6392 = Iso6392Data.Create();
        _ = iso6392.RecordList.Length.Should().BeGreaterThan(0);

        // Find matching language
        Iso6392Data.Record record = iso6392.Find(input, description);
        _ = record.Should().NotBeNull();
        _ = record.RefName.Should().BeEquivalentTo(output);
    }

    [Theory]
    [InlineData("xx")]
    [InlineData("xxx")]
    [InlineData("xxxx")]
    public void Find_Fail(string input)
    {
        // Create full list of languages
        Iso6392Data iso6392 = Iso6392Data.Create();
        _ = iso6392.RecordList.Length.Should().BeGreaterThan(0);

        // Fail to find matching language
        Iso6392Data.Record record = iso6392.Find(input, false);
        _ = record.Should().BeNull();
    }
}
