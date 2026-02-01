using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace ptr727.LanguageTags;

/// <summary>
/// Provides access to ISO 639-2 language code data.
/// </summary>
public sealed partial class Iso6392Data
{
    internal const string DataUri = "https://www.loc.gov/standards/iso639-2/ISO-639-2_utf-8.txt";
    internal const string DataFileName = "iso6392";

    private readonly Lazy<ILogger> _logger = new(LogOptions.CreateLogger<Iso6392Data>);
    internal ILogger Log => _logger.Value;

    [JsonConstructor]
    internal Iso6392Data() { }

    /// <summary>
    /// Gets the collection of ISO 639-2 language records.
    /// </summary>
    [JsonInclude]
    public ImmutableArray<Iso6392Record> RecordList { get; internal set; } = [];

    /// <summary>
    /// Creates an <see cref="Iso6392Data"/> instance from a data file asynchronously.
    /// </summary>
    /// <param name="fileName">The path to the data file.</param>
    /// <returns>The loaded <see cref="Iso6392Data"/>.</returns>
    /// <exception cref="IOException">Thrown when the file cannot be read.</exception>
    /// <exception cref="InvalidDataException">Thrown when the file contains invalid data.</exception>
    public static async Task<Iso6392Data> FromDataAsync(string fileName)
    {
        Iso6392Data iso6392Data = new();
        await iso6392Data.LoadDataAsync(fileName).ConfigureAwait(false);
        return iso6392Data;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA2007:Consider calling ConfigureAwait on the awaited task",
        Justification = "https://github.com/dotnet/roslyn-analyzers/issues/7185"
    )]
    private async Task LoadDataAsync(string fileName)
    {
        // https://www.loc.gov/standards/iso639-2/ascii_8bits.html
        // Alpha-3 (bibliographic) code
        // Alpha-3 (terminologic) code (when given)
        // Alpha-2 code (when given)
        // English name
        // French name (when given)
        // | deliminator
        // LF line terminator

        try
        {
            // Read line by line
            List<Iso6392Record> recordList = [];
            await using FileStream fileStream = new(
                fileName,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan
            );
            using StreamReader lineReader = new(fileStream);
            while (await lineReader.ReadLineAsync().ConfigureAwait(false) is { } line)
            {
                // Parse using pipe character
                List<string> records = [.. line.Split('|').Select(item => item.Trim())];
                if (records.Count != 5)
                {
                    throw new InvalidDataException(
                        $"Invalid data found in ISO 639-2 record: {line}"
                    );
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
                    throw new InvalidDataException(
                        $"Invalid data found in ISO 639-2 record: {line}"
                    );
                }
                recordList.Add(record);
            }

            if (recordList.Count == 0)
            {
                Log.LogDataLoadEmpty(nameof(Iso6392Data), fileName);
                throw new InvalidDataException($"No data found in ISO 639-2 file: {fileName}");
            }

            RecordList = [.. recordList];
            Log.LogDataLoaded(nameof(Iso6392Data), fileName, RecordList.Length);
        }
        catch (Exception exception)
        {
            Log.LogDataLoadFailed(nameof(Iso6392Data), fileName, exception);
            throw;
        }
    }

    /// <summary>
    /// Creates an <see cref="Iso6392Data"/> instance from a JSON file asynchronously.
    /// </summary>
    /// <param name="fileName">The path to the JSON file.</param>
    /// <returns>The loaded <see cref="Iso6392Data"/>.</returns>
    /// <exception cref="InvalidDataException">Thrown when the file contains invalid data.</exception>
    /// <exception cref="IOException">Thrown when the file cannot be read.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid.</exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA2007:Consider calling ConfigureAwait on the awaited task",
        Justification = "https://github.com/dotnet/roslyn-analyzers/issues/7185"
    )]
    public static async Task<Iso6392Data> FromJsonAsync(string fileName)
    {
        ILogger logger = LogOptions.CreateLogger<Iso6392Data>();
        try
        {
            await using FileStream fileStream = new(
                fileName,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan
            );
            Iso6392Data? data = await JsonSerializer
                .DeserializeAsync(fileStream, LanguageJsonContext.Default.Iso6392Data)
                .ConfigureAwait(false);
            if (data == null)
            {
                logger.LogDataLoadEmpty(nameof(Iso6392Data), fileName);
                throw new InvalidDataException($"No data found in ISO 639-2 file: {fileName}");
            }

            logger.LogDataLoaded(nameof(Iso6392Data), fileName, data.RecordList.Length);
            return data;
        }
        catch (Exception exception)
        {
            logger.LogDataLoadFailed(nameof(Iso6392Data), fileName, exception);
            throw;
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA2007:Consider calling ConfigureAwait on the awaited task",
        Justification = "https://github.com/dotnet/roslyn-analyzers/issues/7185"
    )]
    internal async Task SaveJsonAsync(string fileName)
    {
        await using FileStream fileStream = new(
            fileName,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan
        );
        await JsonSerializer
            .SerializeAsync(fileStream, this, LanguageJsonContext.Default.Iso6392Data)
            .ConfigureAwait(false);
    }

    internal async Task SaveCodeAsync(string fileName)
    {
        using StreamWriter writer = new(
            new FileStream(
                fileName,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan
            ),
            new UTF8Encoding(false)
        )
        {
            NewLine = "\r\n",
        };

        await WriteLineAsync("namespace ptr727.LanguageTags;");
        await WriteLineAsync(string.Empty);
        await WriteLineAsync("/// <summary>");
        await WriteLineAsync("/// Provides access to ISO 639-2 language code data.");
        await WriteLineAsync("/// </summary>");
        await WriteLineAsync(
            $"[System.CodeDom.Compiler.GeneratedCode(\"{typeof(Iso6392Data).FullName}\", \"1.0\")]"
        );
        await WriteLineAsync("public sealed partial class Iso6392Data");
        await WriteLineAsync("{");
        await WriteLineAsync("    /// <summary>");
        await WriteLineAsync(
            "    /// Creates an instance loaded from the embedded ISO 639-2 dataset."
        );
        await WriteLineAsync("    /// </summary>");
        await WriteLineAsync(
            "    /// <returns>The populated <see cref=\"Iso6392Data\"/> instance.</returns>"
        );
        await WriteLineAsync("    public static Iso6392Data Create() =>");
        await WriteLineAsync("        new()");
        await WriteLineAsync("        {");
        await WriteLineAsync("            RecordList =");
        await WriteLineAsync("            [");

        foreach (Iso6392Record record in RecordList)
        {
            await WriteLineAsync("                new()");
            await WriteLineAsync("                {");
            await WriteLineAsync(
                $"                    Part2B = {LanguageSchema.GetCodeGenString(record.Part2B)},"
            );
            await WriteLineAsync(
                $"                    Part2T = {LanguageSchema.GetCodeGenString(record.Part2T)},"
            );
            await WriteLineAsync(
                $"                    Part1 = {LanguageSchema.GetCodeGenString(record.Part1)},"
            );
            await WriteLineAsync(
                $"                    RefName = {LanguageSchema.GetCodeGenString(record.RefName)},"
            );
            await WriteLineAsync("                },");
        }

        await WriteLineAsync("            ],");
        await WriteLineAsync("        };");
        await WriteLineAsync("}");
        return;

        ConfiguredTaskAwaitable WriteLineAsync(string value) =>
            writer.WriteLineAsync(value).ConfigureAwait(false);
    }

    /// <summary>
    /// Finds an ISO 639-2 language record by language code or description.
    /// </summary>
    /// <remarks>
    /// Matching is case-insensitive and checks Part 2/B, Part 2/T, Part 1, then (optionally) reference name.
    /// Null or empty values return null.
    /// </remarks>
    /// <param name="languageTag">The language code or description to search for.</param>
    /// <param name="includeDescription">If true, searches in the reference name field; otherwise, only searches language codes.</param>
    /// <returns>The first matching <see cref="Iso6392Record"/>, or null when no match is found.</returns>
    public Iso6392Record? Find(string languageTag, bool includeDescription)
    {
        if (string.IsNullOrEmpty(languageTag))
        {
            Log.LogFindRecordNotFound(nameof(Iso6392Data), languageTag, includeDescription);
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
                Log.LogFindRecordFound(nameof(Iso6392Data), languageTag, includeDescription);
                return record;
            }

            // Try the 639-2/T
            record = RecordList.FirstOrDefault(item =>
                !string.IsNullOrEmpty(item.Part2T)
                && item.Part2T.Equals(languageTag, StringComparison.OrdinalIgnoreCase)
            );
            if (record != null)
            {
                Log.LogFindRecordFound(nameof(Iso6392Data), languageTag, includeDescription);
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
                Log.LogFindRecordFound(nameof(Iso6392Data), languageTag, includeDescription);
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
                Log.LogFindRecordFound(nameof(Iso6392Data), languageTag, includeDescription);
                return record;
            }

            // Partial match
            record = RecordList.FirstOrDefault(item =>
                !string.IsNullOrEmpty(item.RefName)
                && item.RefName.Contains(languageTag, StringComparison.OrdinalIgnoreCase)
            );
            if (record != null)
            {
                Log.LogFindRecordFound(nameof(Iso6392Data), languageTag, includeDescription);
                return record;
            }
        }

        // Not found
        Log.LogFindRecordNotFound(nameof(Iso6392Data), languageTag, includeDescription);
        return null;
    }
}

/// <summary>
/// Represents an ISO 639-2 language code record.
/// </summary>
public sealed record Iso6392Record
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
