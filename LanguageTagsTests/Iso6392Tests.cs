namespace ptr727.LanguageTags.Tests;

public sealed class Iso6392Tests : SingleInstanceFixture
{
    [Fact]
    public void Create()
    {
        // Create full list of languages
        Iso6392Data iso6392 = Iso6392Data.Create();
        _ = iso6392.RecordList.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task FromData()
    {
        Iso6392Data iso6392 = await Iso6392Data.FromDataAsync(
            GetDataFilePath(Iso6392Data.DataFileName)
        );
        _ = iso6392.Should().NotBeNull();
        _ = iso6392.RecordList.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task FromJson()
    {
        Iso6392Data iso6392 = await Iso6392Data.FromJsonAsync(
            GetDataFilePath(Iso6392Data.DataFileName + ".json")
        );
        _ = iso6392.Should().NotBeNull();
        _ = iso6392.RecordList.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Create_FromData_FromJson_RecordsMatch()
    {
        Iso6392Data created = Iso6392Data.Create();
        Iso6392Data fromData = await Iso6392Data.FromDataAsync(
            GetDataFilePath(Iso6392Data.DataFileName)
        );
        Iso6392Data fromJson = await Iso6392Data.FromJsonAsync(
            GetDataFilePath(Iso6392Data.DataFileName + ".json")
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
        Iso6392Data iso6392 = Iso6392Data.Create();
        _ = iso6392.RecordList.Length.Should().BeGreaterThan(0);

        string tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
        try
        {
            await iso6392.SaveJsonAsync(tempFile);
            Iso6392Data roundTrip = await Iso6392Data.FromJsonAsync(tempFile);
            _ = roundTrip.Should().NotBeNull();
            _ = roundTrip.RecordList.Length.Should().Be(iso6392.RecordList.Length);
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
    [InlineData("cpe", false, "Creoles and pidgins, English based")]
    [InlineData("zulu", true, "Zulu")]
    [InlineData("700-300", true, "Official Aramaic (700-300 BCE); Imperial Aramaic (700-300 BCE)")]
    public void Find_Pass(string input, bool description, string output)
    {
        // Create full list of languages
        Iso6392Data iso6392 = Iso6392Data.Create();
        _ = iso6392.RecordList.Length.Should().BeGreaterThan(0);

        // Find matching language
        Iso6392Record? record = iso6392.Find(input, description);
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
        Iso6392Record? record = iso6392.Find(input, false);
        _ = record.Should().BeNull();
    }

    [Fact]
    public void Find_Null_ReturnsNull()
    {
        Iso6392Data iso6392 = Iso6392Data.Create();
        Iso6392Record? record = iso6392.Find(null!, false);
        _ = record.Should().BeNull();
    }

    [Fact]
    public void Find_Empty_ReturnsNull()
    {
        Iso6392Data iso6392 = Iso6392Data.Create();
        Iso6392Record? record = iso6392.Find(string.Empty, false);
        _ = record.Should().BeNull();
    }
}
