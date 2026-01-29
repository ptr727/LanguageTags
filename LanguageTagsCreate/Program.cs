namespace ptr727.LanguageTags.Create;

internal sealed class Program(
    CommandLine.Options commandLineOptions,
    CancellationToken cancellationToken
)
{
    private const string DataDirectory = "LanguageData";
    private const string CodeDirectory = "LanguageTags";

    internal CommandLine.Options GetCommandLineOptions() => commandLineOptions;

    internal CancellationToken GetCancellationToken() => cancellationToken;

    internal static async Task<int> Main(string[] args)
    {
        // Parse commandline
        CommandLine commandLine = new(args);

        // Bypass startup for errors or help and version commands
        if (CommandLine.BypassStartup(commandLine.Result))
        {
            return await commandLine.Result.InvokeAsync().ConfigureAwait(false);
        }

        // Create logger
        _ = LoggerFactory.Create(commandLine.CreateOptions(commandLine.Result).LogOptions);
        Log.Logger.LogOverrideContext().Information("Starting: {Args}", args);

        // Initialize library with logger
        //TemplateLibrary templateLibrary = new(
        //    new Options() { Logger = LoggerFactory.CreateLogger(typeof(TemplateLibrary).FullName!) }
        //);

        // Invoke command
        return await commandLine.Result.InvokeAsync().ConfigureAwait(false);
    }

    internal async Task<int> ExecuteAsync()
    {
        try
        {
            // Data and code directories
            string solutionDirectory = Path.GetFullPath(commandLineOptions.CodePath.FullName);
            string dataDirectory = Path.Combine(solutionDirectory, DataDirectory);
            string codeDirectory = Path.Combine(solutionDirectory, CodeDirectory);
            if (!Directory.Exists(dataDirectory))
            {
                Log.Error("Data directory does not exist: {DataDirectory}", dataDirectory);
                return 1;
            }
            if (!Directory.Exists(codeDirectory))
            {
                Log.Error("Code directory does not exist: {CodeDirectory}", codeDirectory);
                return 1;
            }

            // Download data files
            CreateTagData createTagData = new(dataDirectory, codeDirectory, cancellationToken);
            await createTagData.DownloadDataAsync().ConfigureAwait(false);

            // Convert data files to JSON
            await createTagData.CreateJsonDataAsync().ConfigureAwait(false);

            // Generate code files
            await createTagData.GenerateCodeAsync().ConfigureAwait(false);

            return 0;
        }
        catch (Exception ex) when (Log.Logger.LogAndHandle(ex))
        {
            return 1;
        }
    }
}
