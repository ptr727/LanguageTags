namespace ptr727.LanguageTags.Tests;

public sealed class UnM49Tests : SingleInstanceFixture
{
    [Fact]
    public void Create()
    {
        // Create full list of containment records
        UnM49Data unM49 = UnM49Data.Create();
        _ = unM49.RecordList.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task FromData()
    {
        UnM49Data unM49 = await UnM49Data.FromDataAsync(GetDataFilePath(UnM49Data.DataFileName));
        _ = unM49.Should().NotBeNull();
        _ = unM49.RecordList.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task FromJson()
    {
        UnM49Data unM49 = await UnM49Data.FromJsonAsync(
            GetDataFilePath(UnM49Data.DataFileName + ".json")
        );
        _ = unM49.Should().NotBeNull();
        _ = unM49.RecordList.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Create_FromData_FromJson_RecordsMatch()
    {
        UnM49Data created = UnM49Data.Create();
        UnM49Data fromData = await UnM49Data.FromDataAsync(GetDataFilePath(UnM49Data.DataFileName));
        UnM49Data fromJson = await UnM49Data.FromJsonAsync(
            GetDataFilePath(UnM49Data.DataFileName + ".json")
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
        UnM49Data unM49 = UnM49Data.Create();
        _ = unM49.RecordList.Length.Should().BeGreaterThan(0);

        string tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
        try
        {
            await unM49.SaveJsonAsync(tempFile);
            UnM49Data roundTrip = await UnM49Data.FromJsonAsync(tempFile);
            _ = roundTrip.Should().NotBeNull();
            _ = roundTrip.RecordList.Length.Should().Be(unM49.RecordList.Length);
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
    [InlineData("419")] // Latin America and the Caribbean
    [InlineData("013")] // Central America
    [InlineData("001")] // World
    public void Find_Pass(string code)
    {
        UnM49Data unM49 = UnM49Data.Create();
        UnM49Record? record = unM49.Find(code);
        _ = record.Should().NotBeNull();
        _ = record.Code.Should().BeEquivalentTo(code);
    }

    [Theory]
    [InlineData("XX")]
    [InlineData("999")]
    public void Find_Fail(string code)
    {
        UnM49Data unM49 = UnM49Data.Create();
        UnM49Record? record = unM49.Find(code);
        _ = record.Should().BeNull();
    }

    [Fact]
    public void Find_Empty_ReturnsNull()
    {
        UnM49Data unM49 = UnM49Data.Create();
        _ = unM49.Find(string.Empty).Should().BeNull();
    }

    [Theory]
    [InlineData("419", "MX", true)] // Mexico is in Latin America
    [InlineData("013", "MX", true)] // Mexico is in Central America
    [InlineData("005", "AR", true)] // Argentina is in South America
    [InlineData("419", "013", true)] // Central America is within Latin America
    [InlineData("001", "ZA", true)] // World contains every country
    [InlineData("419", "ES", false)] // Spain is not in Latin America
    [InlineData("419", "US", false)] // United States is not in Latin America
    [InlineData("419", "419", false)] // A group does not contain itself
    public void Contains(string groupCode, string code, bool contained)
    {
        UnM49Data unM49 = UnM49Data.Create();
        _ = unM49.Contains(groupCode, code).Should().Be(contained);
    }

    [Fact]
    public void GetAncestors_Country_ReturnsContainingGroups()
    {
        UnM49Data unM49 = UnM49Data.Create();
        IReadOnlyList<string> ancestors = unM49.GetAncestors("MX");

        // Mexico is nested under Central America, Latin America, the Americas, and the World
        _ = ancestors.Should().Contain(["013", "419", "019", "001"]);
    }

    [Fact]
    public void GetAncestors_Unknown_ReturnsEmpty()
    {
        UnM49Data unM49 = UnM49Data.Create();
        _ = unM49.GetAncestors("ZZ").Should().BeEmpty();
    }
}
