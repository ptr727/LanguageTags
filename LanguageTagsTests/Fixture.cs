namespace ptr727.LanguageTags.Tests;

internal static class Fixture // : IDisposable
{
    // public void Dispose() => GC.SuppressFinalize(this);

    public static string GetDataFilePath(string fileName) =>
        Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "../../../../LanguageData", fileName)
        );
}
