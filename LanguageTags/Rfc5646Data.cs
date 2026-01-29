using System.Text.Json.Serialization;

namespace ptr727.LanguageTags;

/// <summary>
/// Provides access to RFC 5646 / BCP 47 language subtag registry data.
/// </summary>
public sealed partial class Rfc5646Data
{
    internal const string DataUri =
        "https://www.iana.org/assignments/language-subtag-registry/language-subtag-registry";
    internal const string DataFileName = "rfc5646";

    /// <summary>
    /// Loads RFC 5646 data from a file asynchronously.
    /// </summary>
    /// <param name="fileName">The path to the data file.</param>
    /// <returns>The loaded <see cref="Rfc5646Data"/>.</returns>
    /// <exception cref="InvalidDataException">Thrown when the file contains invalid data.</exception>
    public static Task<Rfc5646Data> LoadDataAsync(string fileName) =>
        LoadDataAsync(fileName, LogOptions.CreateLogger<Rfc5646Data>());

    /// <summary>
    /// Loads RFC 5646 data from a file asynchronously using the specified options.
    /// </summary>
    /// <param name="fileName">The path to the data file.</param>
    /// <param name="options">The options used to configure logging.</param>
    /// <returns>The loaded <see cref="Rfc5646Data"/>.</returns>
    /// <exception cref="InvalidDataException">Thrown when the file contains invalid data.</exception>
    public static Task<Rfc5646Data> LoadDataAsync(string fileName, Options? options) =>
        LoadDataAsync(fileName, LogOptions.CreateLogger<Rfc5646Data>(options));

