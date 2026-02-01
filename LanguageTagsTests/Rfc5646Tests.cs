namespace ptr727.LanguageTags.Tests;

public class Rfc5646Tests : SingleInstanceFixture
{
    [Fact]
    public void Create()
    {
        // Create full list of languages
        Rfc5646Data rfc5646 = Rfc5646Data.Create();
        _ = rfc5646.Should().NotBeNull();
        _ = rfc5646.RecordList.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task FromData()
    {
        Rfc5646Data rfc5646 = await Rfc5646Data.FromDataAsync(
            GetDataFilePath(Rfc5646Data.DataFileName)
        );
        _ = rfc5646.Should().NotBeNull();
        _ = rfc5646.RecordList.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task FromJson()
    {
        Rfc5646Data rfc5646 = await Rfc5646Data.FromJsonAsync(
            GetDataFilePath(Rfc5646Data.DataFileName + ".json")
        );
        _ = rfc5646.Should().NotBeNull();
        _ = rfc5646.RecordList.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Create_FromData_FromJson_RecordsMatch()
    {
        Rfc5646Data created = Rfc5646Data.Create();
        Rfc5646Data fromData = await Rfc5646Data.FromDataAsync(
            GetDataFilePath(Rfc5646Data.DataFileName)
        );
        Rfc5646Data fromJson = await Rfc5646Data.FromJsonAsync(
            GetDataFilePath(Rfc5646Data.DataFileName + ".json")
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
        Rfc5646Data rfc5646 = Rfc5646Data.Create();
        _ = rfc5646.RecordList.Length.Should().BeGreaterThan(0);

        string tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
        try
        {
            await rfc5646.SaveJsonAsync(tempFile);
            Rfc5646Data roundTrip = await Rfc5646Data.FromJsonAsync(tempFile);
            _ = roundTrip.Should().NotBeNull();
            _ = roundTrip.RecordList.Length.Should().Be(rfc5646.RecordList.Length);
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
    [InlineData("af", false, "Afrikaans")]
    [InlineData("zh", false, "Chinese")]
    [InlineData("de", false, "German")]
    [InlineData("yue", false, "Yue Chinese")]
    [InlineData("zh-cmn-Hant", false, "Mandarin Chinese (Traditional)")]
    [InlineData("cmn-Hant", false, "Mandarin Chinese (Traditional)")]
    [InlineData("i-klingon", false, "Klingon")]
    [InlineData("zulu", true, "Zulu")]
    [InlineData(
        "language association",
        true,
        "Interlingua (International Auxiliary Language Association)"
    )]
    public void Find_Pass(string input, bool description, string output)
    {
        // Create full list of languages
        Rfc5646Data rfc5646 = Rfc5646Data.Create();

        _ = rfc5646.Should().NotBeNull();
        _ = rfc5646.RecordList.Length.Should().BeGreaterThan(0);

        // Find matching language
        Rfc5646Record? record = rfc5646.Find(input, description);
        _ = record.Should().NotBeNull();
        _ = record
            .Description.Should()
            .Contain(item => item.Equals(output, StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("xx")]
    [InlineData("xxx")]
    [InlineData("xxxx")]
    public void Find_Fail(string input)
    {
        // Create full list of languages
        Rfc5646Data rfc5646 = Rfc5646Data.Create();
        _ = rfc5646.Should().NotBeNull();
        _ = rfc5646.RecordList.Length.Should().BeGreaterThan(0);

        // Fail to find matching language
        Rfc5646Record? record = rfc5646.Find(input, false);
        _ = record.Should().BeNull();
    }

    [Fact]
    public void Find_Null_ReturnsNull()
    {
        Rfc5646Data rfc5646 = Rfc5646Data.Create();
        Rfc5646Record? record = rfc5646.Find(null!, false);
        _ = record.Should().BeNull();
    }

    [Fact]
    public void Find_Empty_ReturnsNull()
    {
        Rfc5646Data rfc5646 = Rfc5646Data.Create();
        Rfc5646Record? record = rfc5646.Find(string.Empty, false);
        _ = record.Should().BeNull();
    }

    [Fact]
    public void FileDate_IsSet()
    {
        Rfc5646Data rfc5646 = Rfc5646Data.Create();
        _ = rfc5646.FileDate.Should().NotBeNull();
        _ = rfc5646.FileDate.Should().HaveValue();
    }
}
