using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace ptr727.LanguageTags;

/// <summary>
/// Provides access to ISO 639-2 language code data.
/// </summary>
public partial class Iso6392Data
{
    internal const string DataUri = "https://www.loc.gov/standards/iso639-2/ISO-639-2_utf-8.txt";
    internal const string DataFileName = "iso6392";

    /// <summary>
    /// Loads ISO 639-2 data from a file.
    /// </summary>
    /// <param name="fileName">The path to the data file.</param>
    /// <returns>The loaded <see cref="Iso6392Data"/>.</returns>
    /// <exception cref="InvalidDataException">Thrown when the file contains invalid data.</exception>
    public static Iso6392Data LoadData(string fileName)
    {
        // https://www.loc.gov/standards/iso639-2/ascii_8bits.html
        // Alpha-3 (bibliographic) code
        // Alpha-3 (terminologic) code (when given)
        // Alpha-2 code (when given)
        // English name
        // French name (when given)
        // | deliminator
        // LF line terminator

        // Read line by line
        List<Iso6392Record> recordList = [];
        using StreamReader lineReader = new(File.OpenRead(fileName));
        while (lineReader.ReadLine() is { } line)
        {
            // Parse using pipe character
            List<string> records = [.. line.Split('|').Select(item => item.Trim())];
            if (records.Count != 5)
            {
                throw new InvalidDataException($"Invalid data found in ISO 639-2 record: {line}");
            }

            // Populate record
            Iso6392Record record = new()
            {
                Part2B = string.IsNullOrEmpty(records[0]) ? null : records[0],
                Part2T = string.IsNullOrEmpty(records[1]) ? null : records[1],
                Part1 = string.IsNullOrEmpty(records[2]) ? null : records[2],
                RefName = string.IsNullOrEmpty(records[3]) ? null : records[3],
            };
            if (string.IsNullOrEmpty(record.Part2B) || string.IsNullOrEmpty(record.RefName))
            {
                throw new InvalidDataException($"Invalid data found in ISO 639-2 record: {line}");
            }
            recordList.Add(record);
        }
        return recordList.Count == 0
            ? throw new InvalidDataException($"No data found in ISO 639-2 file: {fileName}")
            : new Iso6392Data { RecordList = [.. recordList] };
    }

    /// <summary>
    /// Loads ISO 639-2 data from a JSON file.
    /// </summary>
    /// <param name="fileName">The path to the JSON file.</param>
    /// <returns>The loaded <see cref="Iso6392Data"/> or null if deserialization fails.</returns>
    public static Iso6392Data? LoadJson(string fileName) =>
        JsonSerializer.Deserialize(
            File.ReadAllText(fileName),
            LanguageJsonContext.Default.Iso6392Data
        );

    internal static void SaveJson(string fileName, Iso6392Data iso6392) =>
        File.WriteAllText(
            fileName,
            JsonSerializer.Serialize(iso6392, LanguageJsonContext.Default.Iso6392Data)
        );

    internal static void GenCode(string fileName, Iso6392Data iso6392)
    {
        ArgumentNullException.ThrowIfNull(iso6392);
        StringBuilder stringBuilder = new();
        _ = stringBuilder
            .Append(
                """
                namespace ptr727.LanguageTags;

                /// <summary>
                /// Provides access to ISO 639-2 language code data.
                /// </summary>
                public partial class Iso6392Data
                {
                    public static Iso6392Data Create() =>
                        new()
                        {
                            RecordList =
                            [
                """
            )
            .Append("\r\n");

        foreach (Iso6392Record record in iso6392.RecordList)
        {
            _ = stringBuilder
                .Append(
                    CultureInfo.InvariantCulture,
                    $$"""
                                    new()
                                    {
                                        Part2B = {{LanguageSchema.GetCodeGenString(record.Part2B)}},
                                        Part2T = {{LanguageSchema.GetCodeGenString(record.Part2T)}},
                                        Part1 = {{LanguageSchema.GetCodeGenString(record.Part1)}},
                                        RefName = {{LanguageSchema.GetCodeGenString(
                        record.RefName
                    )}},
                                    },
                    """
                )
                .Append("\r\n");
        }
        _ = stringBuilder
            .Append(
                """
                            ],
                        };
                }
                """
            )
            .Append("\r\n");

        LanguageSchema.WriteFile(fileName, stringBuilder.ToString());
    }

    /// <summary>
    /// Gets the collection of ISO 639-2 language records.
    /// </summary>
    public required ImmutableArray<Iso6392Record> RecordList { get; init; }

    /// <summary>
    /// Finds an ISO 639-2 language record by language code or description.
    /// </summary>
    /// <param name="languageTag">The language code or description to search for.</param>
    /// <param name="includeDescription">If true, searches in the reference name field; otherwise, only searches language codes.</param>
    /// <returns>The matching <see cref="Iso6392Record"/> or null if not found.</returns>
    public Iso6392Record? Find(string? languageTag, bool includeDescription)
    {
        if (string.IsNullOrEmpty(languageTag))
        {
            return null;
        }

        // Find the matching language entry
        Iso6392Record? record = null;

        // 693 3 letter form
        if (languageTag.Length == 3)
        {
            // Try the 639-2/B
            record = RecordList.FirstOrDefault(item =>
                !string.IsNullOrEmpty(item.Part2B)
                && item.Part2B.Equals(languageTag, StringComparison.OrdinalIgnoreCase)
            );
            if (record != null)
            {
                return record;
            }

            // Try the 639-2/T
            record = RecordList.FirstOrDefault(item =>
                !string.IsNullOrEmpty(item.Part2T)
                && item.Part2T.Equals(languageTag, StringComparison.OrdinalIgnoreCase)
            );
            if (record != null)
            {
                return record;
            }
        }

        // 693 2 letter form
        if (languageTag.Length == 2)
        {
            // Try 639-1
            record = RecordList.FirstOrDefault(item =>
                !string.IsNullOrEmpty(item.Part1)
                && item.Part1.Equals(languageTag, StringComparison.OrdinalIgnoreCase)
            );
            if (record != null)
            {
                return record;
            }
        }

        // Long form
        if (includeDescription)
        {
            // Exact match
            record = RecordList.FirstOrDefault(item =>
                !string.IsNullOrEmpty(item.RefName)
                && item.RefName.Equals(languageTag, StringComparison.OrdinalIgnoreCase)
            );
            if (record != null)
            {
                return record;
            }

            // Partial match
            record = RecordList.FirstOrDefault(item =>
                !string.IsNullOrEmpty(item.RefName)
                && item.RefName.Contains(languageTag, StringComparison.OrdinalIgnoreCase)
            );
            if (record != null)
            {
                return record;
            }
        }

        // Not found
        return null;
    }
}

/// <summary>
/// Represents an ISO 639-2 language code record.
/// </summary>
public record Iso6392Record
{
    /// <summary>
    /// Gets the ISO 639-2/B bibliographic code (3 letters).
    /// </summary>
    public string? Part2B { get; init; }

    /// <summary>
    /// Gets the ISO 639-2/T terminology code (3 letters).
    /// </summary>
    public string? Part2T { get; init; }

    /// <summary>
    /// Gets the ISO 639-1 code (2 letters).
    /// </summary>
    public string? Part1 { get; init; }

    /// <summary>
    /// Gets the English reference name of the language.
    /// </summary>
    public string? RefName { get; init; }
}
