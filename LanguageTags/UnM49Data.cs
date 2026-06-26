using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Xml;

namespace ptr727.LanguageTags;

/// <summary>
/// Provides access to UN M.49 region containment data.
/// </summary>
public sealed partial class UnM49Data
{
    internal const string DataUri =
        "https://raw.githubusercontent.com/unicode-org/cldr/main/common/supplemental/supplementalData.xml";
    internal const string DataFileName = "unm49";

    private readonly Lazy<ILogger> _logger = new(LogOptions.CreateLogger<UnM49Data>);
    internal ILogger Log => _logger.Value;

    // Lazily built transitive ancestor index, code -> all containing group codes
    private readonly Lazy<Dictionary<string, ImmutableArray<string>>> _ancestorIndex;

    [JsonConstructor]
    internal UnM49Data() => _ancestorIndex = new(BuildAncestorIndex);

    /// <summary>
    /// Gets the collection of UN M.49 containment records.
    /// </summary>
    [JsonInclude]
    public ImmutableArray<UnM49Record> RecordList { get; internal set; } = [];

    /// <summary>
    /// Creates a <see cref="UnM49Data"/> instance from a data file asynchronously.
    /// </summary>
    /// <param name="fileName">The path to the data file.</param>
    /// <returns>The loaded <see cref="UnM49Data"/>.</returns>
    /// <exception cref="IOException">Thrown when the file cannot be read.</exception>
    /// <exception cref="InvalidDataException">Thrown when the file contains invalid data.</exception>
    public static async Task<UnM49Data> FromDataAsync(string fileName)
    {
        UnM49Data unM49Data = new();
        await unM49Data.LoadDataAsync(fileName).ConfigureAwait(false);
        return unM49Data;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA2007:Consider calling ConfigureAwait on the awaited task",
        Justification = "https://github.com/dotnet/roslyn-analyzers/issues/7185"
    )]
    private async Task LoadDataAsync(string fileName)
    {
        // UN M.49 territory containment from the CLDR supplemental data
        // https://github.com/unicode-org/cldr/blob/main/common/supplemental/supplementalData.xml
        // <territoryContainment>
        //   <group type="419" contains="013 029 005" grouping="true"/>
        //   <group type="013" contains="BZ CR GT HN MX NI PA SV"/>
        // Numeric types are UN M.49 region codes, 2 letter values are ISO 3166-1 country codes
        // The 419 Latin America macro region we care about is a grouping="true" overlay, so
        // grouping overlays must be kept, only status="deprecated" containment is skipped

        try
        {
            // Read group elements within the territoryContainment section
            List<UnM49Record> recordList = [];
            await using FileStream fileStream = new(
                fileName,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan
            );

            // XmlReader is AOT safe, do not use the reflection based XmlSerializer
            // CLDR data references an external DTD, ignore it instead of resolving it
            XmlReaderSettings settings = new()
            {
                Async = true,
                DtdProcessing = DtdProcessing.Ignore,
                XmlResolver = null,
                IgnoreComments = true,
                IgnoreWhitespace = true,
            };
            using XmlReader reader = XmlReader.Create(fileStream, settings);

            // Only process group elements inside the territoryContainment section
            bool inContainment = false;
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                // Enter the territoryContainment section
                if (
                    reader.NodeType == XmlNodeType.Element
                    && reader.Name.Equals("territoryContainment", StringComparison.Ordinal)
                )
                {
                    inContainment = true;
                    continue;
                }

                // The section is unique, stop reading the rest of the large file
                if (
                    reader.NodeType == XmlNodeType.EndElement
                    && reader.Name.Equals("territoryContainment", StringComparison.Ordinal)
                )
                {
                    break;
                }

                // Group element
                if (
                    !inContainment
                    || reader.NodeType != XmlNodeType.Element
                    || !reader.Name.Equals("group", StringComparison.Ordinal)
                )
                {
                    continue;
                }

                // Skip deprecated containment, keep canonical and grouping overlays
                string? status = reader.GetAttribute("status");
                if (string.Equals(status, "deprecated", StringComparison.Ordinal))
                {
                    continue;
                }

                // type is the parent code, contains is the space separated child codes
                string? type = reader.GetAttribute("type");
                string? contains = reader.GetAttribute("contains");
                if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(contains))
                {
                    continue;
                }

                // Populate record
                recordList.Add(
                    new UnM49Record
                    {
                        Code = type,
                        Contains =
                        [
                            .. contains.Split(
                                ' ',
                                StringSplitOptions.RemoveEmptyEntries
                                    | StringSplitOptions.TrimEntries
                            ),
                        ],
                    }
                );
            }

            if (recordList.Count == 0)
            {
                Log.LogDataLoadEmpty(nameof(UnM49Data), fileName);
                throw new InvalidDataException($"No data found in UN M.49 file: {fileName}");
            }

            RecordList = [.. recordList];
            Log.LogDataLoaded(nameof(UnM49Data), fileName, RecordList.Length);
        }
        catch (Exception exception)
        {
            Log.LogDataLoadFailed(nameof(UnM49Data), fileName, exception);
            throw;
        }
    }

    /// <summary>
    /// Creates a <see cref="UnM49Data"/> instance from a JSON file asynchronously.
    /// </summary>
    /// <param name="fileName">The path to the JSON file.</param>
    /// <returns>The loaded <see cref="UnM49Data"/>.</returns>
    /// <exception cref="InvalidDataException">Thrown when the file contains invalid data.</exception>
    /// <exception cref="IOException">Thrown when the file cannot be read.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid.</exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA2007:Consider calling ConfigureAwait on the awaited task",
        Justification = "https://github.com/dotnet/roslyn-analyzers/issues/7185"
    )]
    public static async Task<UnM49Data> FromJsonAsync(string fileName)
    {
        ILogger logger = LogOptions.CreateLogger<UnM49Data>();
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
            UnM49Data? data = await JsonSerializer
                .DeserializeAsync(fileStream, LanguageJsonContext.Default.UnM49Data)
                .ConfigureAwait(false);
            if (data == null)
            {
                logger.LogDataLoadEmpty(nameof(UnM49Data), fileName);
                throw new InvalidDataException($"No data found in UN M.49 file: {fileName}");
            }

            logger.LogDataLoaded(nameof(UnM49Data), fileName, data.RecordList.Length);
            return data;
        }
        catch (Exception exception)
        {
            logger.LogDataLoadFailed(nameof(UnM49Data), fileName, exception);
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
            .SerializeAsync(fileStream, this, LanguageJsonContext.Default.UnM49Data)
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
        await WriteLineAsync("/// Provides access to UN M.49 region containment data.");
        await WriteLineAsync("/// </summary>");
        await WriteLineAsync(
            $"[System.CodeDom.Compiler.GeneratedCode(\"{typeof(UnM49Data).FullName}\", \"1.0\")]"
        );
        await WriteLineAsync("public sealed partial class UnM49Data");
        await WriteLineAsync("{");
        await WriteLineAsync("    /// <summary>");
        await WriteLineAsync(
            "    /// Creates an instance loaded from the embedded UN M.49 dataset."
        );
        await WriteLineAsync("    /// </summary>");
        await WriteLineAsync(
            "    /// <returns>The populated <see cref=\"UnM49Data\"/> instance.</returns>"
        );
        await WriteLineAsync("    public static UnM49Data Create() =>");
        await WriteLineAsync("        new()");
        await WriteLineAsync("        {");
        await WriteLineAsync("            RecordList =");
        await WriteLineAsync("            [");

        foreach (UnM49Record record in RecordList)
        {
            await WriteLineAsync("                new()");
            await WriteLineAsync("                {");
            await WriteLineAsync(
                $"                    Code = {LanguageSchema.GetCodeGenString(record.Code)},"
            );
            await WriteLineAsync(
                $"                    Contains = {LanguageSchema.GetCodeGenString(record.Contains)},"
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
    /// Finds a UN M.49 containment record by region code.
    /// </summary>
    /// <param name="code">The region code to search for (e.g. "419" or "013").</param>
    /// <returns>The first matching <see cref="UnM49Record"/>, or null when no match is found.</returns>
    public UnM49Record? Find(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            Log.LogFindRecordNotFound(nameof(UnM49Data), code, false);
            return null;
        }

        // Find the matching containment group
        UnM49Record? record = RecordList.FirstOrDefault(item =>
            !string.IsNullOrEmpty(item.Code)
            && item.Code.Equals(code, StringComparison.OrdinalIgnoreCase)
        );
        if (record != null)
        {
            Log.LogFindRecordFound(nameof(UnM49Data), code, false);
            return record;
        }

        // Not found
        Log.LogFindRecordNotFound(nameof(UnM49Data), code, false);
        return null;
    }

    /// <summary>
    /// Gets the transitive set of UN M.49 group codes that contain the specified code.
    /// </summary>
    /// <remarks>
    /// For a country code the result is its chain of containing regions, e.g. "MX" yields
    /// "013" (Central America), "419" (Latin America and the Caribbean), "019" (Americas), and
    /// "001" (World). The codes are returned nearest containing group first.
    /// </remarks>
    /// <param name="code">The region or country code to resolve.</param>
    /// <returns>The ancestor group codes, or an empty list when the code is unknown.</returns>
    public IReadOnlyList<string> GetAncestors(string code) =>
        string.IsNullOrEmpty(code) ? []
        : _ancestorIndex.Value.TryGetValue(code, out ImmutableArray<string> ancestors) ? ancestors
        : [];

    /// <summary>
    /// Determines whether a region or country code is contained within a UN M.49 group.
    /// </summary>
    /// <param name="groupCode">The containing group code (e.g. "419").</param>
    /// <param name="code">The region or country code to test (e.g. "MX").</param>
    /// <returns>true when <paramref name="code"/> is transitively contained in <paramref name="groupCode"/>; otherwise, false.</returns>
    public bool Contains(string groupCode, string code) =>
        !string.IsNullOrEmpty(groupCode)
        && !string.IsNullOrEmpty(code)
        && GetAncestors(code).Contains(groupCode, StringComparer.OrdinalIgnoreCase);

    private Dictionary<string, ImmutableArray<string>> BuildAncestorIndex()
    {
        // Map each child code to its direct parent group codes
        Dictionary<string, List<string>> parents = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> allCodes = new(StringComparer.OrdinalIgnoreCase);
        foreach (UnM49Record record in RecordList)
        {
            _ = allCodes.Add(record.Code);
            foreach (string child in record.Contains)
            {
                _ = allCodes.Add(child);
                if (!parents.TryGetValue(child, out List<string>? list))
                {
                    list = [];
                    parents[child] = list;
                }
                if (!list.Contains(record.Code, StringComparer.OrdinalIgnoreCase))
                {
                    list.Add(record.Code);
                }
            }
        }

        // Walk up the parent map to collect transitive ancestors, guarding against cycles
        Dictionary<string, ImmutableArray<string>> index = new(StringComparer.OrdinalIgnoreCase);
        foreach (string code in allCodes)
        {
            List<string> ordered = [];
            HashSet<string> visited = new(StringComparer.OrdinalIgnoreCase) { code };
            Queue<string> queue = new();
            queue.Enqueue(code);
            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                if (!parents.TryGetValue(current, out List<string>? currentParents))
                {
                    continue;
                }
                foreach (string parent in currentParents)
                {
                    if (visited.Add(parent))
                    {
                        ordered.Add(parent);
                        queue.Enqueue(parent);
                    }
                }
            }
            index[code] = [.. ordered];
        }
        return index;
    }
}

/// <summary>
/// Represents a UN M.49 region containment record.
/// </summary>
public sealed record UnM49Record
{
    /// <summary>
    /// Gets the region code (UN M.49 numeric code, e.g. "419" or "013").
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// Gets the codes directly contained by this region (region or ISO 3166-1 country codes).
    /// </summary>
    public ImmutableArray<string> Contains { get; init; } = [];
}
