using System.Runtime.CompilerServices;

namespace ptr727.LanguageTags.Create;

internal static partial class LogExtensions
{
    extension(Serilog.ILogger logger)
    {
        internal bool LogAndPropagate(
            Exception exception,
            [CallerMemberName] string function = "unknown"
        )
        {
            logger.Error(exception, "{Function}", function);
            return false;
        }

        internal bool LogAndHandle(
            Exception exception,
            [CallerMemberName] string function = "unknown"
        )
        {
            logger.Error(exception, "{Function}", function);
            return true;
        }

        internal Serilog.ILogger LogOverrideContext() => logger.ForContext<LogOverride>();
    }

    extension(Microsoft.Extensions.Logging.ILogger logger)
    {
        internal bool LogAndPropagate(
            Exception exception,
            [CallerMemberName] string function = "unknown"
        )
        {
            logger.LogCatchException(function, exception);
            return false;
        }

        internal bool LogAndHandle(
            Exception exception,
            [CallerMemberName] string function = "unknown"
        )
        {
            logger.LogCatchException(function, exception);
            return true;
        }
    }

    [LoggerMessage(Message = "Exception in {Function}", Level = LogLevel.Error)]
    internal static partial void LogCatchException(
        this Microsoft.Extensions.Logging.ILogger logger,
        string function,
        Exception exception
    );

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1812:Avoid uninstantiated internal classes",
        Justification = "Used as a type marker for Serilog context filtering"
    )]
    internal sealed class LogOverride;
}