    private static async Task<Rfc5646Data> LoadDataAsync(string fileName, ILogger logger)
    {
        // File Format
        // https://www.rfc-editor.org/rfc/rfc5646#section-3.1

        // https://www.w3.org/International/articles/language-tags
        // https://datatracker.ietf.org/doc/html/draft-phillips-record-jar-02

        try
        {
            List<Rfc5646Record> recordList = [];
            Parser parser = new();
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

                // First record is file date
                _ = await parser.ReadAttributesAsync(lineReader).ConfigureAwait(false);
                DateOnly fileDate = parser.GetFileDate();

                // Read all record attributes separated by %% until EOF
                while (await parser.ReadAttributesAsync(lineReader).ConfigureAwait(false))
                {
                    recordList.Add(parser.GetRecord());
                }
                recordList.Add(parser.GetRecord());

                if (recordList.Count == 0)
                {
                    logger.LogDataLoadEmpty(nameof(Rfc5646Data), fileName);
                }

                Rfc5646Data data = new() { FileDate = fileDate, RecordList = [.. recordList] };
                logger.LogDataLoaded(nameof(Rfc5646Data), fileName, data.RecordList.Length);
                return data;
            }
        }
        catch (Exception exception)
        {
            logger.LogDataLoadFailed(nameof(Rfc5646Data), fileName, exception);
            throw;
        }
    }

    /// <summary>
    /// Loads RFC 5646 data from a JSON file asynchronously.
    /// </summary>
    /// <param name="fileName">The path to the JSON file.</param>
    /// <returns>The loaded <see cref="Rfc5646Data"/> or null if deserialization fails.</returns>
    public static Task<Rfc5646Data?> LoadJsonAsync(string fileName) =>
        LoadJsonAsync(fileName, LogOptions.CreateLogger<Rfc5646Data>());

    /// <summary>
    /// Loads RFC 5646 data from a JSON file asynchronously using the specified options.
    /// </summary>
    /// <param name="fileName">The path to the JSON file.</param>
    /// <param name="options">The options used to configure logging.</param>
    /// <returns>The loaded <see cref="Rfc5646Data"/> or null if deserialization fails.</returns>
    public static Task<Rfc5646Data?> LoadJsonAsync(string fileName, Options? options) =>
        LoadJsonAsync(fileName, LogOptions.CreateLogger<Rfc5646Data>(options));

    private static async Task<Rfc5646Data?> LoadJsonAsync(string fileName, ILogger logger)
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
                Rfc5646Data? data = await JsonSerializer
                    .DeserializeAsync(fileStream, LanguageJsonContext.Default.Rfc5646Data)
                    .ConfigureAwait(false);
                if (data == null)
                {
                    logger.LogDataLoadEmpty(nameof(Rfc5646Data), fileName);
                }
                else
                {
                    logger.LogDataLoaded(nameof(Rfc5646Data), fileName, data.RecordList.Length);
                }

                return data;
            }
        }
        catch (Exception exception)
        {
            logger.LogDataLoadFailed(nameof(Rfc5646Data), fileName, exception);
            throw;
        }
    }

    internal static async Task SaveJsonAsync(string fileName, Rfc5646Data rfc5646)
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
                .SerializeAsync(fileStream, rfc5646, LanguageJsonContext.Default.Rfc5646Data)
                .ConfigureAwait(false);
        }
    }

    internal static async Task GenCodeAsync(string fileName, Rfc5646Data rfc5646)
    {
        ArgumentNullException.ThrowIfNull(rfc5646);
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
                await WriteLineAsync(
                    "/// Provides access to RFC 5646 / BCP 47 language subtag registry data."
                );
                await WriteLineAsync("/// </summary>");
                await WriteLineAsync("public sealed partial class Rfc5646Data");
                await WriteLineAsync("{");
                await WriteLineAsync("    public static Rfc5646Data Create() =>");
                await WriteLineAsync("        new()");
                await WriteLineAsync("        {");
                await WriteLineAsync(
                    $"            FileDate = {LanguageSchema.GetCodeGenString(rfc5646.FileDate)},"
                );
                await WriteLineAsync("            RecordList =");
                await WriteLineAsync("            [");

                foreach (Rfc5646Record record in rfc5646.RecordList)
                {
                    await WriteLineAsync("                new()");
                    await WriteLineAsync("                {");
                    await WriteLineAsync(
                        $"                    Type = {LanguageSchema.GetCodeGenString(record.Type)},"
                    );
                    await WriteLineAsync(
                        $"                    SubTag = {LanguageSchema.GetCodeGenString(record.SubTag)},"
                    );
                    await WriteLineAsync(
                        $"                    Added = {LanguageSchema.GetCodeGenString(record.Added)},"
                    );
                    await WriteLineAsync(
                        $"                    SuppressScript = {LanguageSchema.GetCodeGenString(record.SuppressScript)},"
                    );
                    await WriteLineAsync(
                        $"                    Scope = {LanguageSchema.GetCodeGenString(record.Scope)},"
                    );
                    await WriteLineAsync(
                        $"                    MacroLanguage = {LanguageSchema.GetCodeGenString(record.MacroLanguage)},"
                    );
                    await WriteLineAsync(
                        $"                    Deprecated = {LanguageSchema.GetCodeGenString(record.Deprecated)},"
                    );
                    await WriteLineAsync(
                        $"                    PreferredValue = {LanguageSchema.GetCodeGenString(record.PreferredValue)},"
                    );
                    await WriteLineAsync(
                        $"                    Tag = {LanguageSchema.GetCodeGenString(record.Tag)},"
                    );
                    await WriteLineAsync(
                        $"                    Description = {LanguageSchema.GetCodeGenString(record.Description)},"
                    );
                    await WriteLineAsync(
                        $"                    Comments = {LanguageSchema.GetCodeGenString(record.Comments)},"
                    );
                    await WriteLineAsync(
                        $"                    Prefix = {LanguageSchema.GetCodeGenString(record.Prefix)},"
                    );
                    await WriteLineAsync("                },");
                }

                await WriteLineAsync("            ],");
                await WriteLineAsync("        };");
                await WriteLineAsync("}");
            }
        }
    }

    internal sealed class Parser
    {
        private readonly List<KeyValuePair<string, string>> _attributeList = [];
        private string? _pendingLine;

        public async Task<bool> ReadAttributesAsync(StreamReader lineReader)
        {
            // Read until %% or EOF
            _attributeList.Clear();
            bool eof = false;
            while (true)
            {
                // Read next line
                string? line = await ReadLineAsync(lineReader).ConfigureAwait(false);
                if (string.IsNullOrEmpty(line))
                {
                    // End of file
                    eof = true;
                    break;
                }
                if (line.Equals("%%", StringComparison.Ordinal))
                {
                    // End of record
                    break;
                }

                // First line should not be multiline
                if (line.StartsWith("  ", StringComparison.Ordinal))
                {
                    throw new InvalidDataException(
                        $"Invalid data found in RFC 5646 record: {line}"
                    );
                }

                // Multiline record starts with two spaces
                // Peek at the next line an look for a space
                while (true)
                {
                    // Read next line to check for multiline continuation
                    string? multiLine = await ReadLineAsync(lineReader).ConfigureAwait(false);
                    if (string.IsNullOrEmpty(multiLine))
                    {
                        _pendingLine = multiLine;
                        break;
                    }

                    if (!multiLine.StartsWith("  ", StringComparison.Ordinal))
                    {
                        _pendingLine = multiLine;
                        break;
                    }

                    line = $"{line.Trim()} {multiLine.Trim()}";
                }

                // Create attribute pair
                int divider = line.IndexOf(':', StringComparison.Ordinal);
                string key = line[..divider];
                string value = line[(divider + 1)..].Trim();

                // Add to attribute list
                _attributeList.Add(new KeyValuePair<string, string>(key, value));
            }

            return !eof;
        }

        private async Task<string?> ReadLineAsync(StreamReader lineReader)
        {
            if (_pendingLine is not null)
            {
                string? line = _pendingLine;
                _pendingLine = null;
                return line;
            }

            return await lineReader.ReadLineAsync().ConfigureAwait(false);
        }

        public Rfc5646Record GetRecord()
        {
            // Create a mutable tuple as placeholder
            (
                Rfc5646Record.RecordType Type,
                string? SubTag,
                List<string> Description,
                DateOnly? Added,
                string? SuppressScript,
                Rfc5646Record.RecordScope Scope,
                string? MacroLanguage,
                DateOnly? Deprecated,
                List<string> Comments,
                List<string> Prefix,
                string? PreferredValue,
                string? Tag
            ) record = (
                Type: Rfc5646Record.RecordType.None,
                SubTag: null,
                Description: [],
                Added: null,
                SuppressScript: null,
                Scope: Rfc5646Record.RecordScope.None,
                MacroLanguage: null,
                Deprecated: null,
                Comments: [],
                Prefix: [],
                PreferredValue: null,
                Tag: null
            );

            if (_attributeList.Count == 0)
            {
                throw new InvalidDataException("No data found in RFC 5646 record.");
            }
            foreach (KeyValuePair<string, string> pair in _attributeList)
            {
                switch (pair.Key.ToUpperInvariant())
                {
                    case "TYPE":
                        record.Type = TypeFromString(pair.Value);
                        break;
                    case "SUBTAG":
                        record.SubTag = pair.Value;
                        break;
                    case "DESCRIPTION":
                        record.Description.Add(pair.Value);
                        break;
                    case "ADDED":
                        record.Added = DateFromString(pair.Value);
                        break;
                    case "SUPPRESS-SCRIPT":
                        record.SuppressScript = pair.Value;
                        break;
                    case "SCOPE":
                        record.Scope = ScopeFromString(pair.Value);
                        break;
                    case "MACROLANGUAGE":
                        record.MacroLanguage = pair.Value;
                        break;
                    case "DEPRECATED":
                        record.Deprecated = DateFromString(pair.Value);
                        break;
                    case "COMMENTS":
                        record.Comments.Add(pair.Value);
                        break;
                    case "PREFIX":
                        record.Prefix.Add(pair.Value);
                        break;
                    case "PREFERRED-VALUE":
                        record.PreferredValue = pair.Value;
                        break;
                    case "TAG":
                        record.Tag = pair.Value;
                        break;
                    default:
                        throw new InvalidDataException(
                            $"Invalid data found in RFC 5646 record: {pair.Key}"
                        );
                }
            }
            return
                record.Type == Rfc5646Record.RecordType.None
                || (string.IsNullOrEmpty(record.Tag) && string.IsNullOrEmpty(record.SubTag))
                || record.Description.Count == 0
                ? throw new InvalidDataException("Invalid data found in RFC 5646 record")
                : new Rfc5646Record
                {
                    Type = record.Type,
                    SubTag = record.SubTag,
                    Description = [.. record.Description],
                    Added = record.Added,
                    SuppressScript = record.SuppressScript,
                    Scope = record.Scope,
                    MacroLanguage = record.MacroLanguage,
                    Deprecated = record.Deprecated,
                    Comments = [.. record.Comments],
                    Prefix = [.. record.Prefix],
                    PreferredValue = record.PreferredValue,
                    Tag = record.Tag,
                };
        }

        public DateOnly GetFileDate()
        {
            // First attribute is the date
            KeyValuePair<string, string> pair = _attributeList.First();
            return !pair.Key.Equals("File-Date", StringComparison.Ordinal)
                ? throw new InvalidDataException(
                    $"Invalid data found in RFC 5646 record: {pair.Key}"
                )
                : DateFromString(pair.Value);
        }

        private static DateOnly DateFromString(string value) =>
            DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Gets the file date of the language subtag registry.
    /// </summary>
    public required DateOnly? FileDate { get; init; }

    /// <summary>
    /// Gets the collection of RFC 5646 language subtag records.
    /// </summary>
    public ImmutableArray<Rfc5646Record> RecordList { get; init; }

    private static Rfc5646Record.RecordType TypeFromString(string value) =>
        value.ToUpperInvariant() switch
        {
            "LANGUAGE" => Rfc5646Record.RecordType.Language,
            "EXTLANG" => Rfc5646Record.RecordType.ExtLanguage,
            "SCRIPT" => Rfc5646Record.RecordType.Script,
            "VARIANT" => Rfc5646Record.RecordType.Variant,
            "GRANDFATHERED" => Rfc5646Record.RecordType.Grandfathered,
            "REGION" => Rfc5646Record.RecordType.Region,
            "REDUNDANT" => Rfc5646Record.RecordType.Redundant,
            _ => throw new InvalidDataException($"Invalid data found in RFC 5646 record: {value}"),
        };

    private static Rfc5646Record.RecordScope ScopeFromString(string value) =>
        value.ToUpperInvariant() switch
        {
            "MACROLANGUAGE" => Rfc5646Record.RecordScope.MacroLanguage,
            "COLLECTION" => Rfc5646Record.RecordScope.Collection,
            "SPECIAL" => Rfc5646Record.RecordScope.Special,
            "PRIVATE-USE" => Rfc5646Record.RecordScope.PrivateUse,
            _ => throw new InvalidDataException($"Invalid data found in RFC 5646 record: {value}"),
        };

    /// <summary>
    /// Finds a language subtag record by tag, subtag, preferred value, or description.
    /// </summary>
    /// <param name="languageTag">The language tag, subtag, or description to search for.</param>
    /// <param name="includeDescription">If true, searches in the description field; otherwise, only searches tags and subtags.</param>
    /// <returns>The matching <see cref="Rfc5646Record"/> or null if not found.</returns>
    public Rfc5646Record? Find(string? languageTag, bool includeDescription) =>
        Find(languageTag, includeDescription, LogOptions.CreateLogger<Rfc5646Data>());

    /// <summary>
    /// Finds a language subtag record by tag, subtag, preferred value, or description using the specified options.
    /// </summary>
    /// <param name="languageTag">The language tag, subtag, or description to search for.</param>
    /// <param name="includeDescription">If true, searches in the description field; otherwise, only searches tags and subtags.</param>
    /// <param name="options">The options used to configure logging.</param>
    /// <returns>The matching <see cref="Rfc5646Record"/> or null if not found.</returns>
    public Rfc5646Record? Find(string? languageTag, bool includeDescription, Options? options) =>
        Find(languageTag, includeDescription, LogOptions.CreateLogger<Rfc5646Data>(options));

    private Rfc5646Record? Find(string? languageTag, bool includeDescription, ILogger logger)
    {
        if (string.IsNullOrEmpty(languageTag))
        {
            logger.LogFindRecordNotFound(nameof(Rfc5646Data), languageTag, includeDescription);
            return null;
        }

        // Find the matching language entry
        Rfc5646Record? record = null;

        // Tag
        record = RecordList.FirstOrDefault(item =>
            !string.IsNullOrEmpty(item.Tag)
            && item.Tag.Equals(languageTag, StringComparison.OrdinalIgnoreCase)
        );
        if (record != null)
        {
            logger.LogFindRecordFound(nameof(Rfc5646Data), languageTag, includeDescription);
            return record;
        }

        // SubTag
        record = RecordList.FirstOrDefault(item =>
            !string.IsNullOrEmpty(item.SubTag)
            && item.SubTag.Equals(languageTag, StringComparison.OrdinalIgnoreCase)
        );
        if (record != null)
        {
            logger.LogFindRecordFound(nameof(Rfc5646Data), languageTag, includeDescription);
            return record;
        }

        // PreferredValue
        record = RecordList.FirstOrDefault(item =>
            !string.IsNullOrEmpty(item.PreferredValue)
            && item.PreferredValue.Equals(languageTag, StringComparison.OrdinalIgnoreCase)
        );
        if (record != null)
        {
            logger.LogFindRecordFound(nameof(Rfc5646Data), languageTag, includeDescription);
            return record;
        }

        // Description
        if (includeDescription)
        {
            // Exact match
            record = RecordList.FirstOrDefault(item =>
                item.Description.Any(description =>
                    description.Equals(languageTag, StringComparison.OrdinalIgnoreCase)
                )
            );
            if (record != null)
            {
                logger.LogFindRecordFound(nameof(Rfc5646Data), languageTag, includeDescription);
                return record;
            }

            // Partial match
            record = RecordList.FirstOrDefault(item =>
                item.Description.Any(description =>
                    description.Contains(languageTag, StringComparison.OrdinalIgnoreCase)
                )
            );
            if (record != null)
            {
                logger.LogFindRecordFound(nameof(Rfc5646Data), languageTag, includeDescription);
                return record;
            }
        }

        // Not found
        logger.LogFindRecordNotFound(nameof(Rfc5646Data), languageTag, includeDescription);
        return null;
    }
}

