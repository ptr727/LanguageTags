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
    public async Task FromData()
    {
        Iso6393Data iso6393 = await Iso6393Data.FromDataAsync(
            GetDataFilePath(Iso6393Data.DataFileName)
        );
        _ = iso6393.Should().NotBeNull();
        _ = iso6393.RecordList.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task FromJson()
    {
        Iso6393Data iso6393 = await Iso6393Data.FromJsonAsync(
            GetDataFilePath(Iso6393Data.DataFileName + ".json")
        );
        _ = iso6393.Should().NotBeNull();
        _ = iso6393.RecordList.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Create_FromData_FromJson_RecordsMatch()
    {
        Iso6393Data created = Iso6393Data.Create();
        Iso6393Data fromData = await Iso6393Data.FromDataAsync(
            GetDataFilePath(Iso6393Data.DataFileName)
        );
        Iso6393Data fromJson = await Iso6393Data.FromJsonAsync(
            GetDataFilePath(Iso6393Data.DataFileName + ".json")
        );

        _ = created.RecordList.Length.Should().BeGreaterThan(0);
        _ = fromData.RecordList.Length.Should().BeGreaterThan(0);
        _ = fromJson.RecordList.Length.Should().BeGreaterThan(0);

        _ = fromData.RecordList.Should().BeEquivalentTo(created.RecordList);
        _ = fromJson.RecordList.Should().BeEquivalentTo(created.RecordList);
    }

    [Fact]
    public async Task SaveJsonAsync_RoundTrip()
    {
        Iso6393Data iso6393 = Iso6393Data.Create();
        _ = iso6393.RecordList.Length.Should().BeGreaterThan(0);

        string tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
        try
        {
            await iso6393.SaveJsonAsync(tempFile);
            Iso6393Data roundTrip = await Iso6393Data.FromJsonAsync(tempFile);
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
    public void Find_Null_ReturnsNull()
    {
        Iso6393Data iso6393 = Iso6393Data.Create();
        Iso6393Record? record = iso6393.Find(null!, false);
        _ = record.Should().BeNull();
    }

    [Fact]
    public void Find_Empty_ReturnsNull()
    {
        Iso6393Data iso6393 = Iso6393Data.Create();
        Iso6393Record? record = iso6393.Find(string.Empty, false);
        _ = record.Should().BeNull();
    }
}
