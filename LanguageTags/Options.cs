namespace ptr727.LanguageTags;

/// <summary>
/// Options used to configure the library.
/// </summary>
public sealed class Options
{
    /// <summary>
    /// Gets the logger used by the library.
    /// </summary>
    public ILogger Logger { get; init; } = NullLogger.Instance;
}
