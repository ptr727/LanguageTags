namespace ptr727.LanguageTags.Tests;

public sealed class LanguageSchemaTests
{
    [Theory]
    [InlineData(null, "null")]
    [InlineData("", "null")]
    [InlineData("value", "\"value\"")]
    public void GetCodeGenString_String(string? input, string expected) =>
        _ = LanguageSchema.GetCodeGenString(input).Should().Be(expected);

    [Fact]
    public void GetCodeGenString_DateOnly_Null() =>
        _ = LanguageSchema.GetCodeGenString((DateOnly?)null).Should().Be("null");

    [Fact]
    public void GetCodeGenString_DateOnly_Value() =>
        _ = LanguageSchema
            .GetCodeGenString(new DateOnly(2024, 3, 7))
            .Should()
            .Be("new DateOnly(2024, 3, 7)");

    [Fact]
    public void GetCodeGenString_RecordType() =>
        _ = LanguageSchema
            .GetCodeGenString(Rfc5646Record.RecordType.Language)
            .Should()
            .Be("Rfc5646Record.RecordType.Language");

    [Fact]
    public void GetCodeGenString_RecordScope() =>
        _ = LanguageSchema
            .GetCodeGenString(Rfc5646Record.RecordScope.MacroLanguage)
            .Should()
            .Be("Rfc5646Record.RecordScope.MacroLanguage");

    [Fact]
    public void GetCodeGenString_List_EscapesQuotes() =>
        _ = LanguageSchema.GetCodeGenString(["a", "b\"c"]).Should().Be("[@\"a\", @\"b\"\"c\"]");
}
