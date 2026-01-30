namespace ptr727.LanguageTags.Tests;

[CollectionDefinition("DisableParallelDefinition", DisableParallelization = true)]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Maintainability",
    "CA1515:Consider making public types internal",
    Justification = "https://xunit.net/docs/running-tests-in-parallel"
)]
public sealed class DisableParallelDefinition { }

internal static class Fixture // : IDisposable
{
    // public void Dispose() => GC.SuppressFinalize(this);

    public static string GetDataFilePath(string fileName) =>
        Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "../../../../LanguageData", fileName)
        );
}