/// <summary>
/// Represents a record from the RFC 5646 / BCP 47 language subtag registry.
/// </summary>
public sealed record Rfc5646Record
{
    /// <summary>
    /// Defines the type of language subtag record.
    /// </summary>
    public enum RecordType
    {
        /// <summary>No type specified.</summary>
        None,

        /// <summary>Primary language subtag.</summary>
        Language,

        /// <summary>Extended language subtag.</summary>
        ExtLanguage,

        /// <summary>Script subtag (ISO 15924).</summary>
        Script,

        /// <summary>Variant subtag.</summary>
        Variant,

        /// <summary>Grandfathered tag.</summary>
        Grandfathered,

        /// <summary>Region subtag (ISO 3166-1 or UN M.49).</summary>
        Region,

        /// <summary>Redundant tag.</summary>
        Redundant,
    }

    /// <summary>
    /// Defines the scope of a language subtag.
    /// </summary>
    public enum RecordScope
    {
        /// <summary>No scope specified.</summary>
        None,

        /// <summary>Macrolanguage scope.</summary>
        MacroLanguage,

        /// <summary>Collection scope.</summary>
        Collection,

        /// <summary>Special scope.</summary>
        Special,

        /// <summary>Private use scope.</summary>
        PrivateUse,
    }

    /// <summary>
    /// Gets the type of this record (Language, Script, Region, etc.).
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<RecordType>))]
    public RecordType Type { get; init; }

    /// <summary>
    /// Gets the complete tag for grandfathered or redundant entries.
    /// </summary>
    public string? Tag { get; init; }

    /// <summary>
    /// Gets the subtag value (for language, script, region, variant, or extended language).
    /// </summary>
    public string? SubTag { get; init; }

    /// <summary>
    /// Gets the human-readable description(s) of this subtag.
    /// </summary>
    public ImmutableArray<string> Description { get; init; }

    /// <summary>
    /// Gets the date this subtag was added to the registry.
    /// </summary>
    public DateOnly? Added { get; init; }

    /// <summary>
    /// Gets the script that should be suppressed when used with this language.
    /// </summary>
    public string? SuppressScript { get; init; }

    /// <summary>
    /// Gets the scope of this subtag (macrolanguage, collection, special, or private use).
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<RecordScope>))]
    public RecordScope Scope { get; init; }

    /// <summary>
    /// Gets the macrolanguage this subtag belongs to.
    /// </summary>
    public string? MacroLanguage { get; init; }

    /// <summary>
    /// Gets the date this subtag was deprecated (if applicable).
    /// </summary>
    public DateOnly? Deprecated { get; init; }

    /// <summary>
    /// Gets additional comments about this subtag.
    /// </summary>
    public ImmutableArray<string> Comments { get; init; }

    /// <summary>
    /// Gets the prefix values that constrain where this subtag can be used.
    /// </summary>
    public ImmutableArray<string> Prefix { get; init; }

    /// <summary>
    /// Gets the preferred value to use instead of this subtag (for deprecated tags).
    /// </summary>
    public string? PreferredValue { get; init; }

    /// <summary>
    /// Gets either the Tag or SubTag value, whichever is set.
    /// </summary>
    [JsonIgnore]
    public string TagValue => string.IsNullOrEmpty(SubTag) ? Tag! : SubTag!;
}
