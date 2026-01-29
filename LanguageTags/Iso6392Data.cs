namespace ptr727.LanguageTags;

/// <summary>
/// Provides access to ISO 639-2 language code data.
/// </summary>
public sealed partial class Iso6392Data
{
    internal const string DataUri = "https://www.loc.gov/standards/iso639-2/ISO-639-2_utf-8.txt";
    internal const string DataFileName = "iso6392";

    /// <summary>
    /// Loads ISO 639-2 data from a file asynchronously.
    /// </summary>
    /// <param name="fileName">The path to the data file.</param>
    /// <returns>The loaded <see cref="Iso6392Data"/>.</returns>
    /// <exception cref="InvalidDataException">Thrown when the file contains invalid data.</exception>
    public static Task<Iso6392Data> LoadDataAsync(string fileName) =>
        LoadDataAsync(fileName, LogOptions.CreateLogger<Iso6392Data>());

    /// <summary>
    /// Loads ISO 639-2 data from a file asynchronously using the specified options.
    /// </summary>
    /// <param name="fileName">The path to the data file.</param>
    /// <param name="options">The options used to configure logging.</param>
    /// <returns>The loaded <see cref="Iso6392Data"/>.</returns>
    /// <exception cref="InvalidDataException">Thrown when the file contains invalid data.</exception>
    public static Task<Iso6392Data> LoadDataAsync(string fileName, Options? options) =>
        LoadDataAsync(fileName, LogOptions.CreateLogger<Iso6392Data>(options));

    private static async Task<Iso6392Data> LoadDataAsync(string fileName, ILogger logger)
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
            FileStream fileStream = new(
                fileName,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan
            );
            await using (fileStream.ConfigureAwait(false))
            {
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
            }

            if (recordList.Count == 0)
            {
                logger.LogDataLoadEmpty(nameof(Iso6392Data), fileName);
                throw new InvalidDataException($"No data found in ISO 639-2 file: {fileName}");
            }

