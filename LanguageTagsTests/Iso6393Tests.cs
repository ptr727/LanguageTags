namespace ptr727.LanguageTags.Tests;

public sealed class Iso6393Tests : SingleInstanceFixture
{
    [Fact]
    public void Create()
    {
        // Create full list of languages
        Iso6393Data iso6393 = Iso6393Data.Create();
        _ = iso6393.RecordList.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task LoadData()
    {
        Iso6393Data iso6393 = await Iso6393Data.LoadDataAsync(
            GetDataFilePath(Iso6393Data.DataFileName)
        );
        _ = iso6393.Should().NotBeNull();
        _ = iso6393.RecordList.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task LoadJson()
    {
        Iso6393Data? iso6393 = await Iso6393Data.LoadJsonAsync(
            GetDataFilePath(Iso6393Data.DataFileName + ".json")
        );
        _ = iso6393.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveJsonAsync_RoundTrip()
    {
        Iso6393Data iso6393 = Iso6393Data.Create();
        _ = iso6393.RecordList.Length.Should().BeGreaterThan(0);

        string tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
        try
        {
            await Iso6393Data.SaveJsonAsync(tempFile, iso6393);
            Iso6393Data? roundTrip = await Iso6393Data.LoadJsonAsync(tempFile);
            _ = roundTrip.Should().NotBeNull();
            _ = roundTrip.RecordList.Length.Should().Be(iso6393.RecordList.Length);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
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
    public void Find_Pass(string input, bool description, string output)
    {
        // Create full list of languages
        Iso6393Data iso6393 = Iso6393Data.Create();
        _ = iso6393.RecordList.Length.Should().BeGreaterThan(0);

        // Find matching language
        Iso6393Record? record = iso6393.Find(input, description);
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
        Iso6393Data iso6393 = Iso6393Data.Create();
        _ = iso6393.RecordList.Length.Should().BeGreaterThan(0);

        // Fail to find matching language
        Iso6393Record? record = iso6393.Find(input, false);
        _ = record.Should().BeNull();
    }

    [Fact]
    public void Find_Null_ThrowsArgumentNullException()
    {
        Iso6393Data iso6393 = Iso6393Data.Create();
        _ = Assert.Throws<ArgumentNullException>(() => iso6393.Find(null!, false));
    }

    [Fact]
    public void Find_Empty_ReturnsNull()
    {
        Iso6393Data iso6393 = Iso6393Data.Create();
        Iso6393Record? record = iso6393.Find(string.Empty, false);
        _ = record.Should().BeNull();
    }
}
