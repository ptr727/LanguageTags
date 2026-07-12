namespace ptr727.LanguageTags.Create;

internal sealed class Program(
    CommandLine.Options commandLineOptions,
    CancellationToken cancellationToken
)
{
    private const string DataDirectory = "LanguageData";
    private const string CodeDirectory = "LanguageTags";

    internal static async Task<int> Main(string[] args)
    {
        try
        {
            // Parse commandline
            CommandLine commandLine = new(args);
            commandLine.Result.InvocationConfiguration.EnableDefaultExceptionHandler = false;
            commandLine.Result.InvocationConfiguration.ProcessTerminationTimeout = null;

            // Bypass startup for errors or help and version commands
            if (CommandLine.BypassStartup(commandLine.Result))
            {
                return await commandLine.Result.InvokeAsync().ConfigureAwait(false);
            }

            // Create logger
            Log.Logger = LoggerFactory.Create(
                commandLine.CreateOptions(commandLine.Result).LogOptions
            );
            ILoggerFactory loggerFactory = LoggerFactory.CreateLoggerFactory();
            LogOptions.SetFactory(loggerFactory);
            Utilities.LogOptions.SetFactory(loggerFactory);

            // Invoke command
            Log.Logger.LogOverrideContext().Information("Starting: {Args}", args);
            return await commandLine.Result.InvokeAsync().ConfigureAwait(false);
        }
        catch (Exception ex) when (Log.Logger.LogAndHandle(ex))
        {
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync().ConfigureAwait(false);
        }
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

            // Obtain the source data: download fresh from upstream, or, in offline mode, use the data
            // already committed under LanguageData/ so codegen can be re-exercised with no network.
            CreateTagData createTagData = new(dataDirectory, codeDirectory, cancellationToken);
            if (commandLineOptions.SkipDownload)
            {
                createTagData.UseExistingData();
            }
            else
            {
                await createTagData.DownloadDataAsync().ConfigureAwait(false);
            }

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
