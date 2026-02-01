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
    /// Creates a logger for the specified type using the provided options or global configuration.
    /// </summary>
    /// <typeparam name="T">The type used to derive the logger category.</typeparam>
    /// <param name="options">The options used to configure logging. If null, uses global configuration.</param>
    /// <returns>The configured logger for the category.</returns>
    /// <remarks>
    /// <para>
    /// When options is provided, the logger is resolved using this priority order:
    /// <list type="number">
    /// <item><description>options.Logger if set</description></item>
    /// <item><description>options.LoggerFactory if set, creating a logger for the category</description></item>
    /// <item><description>Global Logger if set</description></item>
    /// <item><description>Global LoggerFactory if set, creating a logger for the category</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// When options is null, the global logger is resolved using this priority order:
    /// <list type="number">
    /// <item><description>Global Logger if set</description></item>
    /// <item><description>Global LoggerFactory if set, creating a logger for the category</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Null logger instances are treated as not configured.
    /// </para>
    /// </remarks>
    public static ILogger CreateLogger<T>(Options? options = null) =>
        CreateLogger(typeof(T).FullName ?? typeof(T).Name, options);

    /// <summary>
    /// Creates a logger for the specified category using the provided options or global configuration.
    /// </summary>
    /// <param name="categoryName">The category name for the logger.</param>
    /// <param name="options">The options used to configure logging. If null, uses global configuration.</param>
    /// <returns>The configured logger for the category.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="categoryName"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="categoryName"/> is empty or whitespace.
    /// </exception>
    /// <remarks>
    /// <para>
    /// When options is provided, the logger is resolved using this priority order:
    /// <list type="number">
    /// <item><description>options.Logger if set</description></item>
    /// <item><description>options.LoggerFactory if set, creating a logger for the category</description></item>
    /// <item><description>Global Logger if set</description></item>
    /// <item><description>Global LoggerFactory if set, creating a logger for the category</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// When options is null, the global logger is resolved using this priority order:
    /// <list type="number">
    /// <item><description>Global Logger if set</description></item>
    /// <item><description>Global LoggerFactory if set, creating a logger for the category</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Null logger instances are treated as not configured.
    /// </para>
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Style",
        "IDE0046:Convert to conditional expression",
        Justification = "Logic clarity."
    )]
    public static ILogger CreateLogger(string categoryName, Options? options = null)
    {
        ArgumentNullException.ThrowIfNull(categoryName);
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            throw new ArgumentException("Category name must not be empty.", nameof(categoryName));
        }

        if (options is not null)
        {
            if (options.Logger is not null && !ReferenceEquals(options.Logger, NullLogger.Instance))
            {
                return options.Logger;
            }

            if (
                options.LoggerFactory is not null
                && !ReferenceEquals(options.LoggerFactory, NullLoggerFactory.Instance)
            )
            {
                return options.LoggerFactory.CreateLogger(categoryName);
            }
        }

        if (!ReferenceEquals(Logger, NullLogger.Instance))
        {
            return Logger;
        }

        if (!ReferenceEquals(LoggerFactory, NullLoggerFactory.Instance))
        {
            return LoggerFactory.CreateLogger(categoryName);
        }

        return NullLogger.Instance;
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
