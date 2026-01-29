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

    internal Task CreateJsonDataAsync()
    {
        ArgumentNullException.ThrowIfNull(_iso6392DataFile, nameof(_iso6392DataFile));
        ArgumentNullException.ThrowIfNull(_iso6393DataFile, nameof(_iso6393DataFile));
        ArgumentNullException.ThrowIfNull(_rfc5646DataFile, nameof(_rfc5646DataFile));

        // TODO: Convert load and save to async

        // Convert data files to JSON
        Log.Information("Converting data files to JSON ...");

        Log.Information("Converting ISO 639-2 data to JSON ...");
        _iso6392 = Iso6392Data.LoadData(_iso6392DataFile);
        _iso6392JsonFile = Path.Combine(dataDirectory, Iso6392Data.DataFileName + ".json");
        Log.Information("Writing ISO 639-2 data to {JsonPath}", _iso6392JsonFile);
        Iso6392Data.SaveJson(_iso6392JsonFile, _iso6392);

        Log.Information("Converting ISO 639-3 data to JSON ...");
        _iso6393 = Iso6393Data.LoadData(_iso6393DataFile);
        _iso6393JsonFile = Path.Combine(dataDirectory, Iso6393Data.DataFileName + ".json");
        Log.Information("Writing ISO 639-3 data to {JsonPath}", _iso6393JsonFile);
        Iso6393Data.SaveJson(_iso6393JsonFile, _iso6393);

        Log.Information("Converting RFC 5646 data to JSON ...");
        _rfc5646 = Rfc5646Data.LoadData(_rfc5646DataFile);
        _rfc5646JsonFile = Path.Combine(dataDirectory, Rfc5646Data.DataFileName + ".json");
        Log.Information("Writing RFC 5646 data to {JsonPath}", _rfc5646JsonFile);
        Rfc5646Data.SaveJson(_rfc5646JsonFile, _rfc5646);

        Log.Information("Data files converted to JSON successfully.");

        return Task.CompletedTask;
    }

    internal Task GenerateCodeAsync()
    {
        ArgumentNullException.ThrowIfNull(_iso6392, nameof(_iso6392));
        ArgumentNullException.ThrowIfNull(_iso6393, nameof(_iso6393));
        ArgumentNullException.ThrowIfNull(_rfc5646, nameof(_rfc5646));

        // TODO: Convert to async

        // Generate code files
        Log.Information("Generating code files ...");

        Log.Information("Generating ISO 639-2 code ...");
        _iso6392CodeFile = Path.Combine(codeDirectory, nameof(Iso6392Data) + "Gen.cs");
        Log.Information("Writing ISO 639-2 code to {CodePath}", _iso6392CodeFile);
        Iso6392Data.GenCode(_iso6392CodeFile, _iso6392);

        Log.Information("Generating ISO 639-3 code ...");
        _iso6393CodeFile = Path.Combine(codeDirectory, nameof(Iso6393Data) + "Gen.cs");
        Log.Information("Writing ISO 639-3 code to {CodePath}", _iso6393CodeFile);
        Iso6393Data.GenCode(_iso6393CodeFile, _iso6393);

        Log.Information("Generating RFC 5646 code ...");
        _rfc5646CodeFile = Path.Combine(codeDirectory, nameof(Rfc5646Data) + "Gen.cs");
        Log.Information("Writing RFC 5646 code to {CodePath}", _rfc5646CodeFile);
        Rfc5646Data.GenCode(_rfc5646CodeFile, _rfc5646);

        Log.Information("Code files generated successfully.");
        return Task.CompletedTask;
    }

    private async Task DownloadFileAsync(Uri uri, string fileName)
    {
        ArgumentNullException.ThrowIfNull(uri, nameof(uri));
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName, nameof(fileName));

        Log.Information("Downloading \"{Uri}\" to \"{FileName}\" ...", uri.ToString(), fileName);
        Stream httpStream = await HttpClientFactory
            .GetHttpClient()
            .GetStreamAsync(uri, cancellationToken)
            .ConfigureAwait(false);
        await using (httpStream.ConfigureAwait(false))
        {
            FileStream fileStream = File.Create(fileName);
            await using (fileStream.ConfigureAwait(false))
            {
                await httpStream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
