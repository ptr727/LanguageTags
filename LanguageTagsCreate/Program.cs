using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace ptr727.LanguageTags.Create;

public static class Program
{
    private const string LanguageDataDirectory = "LanguageData";
    private const string LanguageTagsDirectory = "LanguageTags";

    private static HttpClient s_httpClient;

    private static async Task DownloadFileAsync(Uri uri, string fileName)
    {
        Log.Information("Downloading \"{Uri}\" to \"{FileName}\" ...", uri.ToString(), fileName);
        Stream httpStream = await GetHttpClient().GetStreamAsync(uri);
        await using FileStream fileStream = File.Create(fileName);
        await httpStream.CopyToAsync(fileStream);
    }

    private static HttpClient GetHttpClient()
    {
        if (s_httpClient != null)
        {
            return s_httpClient;
        }
        s_httpClient = new() { Timeout = TimeSpan.FromSeconds(120) };
        s_httpClient.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue(
                Assembly.GetExecutingAssembly().GetName().Name,
                Assembly.GetExecutingAssembly().GetName().Version.ToString()
            )
        );
        return s_httpClient;
    }

    public static async Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(
                theme: AnsiConsoleTheme.Code,
                formatProvider: CultureInfo.InvariantCulture
            )
            .CreateLogger();

        // args[0] : Root directory, defaults to entry assembly directory
        string rootDirectory;
        if (args.Length > 0)
        {
            if (!Directory.Exists(args[0]))
            {
                Log.Error("Directory does not exist: \"{Directory}\"", args[0]);
                return 1;
            }
            rootDirectory = Path.GetFullPath(args[0]);
        }
        else
        {
            Assembly entryAssembly = Assembly.GetEntryAssembly();
            string assemblyDirectory = Path.GetDirectoryName(entryAssembly.Location);
            rootDirectory = Path.GetFullPath(assemblyDirectory);
        }

        // Code directory
        string codeDirectory = Path.Combine(rootDirectory, LanguageTagsDirectory);
        if (!Directory.Exists(codeDirectory))
        {
            Log.Error("Directory does not exist: \"{Directory}\"", codeDirectory);
            return 1;
        }

        // Data directory
        string dataDirectory = Path.Combine(rootDirectory, LanguageDataDirectory);
        Log.Information("Root directory: {RootDirectory}", rootDirectory);
        if (!Directory.Exists(dataDirectory))
        {
            Log.Information("Creating data directory: {DataDirectory}", dataDirectory);
            _ = Directory.CreateDirectory(dataDirectory);
        }
        Log.Information("Data directory: {DataDirectory}", dataDirectory);

        // Download all the data files
        Log.Information("Downloading all language tag data files ...");
        Log.Information("Downloading ISO 639-2 data ...");
        await DownloadFileAsync(
            new Uri(Iso6392Data.DataUri),
            Path.Combine(dataDirectory, Iso6392Data.DataFileName)
        );
        Log.Information("Downloading ISO 639-3 data ...");
        await DownloadFileAsync(
            new Uri(Iso6393Data.DataUri),
            Path.Combine(dataDirectory, Iso6393Data.DataFileName)
        );
        Log.Information("Downloading RFC 5646 data ...");
        await DownloadFileAsync(
            new Uri(Rfc5646Data.DataUri),
            Path.Combine(dataDirectory, Rfc5646Data.DataFileName)
        );
        Log.Information("Language tag data files downloaded successfully.");

        // Convert data files to JSON
        Log.Information("Converting data files to JSON ...");
        Log.Information("Converting ISO 639-2 data to JSON ...");
        Iso6392Data iso6392 = Iso6392Data.LoadData(
            Path.Combine(dataDirectory, Iso6392Data.DataFileName)
        );
        Iso6392Data.SaveJson(
            Path.Combine(dataDirectory, Iso6392Data.DataFileName + ".json"),
            iso6392
        );
        Log.Information("Converting ISO 639-3 data to JSON ...");
        Iso6393Data iso6393 = Iso6393Data.LoadData(
            Path.Combine(dataDirectory, Iso6393Data.DataFileName)
        );
        Iso6393Data.SaveJson(
            Path.Combine(dataDirectory, Iso6393Data.DataFileName + ".json"),
            iso6393
        );
        Log.Information("Converting RFC 5646 data to JSON ...");
        Rfc5646Data rfc5646 = Rfc5646Data.LoadData(
            Path.Combine(dataDirectory, Rfc5646Data.DataFileName)
        );
        Rfc5646Data.SaveJson(
            Path.Combine(dataDirectory, Rfc5646Data.DataFileName + ".json"),
            rfc5646
        );
        Log.Information("Data files converted to JSON successfully.");

        // Generate code files
        Log.Information("Generating code files ...");
        Log.Information("Generating ISO 639-2 code ...");
        Iso6392Data.GenCode(Path.Combine(codeDirectory, nameof(Iso6392Data) + "Gen.cs"), iso6392);
        Log.Information("Generating ISO 639-3 code ...");
        Iso6393Data.GenCode(Path.Combine(codeDirectory, nameof(Iso6393Data) + "Gen.cs"), iso6393);
        Log.Information("Generating RFC 5646 code ...");
        Rfc5646Data.GenCode(Path.Combine(codeDirectory, nameof(Rfc5646Data) + "Gen.cs"), rfc5646);

        return 0;
    }
}
