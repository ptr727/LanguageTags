namespace ptr727.LanguageTags;

/// <summary>
/// Options used to configure the library.
/// </summary>
public sealed class Options
{
    /// <summary>
    /// Gets the logger factory used to create per-instance loggers.
    /// </summary>
    public ILoggerFactory? LoggerFactory { get; init; }

    /// <summary>
    /// Gets the logger used by the library.
    /// </summary>
    public ILogger? Logger { get; init; }
}
