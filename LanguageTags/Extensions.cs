using System.Runtime.CompilerServices;

namespace ptr727.LanguageTags;

internal static partial class LogExtensions
{
    extension(ILogger logger)
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
        this ILogger logger,
        string function,
        Exception exception
    );

    [LoggerMessage(Message = "{Message}", Level = LogLevel.Information)]
    internal static partial void LogInformation(this ILogger logger, string message);

    [LoggerMessage(
        Message = "Failed to parse language tag {LanguageTag}: {Reason}",
        Level = LogLevel.Debug
    )]
    internal static partial void LogParseFailure(
        this ILogger logger,
        string? languageTag,
        string reason
    );

    [LoggerMessage(
        Message = "Normalized language tag {OriginalTag} to {NormalizedTag}",
        Level = LogLevel.Debug
    )]
    internal static partial void LogNormalizedTag(
        this ILogger logger,
        string originalTag,
        string normalizedTag
    );

    [LoggerMessage(
        Message = "Language tag conversion returned undetermined for {LanguageTag} in {Operation}",
        Level = LogLevel.Debug
    )]
    internal static partial void LogUndeterminedFallback(
        this ILogger logger,
        string languageTag,
        string operation
    );

    [LoggerMessage(
        Message = "Loaded {RecordCount} records for {DataKind} from {FileName}.",
        Level = LogLevel.Information
    )]
    internal static partial void LogDataLoaded(
        this ILogger logger,
        string dataKind,
        string fileName,
        int recordCount
    );

    [LoggerMessage(
        Message = "No data was loaded for {DataKind} from {FileName}.",
        Level = LogLevel.Warning
    )]
    internal static partial void LogDataLoadEmpty(
        this ILogger logger,
        string dataKind,
        string fileName
    );

    [LoggerMessage(Message = "Failed to load {DataKind} from {FileName}.", Level = LogLevel.Error)]
    internal static partial void LogDataLoadFailed(
        this ILogger logger,
        string dataKind,
        string fileName,
        Exception exception
    );

    [LoggerMessage(
        Message = "Found {DataKind} record for {LanguageTag} (include description: {IncludeDescription}).",
        Level = LogLevel.Debug
    )]
    internal static partial void LogFindRecordFound(
        this ILogger logger,
        string dataKind,
        string? languageTag,
        bool includeDescription
    );

    [LoggerMessage(
        Message = "No {DataKind} record found for {LanguageTag} (include description: {IncludeDescription}).",
        Level = LogLevel.Debug
    )]
    internal static partial void LogFindRecordNotFound(
        this ILogger logger,
        string dataKind,
        string? languageTag,
        bool includeDescription
    );

    [LoggerMessage(
        Message = "Language tag {LanguageTag} did not match prefix {Prefix}.",
        Level = LogLevel.Debug
    )]
    internal static partial void LogPrefixMatchFailed(
        this ILogger logger,
        string prefix,
        string languageTag
    );
}
