using System.Threading;

namespace ptr727.LanguageTags;

/// <summary>
/// Provides global logging configuration for the library.
/// </summary>
public static class LogOptions
{
    private static ILoggerFactory s_loggerFactory = NullLoggerFactory.Instance;
    private static ILogger s_logger = NullLogger.Instance;

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
    /// Gets or sets the global fallback logger used when no factory is configured.
    /// </summary>
    /// <remarks>
    /// Changes to this property after loggers have been created will not affect existing cached loggers.
    /// </remarks>
    public static ILogger Logger
    {
        get => Volatile.Read(ref s_logger);
        set => _ = Interlocked.Exchange(ref s_logger, value ?? NullLogger.Instance);
    }

    /// <summary>
    /// Creates a logger for the specified type using the current factory or fallback logger.
    /// </summary>
    /// <typeparam name="T">The type used to derive the logger category.</typeparam>
    /// <returns>The configured logger for the category.</returns>
    public static ILogger CreateLogger<T>() => CreateLogger(typeof(T).FullName ?? typeof(T).Name);

    /// <summary>
    /// Creates a logger for the specified type using the provided options or global configuration.
    /// </summary>
    /// <typeparam name="T">The type used to derive the logger category.</typeparam>
    /// <param name="options">The options used to configure logging.</param>
    /// <returns>The configured logger for the category.</returns>
    public static ILogger CreateLogger<T>(Options? options) =>
        CreateLogger(typeof(T).FullName ?? typeof(T).Name, options);

    /// <summary>
    /// Creates a logger for the specified category using the current factory or fallback logger.
    /// </summary>
    /// <param name="categoryName">The category name for the logger.</param>
    /// <returns>The configured logger for the category.</returns>
    public static ILogger CreateLogger(string categoryName)
    {
        ILoggerFactory loggerFactory = LoggerFactory;
        return !ReferenceEquals(loggerFactory, NullLoggerFactory.Instance)
            ? loggerFactory.CreateLogger(categoryName)
            : Logger;
    }

    /// <summary>
    /// Creates a logger for the specified category using the provided options or global configuration.
    /// </summary>
    /// <param name="categoryName">The category name for the logger.</param>
    /// <param name="options">The options used to configure logging.</param>
    /// <returns>The configured logger for the category.</returns>
    public static ILogger CreateLogger(string categoryName, Options? options) =>
        options is null ? CreateLogger(categoryName)
        : options.LoggerFactory is not null ? options.LoggerFactory.CreateLogger(categoryName)
        : options.Logger ?? CreateLogger(categoryName);

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

    /// <summary>
    /// Configures the library to use the specified global logger.
    /// </summary>
    /// <param name="logger">The logger used as the global fallback.</param>
    /// <remarks>
    /// This will only affect loggers created after this call.
    /// Existing cached loggers remain unchanged.
    /// </remarks>
    public static void SetLogger(ILogger logger) => Logger = logger;

    /// <summary>
    /// Attempts to configure the library to use the specified global logger if none is set.
    /// </summary>
    /// <param name="logger">The logger used as the global fallback.</param>
    /// <returns>
    /// <c>true</c> when the logger was set because no logger was configured; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Use this method for one-time initialization to avoid overwriting an existing logger.
    /// </remarks>
    public static bool TrySetLogger(ILogger logger)
    {
        ILogger candidate = logger ?? NullLogger.Instance;
        ILogger original = Interlocked.CompareExchange(
            ref s_logger,
            candidate,
            NullLogger.Instance
        );

        return ReferenceEquals(original, NullLogger.Instance);
    }
}
