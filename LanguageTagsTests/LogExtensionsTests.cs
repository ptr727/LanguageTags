using Microsoft.Extensions.Logging.Abstractions;

namespace ptr727.LanguageTags.Tests;

public sealed class LogExtensionsTests
{
    [Fact]
    public void LogAndPropagate_ReturnsFalse() =>
        _ = NullLogger
            .Instance.LogAndPropagate(new InvalidOperationException("test"))
            .Should()
            .BeFalse();

    [Fact]
    public void LogAndHandle_ReturnsTrue() =>
        _ = NullLogger
            .Instance.LogAndHandle(new InvalidOperationException("test"))
            .Should()
            .BeTrue();
}
