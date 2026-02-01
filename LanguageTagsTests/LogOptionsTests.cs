using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ptr727.LanguageTags.Tests;

[Collection("Sequential Test Collection")]
public sealed class LogOptionsTests : SingleInstanceFixture
{
    [Fact]
    public void CreateLogger_UsesLogger_WhenBothSet()
    {
        ILoggerFactory originalFactory = LogOptions.LoggerFactory;
        ILogger originalLogger = LogOptions.Logger;
        using TestLoggerFactory testFactory = new();
        TestLogger testLogger = new();

        try
        {
            LogOptions.LoggerFactory = testFactory;
            LogOptions.Logger = testLogger;

            ILogger logger = LogOptions.CreateLogger("category");

            // Logger should take precedence over LoggerFactory
            _ = logger.Should().BeSameAs(testLogger);
            // Factory should not be called
            _ = testFactory.LastCategory.Should().BeNull();
        }
        finally
        {
            LogOptions.LoggerFactory = originalFactory;
            LogOptions.Logger = originalLogger;
        }
    }

    [Fact]
    public void CreateLogger_UsesLogger_WhenFactoryDefault()
    {
        ILoggerFactory originalFactory = LogOptions.LoggerFactory;
        ILogger originalLogger = LogOptions.Logger;
        TestLogger testLogger = new();

        try
        {
            LogOptions.LoggerFactory = NullLoggerFactory.Instance;
            LogOptions.Logger = testLogger;

            ILogger logger = LogOptions.CreateLogger("category");

            _ = logger.Should().BeSameAs(testLogger);
        }
        finally
        {
            LogOptions.LoggerFactory = originalFactory;
            LogOptions.Logger = originalLogger;
        }
    }

    [Fact]
    public void CreateLogger_WithOptions_UsesOptionsLoggerFirst()
    {
        ILoggerFactory originalFactory = LogOptions.LoggerFactory;
        ILogger originalLogger = LogOptions.Logger;
        using TestLoggerFactory testFactory = new();
        TestLogger testLogger = new();
        Options options = new() { LoggerFactory = testFactory, Logger = testLogger };

        try
        {
            LogOptions.LoggerFactory = NullLoggerFactory.Instance;
            LogOptions.Logger = NullLogger.Instance;

            ILogger logger = LogOptions.CreateLogger("category", options);

            // Logger should take precedence over LoggerFactory
            _ = logger.Should().BeSameAs(testLogger);
            // Factory should not be called
            _ = testFactory.LastCategory.Should().BeNull();
        }
        finally
        {
            LogOptions.LoggerFactory = originalFactory;
            LogOptions.Logger = originalLogger;
        }
    }

    [Fact]
    public void CreateLogger_WithOptions_UsesOptionsLoggerWhenNoFactory()
    {
        ILoggerFactory originalFactory = LogOptions.LoggerFactory;
        ILogger originalLogger = LogOptions.Logger;
        TestLogger testLogger = new();
        Options options = new() { Logger = testLogger };

        try
        {
            using TestLoggerFactory testFactory = new();
            LogOptions.LoggerFactory = testFactory;
            LogOptions.Logger = new TestLogger();

            ILogger logger = LogOptions.CreateLogger("category", options);

            _ = logger.Should().BeSameAs(testLogger);
        }
        finally
        {
            LogOptions.LoggerFactory = originalFactory;
            LogOptions.Logger = originalLogger;
        }
    }

    [Fact]
    public void CreateLogger_WithOptions_UsesOptionsFactoryWhenLoggerNotSet()
    {
        ILoggerFactory originalFactory = LogOptions.LoggerFactory;
        ILogger originalLogger = LogOptions.Logger;
        using TestLoggerFactory testFactory = new();
        Options options = new() { LoggerFactory = testFactory };

        try
        {
            LogOptions.LoggerFactory = NullLoggerFactory.Instance;
            LogOptions.Logger = NullLogger.Instance;

            ILogger logger = LogOptions.CreateLogger("category", options);

            // Factory should be used when Logger is not set
            _ = logger.Should().BeSameAs(testFactory.Logger);
            _ = testFactory.LastCategory.Should().Be("category");
        }
        finally
        {
            LogOptions.LoggerFactory = originalFactory;
            LogOptions.Logger = originalLogger;
        }
    }

    [Fact]
    public void CreateLogger_WithOptions_FallsBackToGlobal()
    {
        ILoggerFactory originalFactory = LogOptions.LoggerFactory;
        ILogger originalLogger = LogOptions.Logger;
        using TestLoggerFactory testFactory = new();
        TestLogger testLogger = new();
        Options options = new();

        try
        {
            LogOptions.LoggerFactory = testFactory;
            LogOptions.Logger = testLogger;

            ILogger logger = LogOptions.CreateLogger("category", options);

            _ = logger.Should().BeSameAs(testLogger);
            _ = testFactory.LastCategory.Should().BeNull();
        }
        finally
        {
            LogOptions.LoggerFactory = originalFactory;
            LogOptions.Logger = originalLogger;
        }
    }

