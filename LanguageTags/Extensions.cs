using System.Runtime.CompilerServices;

namespace ptr727.ProjectTemplate.Library;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1034:Nested types should not be visible",
    Justification = "https://github.com/dotnet/sdk/issues/51681"
)]
internal static partial class LogExtensions
{
    extension(ILogger logger)
    {
        public bool LogAndPropagate(
            Exception exception,
            [CallerMemberName] string function = "unknown"
        )
        {
            LogCatchException(logger, function, exception);
            return false;
        }

        public bool LogAndHandle(
            Exception exception,
            [CallerMemberName] string function = "unknown"
        )
        {
            LogCatchException(logger, function, exception);
            return true;
        }
    }

    [LoggerMessage(Message = "Exception in {Function}", Level = LogLevel.Error)]
    internal static partial void LogCatchException(
        this ILogger logger,
        string function,
        Exception exception
    );

    [LoggerMessage(Message = "{Message}", Level = LogLevel.Information)]
    internal static partial void LogInformation(this ILogger logger, string message);
}
