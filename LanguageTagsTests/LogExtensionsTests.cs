using Microsoft.Extensions.Logging;

namespace ptr727.LanguageTags.Tests;

public sealed class LogExtensionsTests
{
    [Fact]
    public void LogAndPropagate_LogsExceptionAtErrorAndReturnsFalse()
    {
        CapturingLogger logger = new();
        InvalidOperationException exception = new("test");

        bool result = logger.LogAndPropagate(exception);

        _ = result.Should().BeFalse();
        _ = logger.Entries.Should().ContainSingle();
        _ = logger.Entries[0].Level.Should().Be(LogLevel.Error);
        _ = logger.Entries[0].Exception.Should().BeSameAs(exception);
    }

    [Fact]
    public void LogAndHandle_LogsExceptionAtErrorAndReturnsTrue()
    {
        CapturingLogger logger = new();
        InvalidOperationException exception = new("test");

        bool result = logger.LogAndHandle(exception);

        _ = result.Should().BeTrue();
        _ = logger.Entries.Should().ContainSingle();
        _ = logger.Entries[0].Level.Should().Be(LogLevel.Error);
        _ = logger.Entries[0].Exception.Should().BeSameAs(exception);
    }

    private sealed class CapturingLogger : ILogger
    {
        public List<(LogLevel Level, Exception? Exception)> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        ) => Entries.Add((logLevel, exception));

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose() { }
        }
    }
}