    [Fact]
    public void CreateLogger_FallsBackToFactory_WhenGlobalLoggerIsNullLogger()
    {
        ILoggerFactory originalFactory = LogOptions.LoggerFactory;
        ILogger originalLogger = LogOptions.Logger;
        using TestLoggerFactory testFactory = new();

        try
        {
            LogOptions.LoggerFactory = testFactory;
            LogOptions.Logger = NullLogger.Instance;

            ILogger logger = LogOptions.CreateLogger("category");

            _ = logger.Should().BeSameAs(testFactory.Logger);
            _ = testFactory.LastCategory.Should().Be("category");
        }
        finally
        {
            LogOptions.LoggerFactory = originalFactory;
            LogOptions.Logger = originalLogger;
        }
    }

    [Fact]
    public void CreateLogger_WithOptions_IgnoresNullLoggerAndFallsBackToFactory()
    {
        ILoggerFactory originalFactory = LogOptions.LoggerFactory;
        ILogger originalLogger = LogOptions.Logger;
        using TestLoggerFactory testFactory = new();
        Options options = new() { Logger = NullLogger.Instance, LoggerFactory = testFactory };

        try
        {
            LogOptions.LoggerFactory = NullLoggerFactory.Instance;
            LogOptions.Logger = NullLogger.Instance;

            ILogger logger = LogOptions.CreateLogger("category", options);

            _ = logger.Should().BeSameAs(testFactory.Logger);
            _ = testFactory.LastCategory.Should().Be("category");
        }
        finally
        {
            LogOptions.LoggerFactory = originalFactory;
            LogOptions.Logger = originalLogger;
        }
    }

    [Fact]
    public void CreateLogger_WithNullCategory_ThrowsArgumentNullException() =>
        _ = Assert.Throws<ArgumentNullException>(() => LogOptions.CreateLogger(null!));

    [Fact]
    public void CreateLogger_WithEmptyCategory_ThrowsArgumentException() =>
        _ = Assert.Throws<ArgumentException>(() => LogOptions.CreateLogger(" "));

    [Fact]
    public void TrySetFactory_WhenUnset_ReturnsTrueAndSets()
    {
        ILoggerFactory originalFactory = LogOptions.LoggerFactory;
        using TestLoggerFactory testFactory = new();

        try
        {
            LogOptions.LoggerFactory = NullLoggerFactory.Instance;

            bool result = LogOptions.TrySetFactory(testFactory);

            _ = result.Should().BeTrue();
            _ = LogOptions.LoggerFactory.Should().BeSameAs(testFactory);
        }
        finally
        {
            LogOptions.LoggerFactory = originalFactory;
        }
    }

    [Fact]
    public void TrySetFactory_WhenAlreadySet_ReturnsFalseAndDoesNotOverwrite()
    {
        ILoggerFactory originalFactory = LogOptions.LoggerFactory;
        using TestLoggerFactory testFactory = new();
        using TestLoggerFactory otherFactory = new();

        try
        {
            LogOptions.LoggerFactory = testFactory;

            bool result = LogOptions.TrySetFactory(otherFactory);

            _ = result.Should().BeFalse();
            _ = LogOptions.LoggerFactory.Should().BeSameAs(testFactory);
        }
        finally
        {
            LogOptions.LoggerFactory = originalFactory;
        }
    }

    [Fact]
    public void TrySetLogger_WhenUnset_ReturnsTrueAndSets()
    {
        ILogger originalLogger = LogOptions.Logger;
        TestLogger testLogger = new();

        try
        {
            LogOptions.Logger = NullLogger.Instance;

            bool result = LogOptions.TrySetLogger(testLogger);

            _ = result.Should().BeTrue();
            _ = LogOptions.Logger.Should().BeSameAs(testLogger);
        }
        finally
        {
            LogOptions.Logger = originalLogger;
        }
    }

    [Fact]
    public void TrySetLogger_WhenAlreadySet_ReturnsFalseAndDoesNotOverwrite()
    {
        ILogger originalLogger = LogOptions.Logger;
        TestLogger testLogger = new();
        TestLogger otherLogger = new();

        try
        {
            LogOptions.Logger = testLogger;

            bool result = LogOptions.TrySetLogger(otherLogger);

            _ = result.Should().BeFalse();
            _ = LogOptions.Logger.Should().BeSameAs(testLogger);
        }
        finally
        {
            LogOptions.Logger = originalLogger;
        }
    }

    private sealed class TestLoggerFactory : ILoggerFactory
    {
        public ILogger Logger { get; } = new TestLogger();

        public string? LastCategory { get; private set; }

        public void AddProvider(ILoggerProvider provider) { }

        public ILogger CreateLogger(string categoryName)
        {
            LastCategory = categoryName;
            return Logger;
        }

        public void Dispose() { }
    }

    private sealed class TestLogger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        ) { }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose() { }
        }
    }
}
