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
    public async Task LoadData()
    {
        Rfc5646Data rfc5646 = await Rfc5646Data.LoadDataAsync(
            GetDataFilePath(Rfc5646Data.DataFileName)
        );
        _ = rfc5646.Should().NotBeNull();
        _ = rfc5646.RecordList.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task LoadJson()
    {
        Rfc5646Data? rfc5646 = await Rfc5646Data.LoadJsonAsync(
            GetDataFilePath(Rfc5646Data.DataFileName + ".json")
        );
        _ = rfc5646.Should().NotBeNull();
        _ = rfc5646.RecordList.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SaveJsonAsync_RoundTrip()
    {
        Rfc5646Data rfc5646 = Rfc5646Data.Create();
        _ = rfc5646.RecordList.Length.Should().BeGreaterThan(0);

        string tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
        try
        {
            await Rfc5646Data.SaveJsonAsync(tempFile, rfc5646);
            Rfc5646Data? roundTrip = await Rfc5646Data.LoadJsonAsync(tempFile);
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Find_NullOrEmpty_ReturnsNull(string? input)
    {
        Rfc5646Data rfc5646 = Rfc5646Data.Create();
        Rfc5646Record? record = rfc5646.Find(input, false);
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
