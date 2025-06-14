using System;
using System.IO;
using System.Reflection;

namespace ptr727.LanguageTags.Tests;

public class Fixture : IDisposable
{
    public void Dispose() => GC.SuppressFinalize(this);

    public static string GetDataFilePath(string fileName)
    {
        string assemblyDirectory = Path.GetDirectoryName(typeof(Fixture).Assembly.Location);
        string dataDirectory = Path.GetFullPath(
            Path.Combine(assemblyDirectory, "../../../../LanguageData")
        );
        return Path.GetFullPath(Path.Combine(dataDirectory, fileName));
    }
}
