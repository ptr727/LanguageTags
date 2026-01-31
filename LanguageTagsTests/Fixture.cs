// Single instance for all tests in assembly
[assembly: AssemblyFixture(typeof(ptr727.LanguageTags.Tests.SingleInstanceFixture))]

namespace ptr727.LanguageTags.Tests;

public sealed class DisableParallelDefinition;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1063:Implement IDisposable Correctly",
    Justification = "No unmanaged resources to dispose"
)]
public class SingleInstanceFixture : IDisposable
{
    public void Dispose() => GC.SuppressFinalize(this);

    protected static string GetDataFilePath(string fileName) =>
        Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "../../../../LanguageData", fileName)
        );
}
