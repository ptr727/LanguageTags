namespace ptr727.LanguageTags.Create;

internal sealed class CreateTagData(
    string dataDirectory,
    string codeDirectory,
    CancellationToken cancellationToken
)
{
    private Iso6392Data? _iso6392;
    private string? _iso6392DataFile;
    private string? _iso6392JsonFile;
    private string? _iso6392CodeFile;
    private Iso6393Data? _iso6393;
    private string? _iso6393DataFile;
    private string? _iso6393JsonFile;
    private string? _iso6393CodeFile;
    private Rfc5646Data? _rfc5646;
    private string? _rfc5646DataFile;
    private string? _rfc5646JsonFile;
    private string? _rfc5646CodeFile;

    internal async Task DownloadDataAsync()
    {
        // Download all the data files
        Log.Information("Downloading all language tag data files ...");

        Log.Information("Downloading ISO 639-2 data ...");
        _iso6392DataFile = Path.Combine(dataDirectory, Iso6392Data.DataFileName);
        await DownloadFileAsync(new Uri(Iso6392Data.DataUri), _iso6392DataFile)
            .ConfigureAwait(false);

        Log.Information("Downloading ISO 639-3 data ...");
        _iso6393DataFile = Path.Combine(dataDirectory, Iso6393Data.DataFileName);
        await DownloadFileAsync(new Uri(Iso6393Data.DataUri), _iso6393DataFile)
            .ConfigureAwait(false);

        Log.Information("Downloading RFC 5646 data ...");
        _rfc5646DataFile = Path.Combine(dataDirectory, Rfc5646Data.DataFileName);
        await DownloadFileAsync(new Uri(Rfc5646Data.DataUri), _rfc5646DataFile)
            .ConfigureAwait(false);

        Log.Information("Language tag data files downloaded successfully.");
    }

    internal async Task CreateJsonDataAsync()
    {
        ArgumentNullException.ThrowIfNull(_iso6392DataFile);
        ArgumentNullException.ThrowIfNull(_iso6393DataFile);
        ArgumentNullException.ThrowIfNull(_rfc5646DataFile);

        // Convert data files to JSON
        Log.Information("Converting data files to JSON ...");

        Log.Information("Converting ISO 639-2 data to JSON ...");
        _iso6392 = await Iso6392Data.FromDataAsync(_iso6392DataFile).ConfigureAwait(false);
        _iso6392JsonFile = Path.Combine(dataDirectory, Iso6392Data.DataFileName + ".json");
        Log.Information("Writing ISO 639-2 data to {JsonPath}", _iso6392JsonFile);
        await _iso6392.SaveJsonAsync(_iso6392JsonFile).ConfigureAwait(false);

        Log.Information("Converting ISO 639-3 data to JSON ...");
        _iso6393 = await Iso6393Data.FromDataAsync(_iso6393DataFile).ConfigureAwait(false);
        _iso6393JsonFile = Path.Combine(dataDirectory, Iso6393Data.DataFileName + ".json");
        Log.Information("Writing ISO 639-3 data to {JsonPath}", _iso6393JsonFile);
        await _iso6393.SaveJsonAsync(_iso6393JsonFile).ConfigureAwait(false);

        Log.Information("Converting RFC 5646 data to JSON ...");
        _rfc5646 = await Rfc5646Data.FromDataAsync(_rfc5646DataFile).ConfigureAwait(false);
        _rfc5646JsonFile = Path.Combine(dataDirectory, Rfc5646Data.DataFileName + ".json");
        Log.Information("Writing RFC 5646 data to {JsonPath}", _rfc5646JsonFile);
        await _rfc5646.SaveJsonAsync(_rfc5646JsonFile).ConfigureAwait(false);

        Log.Information("Data files converted to JSON successfully.");
    }

    internal async Task GenerateCodeAsync()
    {
        ArgumentNullException.ThrowIfNull(_iso6392);
        ArgumentNullException.ThrowIfNull(_iso6393);
        ArgumentNullException.ThrowIfNull(_rfc5646);

        // Generate code files
        Log.Information("Generating code files ...");

        Log.Information("Generating ISO 639-2 code ...");
        _iso6392CodeFile = Path.Combine(codeDirectory, nameof(Iso6392Data) + "Gen.cs");
        Log.Information("Writing ISO 639-2 code to {CodePath}", _iso6392CodeFile);
        await _iso6392.SaveCodeAsync(_iso6392CodeFile).ConfigureAwait(false);

        Log.Information("Generating ISO 639-3 code ...");
        _iso6393CodeFile = Path.Combine(codeDirectory, nameof(Iso6393Data) + "Gen.cs");
        Log.Information("Writing ISO 639-3 code to {CodePath}", _iso6393CodeFile);
        await _iso6393.SaveCodeAsync(_iso6393CodeFile).ConfigureAwait(false);

        Log.Information("Generating RFC 5646 code ...");
        _rfc5646CodeFile = Path.Combine(codeDirectory, nameof(Rfc5646Data) + "Gen.cs");
        Log.Information("Writing RFC 5646 code to {CodePath}", _rfc5646CodeFile);
        await _rfc5646.SaveCodeAsync(_rfc5646CodeFile).ConfigureAwait(false);

        Log.Information("Code files generated successfully.");
    }

    private async Task DownloadFileAsync(Uri uri, string fileName)
    {
        ArgumentNullException.ThrowIfNull(uri);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        Log.Information("Downloading \"{Uri}\" to \"{FileName}\" ...", uri.ToString(), fileName);

        using Stream httpStream = await HttpClientFactory
            .GetHttpClient()
            .GetStreamAsync(uri, cancellationToken)
            .ConfigureAwait(false);

        using FileStream fileStream = new(
            fileName,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            8192,
            FileOptions.Asynchronous | FileOptions.SequentialScan
        );

        await httpStream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
    }
}
