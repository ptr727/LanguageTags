namespace ptr727.LanguageTags.Tests;

public class Rfc5646Tests
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
    public void LoadData()
    {
        Rfc5646Data rfc5646 = Rfc5646Data.LoadData(
            Fixture.GetDataFilePath(Rfc5646Data.DataFileName)
        );
        _ = rfc5646.Should().NotBeNull();
        _ = rfc5646.RecordList.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void LoadJson()
    {
        Rfc5646Data? rfc5646 = Rfc5646Data.LoadJson(
            Fixture.GetDataFilePath(Rfc5646Data.DataFileName + ".json")
        );
        _ = rfc5646.Should().NotBeNull();
        _ = rfc5646.RecordList.Length.Should().BeGreaterThan(0);
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
