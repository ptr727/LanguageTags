using System.Runtime.CompilerServices;

namespace ptr727.LanguageTags;

/// <summary>
/// Provides access to ISO 639-3 language code data.
/// </summary>
public sealed partial class Iso6393Data
{
    internal const string DataUri =
        "https://iso639-3.sil.org/sites/iso639-3/files/downloads/iso-639-3.tab";
    internal const string DataFileName = "iso6393";

    /// <summary>
    /// Loads ISO 639-3 data from a file asynchronously.
    /// </summary>
    /// <param name="fileName">The path to the data file.</param>
    /// <returns>The loaded <see cref="Iso6393Data"/>.</returns>
    /// <exception cref="InvalidDataException">Thrown when the file contains invalid data.</exception>
    public static Task<Iso6393Data> LoadDataAsync(string fileName) =>
        LoadDataAsync(fileName, LogOptions.CreateLogger<Iso6393Data>());

    /// <summary>
    /// Loads ISO 639-3 data from a file asynchronously using the specified options.
    /// </summary>
    /// <param name="fileName">The path to the data file.</param>
    /// <param name="options">The options used to configure logging.</param>
    /// <returns>The loaded <see cref="Iso6393Data"/>.</returns>
    /// <exception cref="InvalidDataException">Thrown when the file contains invalid data.</exception>
    public static Task<Iso6393Data> LoadDataAsync(string fileName, Options? options) =>
        LoadDataAsync(fileName, LogOptions.CreateLogger<Iso6393Data>(options));

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA2007:Consider calling ConfigureAwait on the awaited task",
        Justification = "https://github.com/dotnet/roslyn-analyzers/issues/7185"
    )]
    private static async Task<Iso6393Data> LoadDataAsync(string fileName, ILogger logger)
    {
        // https://iso639-3.sil.org/code_tables/download_tables
        // Id char(3) NOT NULL, The three-letter 639-3 identifier
        // Part2B char(3) NULL, Equivalent 639-2 identifier of the bibliographic applications code set, if there is one
        // Part2T char(3) NULL, Equivalent 639-2 identifier of the terminology applications code set, if there is one
        // Part1 char(2) NULL, Equivalent 639-1 identifier, if there is one
        // Scope char(1) NOT NULL, I(ndividual), M(acrolanguage), S(pecial)
        // Type char(1) NOT NULL, A(ncient), C(onstructed), E(xtinct), H(istorical), L(iving), S(pecial)
        // Ref_Name varchar(150) NOT NULL, Reference language name
        // Comment varchar(150) NULL) Comment relating to one or more of the columns

        try
        {
            // Read header
            // Id	Part2b	Part2t	Part1	Scope	Language_Type	Ref_Name	Comment
            List<Iso6393Record> recordList = [];
            await using FileStream fileStream = new(
                fileName,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan
            );
            using StreamReader lineReader = new(fileStream);
            string? line = await lineReader.ReadLineAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(line))
            {
                throw new InvalidDataException(
                    $"Missing header line in ISO 639-3 file: {fileName}"
                );
            }
            List<string> records = [.. line.Split('\t').Select(item => item.Trim())];
            if (records.Count != 8)
            {
                throw new InvalidDataException($"Invalid data found in ISO 639-3 record: {line}");
            }

            // Read line by line
            while ((line = await lineReader.ReadLineAsync().ConfigureAwait(false)) is not null)
            {
                // Parse using tab character
                records = [.. line.Split('\t').Select(item => item.Trim())];
                if (records.Count != 8)
                {
                    throw new InvalidDataException(
                        $"Invalid data found in ISO 639-3 record: {line}"
                    );
                }

                // Populate record
                Iso6393Record record = new()
                {
                    Id = string.IsNullOrEmpty(records[0]) ? null : records[0],
                    Part2B = string.IsNullOrEmpty(records[1]) ? null : records[1],
                    Part2T = string.IsNullOrEmpty(records[2]) ? null : records[2],
                    Part1 = string.IsNullOrEmpty(records[3]) ? null : records[3],
                    Scope = string.IsNullOrEmpty(records[4]) ? null : records[4],
                    LanguageType = string.IsNullOrEmpty(records[5]) ? null : records[5],
                    RefName = string.IsNullOrEmpty(records[6]) ? null : records[6],
                    Comment = string.IsNullOrEmpty(records[7]) ? null : records[7],
                };
                if (
                    string.IsNullOrEmpty(record.Id)
                    || string.IsNullOrEmpty(record.Scope)
                    || string.IsNullOrEmpty(record.LanguageType)
                    || string.IsNullOrEmpty(record.RefName)
                )
                {
                    throw new InvalidDataException(
                        $"Invalid data found in ISO 639-3 record: {line}"
                    );
                }
                recordList.Add(record);
            }

            if (recordList.Count == 0)
            {
                logger.LogDataLoadEmpty(nameof(Iso6393Data), fileName);
                throw new InvalidDataException($"No data found in ISO 639-3 file: {fileName}");
            }

            Iso6393Data data = new() { RecordList = [.. recordList] };
            logger.LogDataLoaded(nameof(Iso6393Data), fileName, data.RecordList.Length);
            return data;
        }
        catch (Exception exception)
        {
            logger.LogDataLoadFailed(nameof(Iso6393Data), fileName, exception);
            throw;
        }
    }

    /// <summary>
    /// Loads ISO 639-3 data from a JSON file asynchronously.
    /// </summary>
    /// <param name="fileName">The path to the JSON file.</param>
    /// <returns>
    /// The loaded <see cref="Iso6393Data"/>, or null when deserialization yields no data.
    /// </returns>
    /// <exception cref="IOException">Thrown when the file cannot be read.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid.</exception>
    public static Task<Iso6393Data?> LoadJsonAsync(string fileName) =>
        LoadJsonAsync(fileName, LogOptions.CreateLogger<Iso6393Data>());

    /// <summary>
    /// Loads ISO 639-3 data from a JSON file asynchronously using the specified options.
    /// </summary>
    /// <param name="fileName">The path to the JSON file.</param>
    /// <param name="options">The options used to configure logging.</param>
    /// <returns>
    /// The loaded <see cref="Iso6393Data"/>, or null when deserialization yields no data.
    /// </returns>
    /// <exception cref="IOException">Thrown when the file cannot be read.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid.</exception>
    public static Task<Iso6393Data?> LoadJsonAsync(string fileName, Options? options) =>
        LoadJsonAsync(fileName, LogOptions.CreateLogger<Iso6393Data>(options));

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA2007:Consider calling ConfigureAwait on the awaited task",
        Justification = "https://github.com/dotnet/roslyn-analyzers/issues/7185"
    )]
    private static async Task<Iso6393Data?> LoadJsonAsync(string fileName, ILogger logger)
    {
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
            Iso6393Data? data = await JsonSerializer
                .DeserializeAsync(fileStream, LanguageJsonContext.Default.Iso6393Data)
                .ConfigureAwait(false);
            if (data == null)
            {
                logger.LogDataLoadEmpty(nameof(Iso6393Data), fileName);
            }
            else
            {
                logger.LogDataLoaded(nameof(Iso6393Data), fileName, data.RecordList.Length);
            }

            return data;
        }
        catch (Exception exception)
        {
            logger.LogDataLoadFailed(nameof(Iso6393Data), fileName, exception);
            throw;
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA2007:Consider calling ConfigureAwait on the awaited task",
        Justification = "https://github.com/dotnet/roslyn-analyzers/issues/7185"
    )]
    internal static async Task SaveJsonAsync(string fileName, Iso6393Data iso6393)
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
            .SerializeAsync(fileStream, iso6393, LanguageJsonContext.Default.Iso6393Data)
            .ConfigureAwait(false);
    }

    internal static async Task GenCodeAsync(string fileName, Iso6393Data iso6393)
    {
        ArgumentNullException.ThrowIfNull(iso6393);

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
        await WriteLineAsync("/// Provides access to ISO 639-3 language code data.");
        await WriteLineAsync("/// </summary>");
        await WriteLineAsync(
            $"[System.CodeDom.Compiler.GeneratedCode(\"{typeof(Iso6393Data).FullName}\", \"1.0\")]"
        );
        await WriteLineAsync("public sealed partial class Iso6393Data");
        await WriteLineAsync("{");
        await WriteLineAsync("    /// <summary>");
        await WriteLineAsync(
            "    /// Creates an instance loaded from the embedded ISO 639-3 dataset."
        );
        await WriteLineAsync("    /// </summary>");
        await WriteLineAsync(
            "    /// <returns>The populated <see cref=\"Iso6393Data\"/> instance.</returns>"
        );
        await WriteLineAsync("    public static Iso6393Data Create() =>");
        await WriteLineAsync("        new()");
        await WriteLineAsync("        {");
        await WriteLineAsync("            RecordList =");
        await WriteLineAsync("            [");

        foreach (Iso6393Record record in iso6393.RecordList)
        {
            await WriteLineAsync("                new()");
            await WriteLineAsync("                {");
            await WriteLineAsync(
                $"                    Id = {LanguageSchema.GetCodeGenString(record.Id)},"
            );
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
                $"                    Scope = {LanguageSchema.GetCodeGenString(record.Scope)},"
            );
            await WriteLineAsync(
                $"                    LanguageType = {LanguageSchema.GetCodeGenString(record.LanguageType)},"
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
    /// Gets the collection of ISO 639-3 language records.
    /// </summary>
    public required ImmutableArray<Iso6393Record> RecordList { get; init; }

    /// <summary>
    /// Finds an ISO 639-3 language record by language code or description.
    /// </summary>
    /// <remarks>
    /// Matching is case-insensitive and checks Id, Part 2/B, Part 2/T, Part 1, then (optionally) reference name.
    /// </remarks>
    /// <param name="languageTag">The language code or description to search for.</param>
    /// <param name="includeDescription">If true, searches in the reference name field; otherwise, only searches language codes.</param>
    /// <returns>The matching <see cref="Iso6393Record"/> or null if not found.</returns>
    public Iso6393Record? Find(string? languageTag, bool includeDescription) =>
        Find(languageTag, includeDescription, LogOptions.CreateLogger<Iso6393Data>());

    /// <summary>
    /// Finds an ISO 639-3 language record by language code or description using the specified options.
    /// </summary>
    /// <remarks>
    /// Matching is case-insensitive and checks Id, Part 2/B, Part 2/T, Part 1, then (optionally) reference name.
    /// </remarks>
    /// <param name="languageTag">The language code or description to search for.</param>
    /// <param name="includeDescription">If true, searches in the reference name field; otherwise, only searches language codes.</param>
    /// <param name="options">The options used to configure logging.</param>
    /// <returns>The matching <see cref="Iso6393Record"/> or null if not found.</returns>
    public Iso6393Record? Find(string? languageTag, bool includeDescription, Options? options) =>
        Find(languageTag, includeDescription, LogOptions.CreateLogger<Iso6393Data>(options));

    private Iso6393Record? Find(string? languageTag, bool includeDescription, ILogger logger)
    {
        if (string.IsNullOrEmpty(languageTag))
        {
            logger.LogFindRecordNotFound(nameof(Iso6393Data), languageTag, includeDescription);
            return null;
        }

        // Find the matching language entry
        Iso6393Record? record = null;

        // 693 3 letter form
        if (languageTag.Length == 3)
        {
            // Try 639-3
            record = RecordList.FirstOrDefault(item =>
                !string.IsNullOrEmpty(item.Id)
                && item.Id.Equals(languageTag, StringComparison.OrdinalIgnoreCase)
            );
            if (record != null)
            {
                logger.LogFindRecordFound(nameof(Iso6393Data), languageTag, includeDescription);
                return record;
            }

            // Try the 639-2/B
            record = RecordList.FirstOrDefault(item =>
                !string.IsNullOrEmpty(item.Part2B)
                && item.Part2B.Equals(languageTag, StringComparison.OrdinalIgnoreCase)
            );
            if (record != null)
            {
                logger.LogFindRecordFound(nameof(Iso6393Data), languageTag, includeDescription);
                return record;
            }

            // Try the 639-2/T
            record = RecordList.FirstOrDefault(item =>
                !string.IsNullOrEmpty(item.Part2T)
                && item.Part2T.Equals(languageTag, StringComparison.OrdinalIgnoreCase)
            );
            if (record != null)
            {
                logger.LogFindRecordFound(nameof(Iso6393Data), languageTag, includeDescription);
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
                logger.LogFindRecordFound(nameof(Iso6393Data), languageTag, includeDescription);
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
                logger.LogFindRecordFound(nameof(Iso6393Data), languageTag, includeDescription);
                return record;
            }

            // Partial match
            record = RecordList.FirstOrDefault(item =>
                !string.IsNullOrEmpty(item.RefName)
                && item.RefName.Contains(languageTag, StringComparison.OrdinalIgnoreCase)
            );
            if (record != null)
            {
                logger.LogFindRecordFound(nameof(Iso6393Data), languageTag, includeDescription);
                return record;
            }
        }

        // Not found
        logger.LogFindRecordNotFound(nameof(Iso6393Data), languageTag, includeDescription);
        return null;
    }
}

/// <summary>
/// Represents an ISO 639-3 language code record.
/// </summary>
public sealed record Iso6393Record
{
    /// <summary>
    /// Gets the ISO 639-3 identifier (3 letters).
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// Gets the equivalent ISO 639-2/B bibliographic code (3 letters).
    /// </summary>
    public string? Part2B { get; init; }

    /// <summary>
    /// Gets the equivalent ISO 639-2/T terminology code (3 letters).
    /// </summary>
    public string? Part2T { get; init; }

    /// <summary>
    /// Gets the equivalent ISO 639-1 code (2 letters).
    /// </summary>
    public string? Part1 { get; init; }

    /// <summary>
    /// Gets the scope of the language: I(ndividual), M(acrolanguage), or S(pecial).
    /// </summary>
    public string? Scope { get; init; }

    /// <summary>
    /// Gets the type of the language: A(ncient), C(onstructed), E(xtinct), H(istorical), L(iving), or S(pecial).
    /// </summary>
    public string? LanguageType { get; init; }

    /// <summary>
    /// Gets the reference name of the language.
    /// </summary>
    public string? RefName { get; init; }

    /// <summary>
    /// Gets additional comments about the language.
    /// </summary>
    public string? Comment { get; init; }
}
