using System.Threading;

namespace ptr727.LanguageTags;

/// <summary>
/// Provides global logging configuration for the library.
/// </summary>
/// <remarks>
/// <para>
/// This class manages static logger configuration used by all library classes.
/// When a logger is requested, it is resolved using the following priority order:
/// </para>
/// <list type="number">
/// <item>
/// <description><see cref="LoggerFactory"/> - The global logger factory, when set to a non-<see cref="NullLoggerFactory"/> instance. Creates a logger for the requested category.</description>
/// </item>
/// <item>
/// <description><see cref="NullLogger.Instance"/> - The default fallback when no logger factory is configured.</description>
/// </item>
/// </list>
/// <para>
/// Note that loggers are created and cached at the time of use by each class instance. Changes to <see cref="LoggerFactory"/>
/// after a logger has been created will not affect existing cached loggers—only new logger requests will use the updated configuration.
/// </para>
/// </remarks>
public static class LogOptions
{
    private static ILoggerFactory s_loggerFactory = NullLoggerFactory.Instance;

    /// <summary>
    /// Gets or sets the logger factory used to create category loggers.
    /// </summary>
    /// <remarks>
    /// Changes to this property after loggers have been created will not affect existing cached loggers.
    /// </remarks>
    public static ILoggerFactory LoggerFactory
    {
        get => Volatile.Read(ref s_loggerFactory);
        set => _ = Interlocked.Exchange(ref s_loggerFactory, value ?? NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Configures the library to use the specified logger factory.
    /// </summary>
    /// <param name="loggerFactory">The factory to use for new loggers.</param>
    /// <remarks>
    /// This will only affect loggers created after this call.
    /// Existing cached loggers remain unchanged.
    /// </remarks>
    public static void SetFactory(ILoggerFactory loggerFactory) => LoggerFactory = loggerFactory;

    /// <summary>
    /// Attempts to configure the library to use the specified logger factory if none is set.
    /// </summary>
    /// <param name="loggerFactory">The factory to use for new loggers.</param>
    /// <returns>
    /// <c>true</c> when the factory was set because no factory was configured; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Use this method for one-time initialization to avoid overwriting an existing factory.
    /// </remarks>
    public static bool TrySetFactory(ILoggerFactory loggerFactory)
    {
        ILoggerFactory candidate = loggerFactory ?? NullLoggerFactory.Instance;
        ILoggerFactory original = Interlocked.CompareExchange(
            ref s_loggerFactory,
            candidate,
            NullLoggerFactory.Instance
        );

        return ReferenceEquals(original, NullLoggerFactory.Instance);
    }

    internal static ILogger CreateLogger<T>() => CreateLogger(typeof(T).FullName ?? typeof(T).Name);

    internal static ILogger CreateLogger(string categoryName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryName);

        // LoggerFactory -> NullLogger
        return !ReferenceEquals(LoggerFactory, NullLoggerFactory.Instance)
            ? LoggerFactory.CreateLogger(categoryName)
            : NullLogger.Instance;
    }
}
