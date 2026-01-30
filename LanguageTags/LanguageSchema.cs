using System.Text.Json.Serialization;

namespace ptr727.LanguageTags;

internal static class LanguageSchema
{
    internal static async Task WriteFileAsync(string fileName, string value)
    {
        // Always write as CRLF with newline at the end
        if (
            value.Contains('\n', StringComparison.Ordinal)
            && !value.Contains('\r', StringComparison.Ordinal)
        )
        {
            value = value.Replace("\n", "\r\n", StringComparison.Ordinal);
        }
        value = value.TrimEnd() + "\r\n";
        await File.WriteAllTextAsync(fileName, value).ConfigureAwait(false);
    }

    internal static string GetCodeGenString(string? text) =>
        string.IsNullOrEmpty(text) ? "null" : $"\"{text}\"";

    internal static string GetCodeGenString(DateOnly? date) =>
        date == null
            ? "null"
            : $"new DateOnly({date.Value.Year}, {date.Value.Month}, {date.Value.Day})";

    internal static string GetCodeGenString(Rfc5646Record.RecordType type) =>
        $"Rfc5646Record.RecordType.{type}";

    internal static string GetCodeGenString(Rfc5646Record.RecordScope scope) =>
        $"Rfc5646Record.RecordScope.{scope}";

    internal static string GetCodeGenString(IEnumerable<string> list) =>
        $"[{string.Join(", ", list.Select(item => $"@\"{item.Replace("\"", "\"\"", StringComparison.Ordinal)}\""))}]";
}

[JsonSourceGenerationOptions(
    AllowTrailingCommas = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    IncludeFields = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
    ReadCommentHandling = JsonCommentHandling.Skip,
    WriteIndented = true,
    NewLine = "\r\n"
)]
[JsonSerializable(typeof(Iso6392Data))]
[JsonSerializable(typeof(Iso6393Data))]
[JsonSerializable(typeof(Rfc5646Data))]
internal partial class LanguageJsonContext : JsonSerializerContext;
