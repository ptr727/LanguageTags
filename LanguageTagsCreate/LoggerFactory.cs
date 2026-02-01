using Serilog.Extensions.Logging;
using Serilog.Sinks.SystemConsole.Themes;

namespace ptr727.LanguageTags.Create;

internal static class LoggerFactory
{
    private static readonly Lazy<SerilogLoggerFactory> s_serilogLoggerFactory = new(() =>
    {
        // Use already configured Log.Logger if set, else create a new logger factory
        Serilog.ILogger logger = ReferenceEquals(Log.Logger, Serilog.Core.Logger.None)
            ? Create()
            : Log.Logger;
        bool disposeLogger = !ReferenceEquals(logger, Log.Logger);
        return new SerilogLoggerFactory(logger, dispose: disposeLogger);
    });

    internal static Serilog.ILogger Create(Options? options = null)
    {
        // Log to the console
        LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Is(options?.Level ?? LogEventLevel.Information)
            .MinimumLevel.Override(
                typeof(LogExtensions.LogOverride).FullName!,
                LogEventLevel.Verbose
            )
            .Enrich.WithThreadId()
            .Enrich.WithThreadName()
            .WriteTo.Console(
                theme: AnsiConsoleTheme.Code,
                formatProvider: CultureInfo.InvariantCulture,
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [t:{ThreadId}{ThreadName}] {Message:lj}{NewLine}{Exception}"
            );

        // Log to file
        if (!string.IsNullOrEmpty(options?.File))
        {
            if (options.FileClear && File.Exists(options.File))
            {
                File.Delete(options.File);
            }
            _ = loggerConfiguration.WriteTo.File(
                options.File,
                formatProvider: CultureInfo.InvariantCulture,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] [t:{ThreadId}{ThreadName}] {Message:lj}{NewLine}{Exception}"
            );
        }

        // Create logger
        return loggerConfiguration.CreateLogger();
    }

    internal static ILoggerFactory CreateLoggerFactory() => s_serilogLoggerFactory.Value;

    internal static Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName) =>
        s_serilogLoggerFactory.Value.CreateLogger(categoryName);

    internal sealed class Options
    {
        internal required LogEventLevel Level { get; init; }
        internal required string File { get; init; }
        internal required bool FileClear { get; init; }
    }
}
