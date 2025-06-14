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

public class Program
{
    private const string LanguageDataDirectory = "LanguageData";
    private const string LanguageTagsDirectory = "LanguageTags";

    private static HttpClient s_httpClient;

    private static async Task DownloadFileAsync(Uri uri, string fileName)
    {
        Log.Information("Downloading \"{Uri}\" to \"{FileName}\" ...", uri.ToString(), fileName);
        Stream httpStream = await GetHttpClient().GetStreamAsync(uri);
        using FileStream fileStream = File.OpenWrite(fileName);
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
        Log.Information("Downloading Iso6392 data ...");
        await DownloadFileAsync(
            new Uri(Iso6392.DataUri),
            Path.Combine(dataDirectory, Iso6392.DataFileName)
        );
        Log.Information("Downloading Iso6393 data ...");
        await DownloadFileAsync(
            new Uri(Iso6393.DataUri),
            Path.Combine(dataDirectory, Iso6393.DataFileName)
        );
        Log.Information("Downloading Rfc5646 data ...");
        await DownloadFileAsync(
            new Uri(Rfc5646.DataUri),
            Path.Combine(dataDirectory, Rfc5646.DataFileName)
        );
        Log.Information("Language tag data files downloaded successfully.");

        // Convert data files to JSON
        Log.Information("Converting data files to JSON ...");
        Log.Information("Converting Iso6392 data to JSON ...");
        Iso6392 iso6392 = Iso6392.LoadData(Path.Combine(dataDirectory, Iso6392.DataFileName));
        Iso6392.SaveJson(Path.Combine(dataDirectory, Iso6392.DataFileName + ".json"), iso6392);
        Log.Information("Converting Iso6393 data to JSON ...");
        Iso6393 iso6393 = Iso6393.LoadData(Path.Combine(dataDirectory, Iso6393.DataFileName));
        Iso6393.SaveJson(Path.Combine(dataDirectory, Iso6393.DataFileName + ".json"), iso6393);
        Log.Information("Converting Rfc5646 data to JSON ...");
        Rfc5646 rfc5646 = Rfc5646.LoadData(Path.Combine(dataDirectory, Rfc5646.DataFileName));
        Rfc5646.SaveJson(Path.Combine(dataDirectory, Rfc5646.DataFileName + ".json"), rfc5646);
        Log.Information("Data files converted to JSON successfully.");

        // Generate code files
        Log.Information("Generating code files ...");
        Log.Information("Generating Iso6392 code ...");
        Iso6392.GenCode(Path.Combine(codeDirectory, "Iso6392Gen.cs"), iso6392);
        Log.Information("Generating Iso6393 code ...");
        Iso6393.GenCode(Path.Combine(codeDirectory, "Iso6393Gen.cs"), iso6393);
        Log.Information("Generating Rfc5646 code ...");
        Rfc5646.GenCode(Path.Combine(codeDirectory, "Rfc5646Gen.cs"), rfc5646);

        return 0;
    }
}