            Iso6392Data data = new() { RecordList = [.. recordList] };
            logger.LogDataLoaded(nameof(Iso6392Data), fileName, data.RecordList.Length);
            return data;
        }
        catch (Exception exception)
        {
            logger.LogDataLoadFailed(nameof(Iso6392Data), fileName, exception);
            throw;
        }
    }

    /// <summary>
    /// Loads ISO 639-2 data from a JSON file asynchronously.
    /// </summary>
    /// <param name="fileName">The path to the JSON file.</param>
    /// <returns>The loaded <see cref="Iso6392Data"/> or null if deserialization fails.</returns>
    public static Task<Iso6392Data?> LoadJsonAsync(string fileName) =>
        LoadJsonAsync(fileName, LogOptions.CreateLogger<Iso6392Data>());

    /// <summary>
    /// Loads ISO 639-2 data from a JSON file asynchronously using the specified options.
    /// </summary>
    /// <param name="fileName">The path to the JSON file.</param>
    /// <param name="options">The options used to configure logging.</param>
    /// <returns>The loaded <see cref="Iso6392Data"/> or null if deserialization fails.</returns>
    public static Task<Iso6392Data?> LoadJsonAsync(string fileName, Options? options) =>
        LoadJsonAsync(fileName, LogOptions.CreateLogger<Iso6392Data>(options));

    private static async Task<Iso6392Data?> LoadJsonAsync(string fileName, ILogger logger)
    {
        try
        {
            FileStream fileStream = new(
                fileName,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan
            );
            await using (fileStream.ConfigureAwait(false))
            {
                Iso6392Data? data = await JsonSerializer
                    .DeserializeAsync(fileStream, LanguageJsonContext.Default.Iso6392Data)
                    .ConfigureAwait(false);
                if (data == null)
                {
                    logger.LogDataLoadEmpty(nameof(Iso6392Data), fileName);
                }
                else
                {
                    logger.LogDataLoaded(nameof(Iso6392Data), fileName, data.RecordList.Length);
                }

                return data;
            }
        }
        catch (Exception exception)
        {
            logger.LogDataLoadFailed(nameof(Iso6392Data), fileName, exception);
            throw;
        }
    }

    internal static async Task SaveJsonAsync(string fileName, Iso6392Data iso6392)
    {
        FileStream fileStream = new(
            fileName,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan
        );
        await using (fileStream.ConfigureAwait(false))
        {
            await JsonSerializer
                .SerializeAsync(fileStream, iso6392, LanguageJsonContext.Default.Iso6392Data)
                .ConfigureAwait(false);
        }
    }

    internal static async Task GenCodeAsync(string fileName, Iso6392Data iso6392)
    {
        ArgumentNullException.ThrowIfNull(iso6392);
        FileStream fileStream = new(
            fileName,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan
        );
        await using (fileStream.ConfigureAwait(false))
        {
            StreamWriter writer = new(fileStream, new UTF8Encoding(false)) { NewLine = "\r\n" };
            await using (writer.ConfigureAwait(false))
            {
                System.Runtime.CompilerServices.ConfiguredTaskAwaitable WriteLineAsync(
                    string value
                ) => writer.WriteLineAsync(value).ConfigureAwait(false);

                await WriteLineAsync("namespace ptr727.LanguageTags;");
                await WriteLineAsync(string.Empty);
                await WriteLineAsync("/// <summary>");
                await WriteLineAsync("/// Provides access to ISO 639-2 language code data.");
                await WriteLineAsync("/// </summary>");
                await WriteLineAsync("public sealed partial class Iso6392Data");
                await WriteLineAsync("{");
                await WriteLineAsync("    public static Iso6392Data Create() =>");
                await WriteLineAsync("        new()");
                await WriteLineAsync("        {");
                await WriteLineAsync("            RecordList =");
                await WriteLineAsync("            [");

                foreach (Iso6392Record record in iso6392.RecordList)
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
            }
        }
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
    public Iso6392Record? Find(string? languageTag, bool includeDescription) =>
        Find(languageTag, includeDescription, LogOptions.CreateLogger<Iso6392Data>());

    /// <summary>
    /// Finds an ISO 639-2 language record by language code or description using the specified options.
    /// </summary>
    /// <param name="languageTag">The language code or description to search for.</param>
    /// <param name="includeDescription">If true, searches in the reference name field; otherwise, only searches language codes.</param>
    /// <param name="options">The options used to configure logging.</param>
    /// <returns>The matching <see cref="Iso6392Record"/> or null if not found.</returns>
    public Iso6392Record? Find(string? languageTag, bool includeDescription, Options? options) =>
        Find(languageTag, includeDescription, LogOptions.CreateLogger<Iso6392Data>(options));

    private Iso6392Record? Find(string? languageTag, bool includeDescription, ILogger logger)
    {
        if (string.IsNullOrEmpty(languageTag))
        {
            logger.LogFindRecordNotFound(nameof(Iso6392Data), languageTag, includeDescription);
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
                logger.LogFindRecordFound(nameof(Iso6392Data), languageTag, includeDescription);
                return record;
            }

            // Try the 639-2/T
            record = RecordList.FirstOrDefault(item =>
                !string.IsNullOrEmpty(item.Part2T)
                && item.Part2T.Equals(languageTag, StringComparison.OrdinalIgnoreCase)
            );
            if (record != null)
            {
                logger.LogFindRecordFound(nameof(Iso6392Data), languageTag, includeDescription);
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
                logger.LogFindRecordFound(nameof(Iso6392Data), languageTag, includeDescription);
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
                logger.LogFindRecordFound(nameof(Iso6392Data), languageTag, includeDescription);
                return record;
            }

            // Partial match
            record = RecordList.FirstOrDefault(item =>
                !string.IsNullOrEmpty(item.RefName)
                && item.RefName.Contains(languageTag, StringComparison.OrdinalIgnoreCase)
            );
            if (record != null)
            {
                logger.LogFindRecordFound(nameof(Iso6392Data), languageTag, includeDescription);
                return record;
            }
        }

        // Not found
        logger.LogFindRecordNotFound(nameof(Iso6392Data), languageTag, includeDescription);
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
