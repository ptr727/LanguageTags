using System.Text.Json;
using System.Text.Json.Serialization;

namespace ptr727.LanguageTags;

internal sealed class JsonOptions
{
    public static readonly JsonSerializerOptions JsonReadOptions = new()
    {
        AllowTrailingCommas = true,
        IncludeFields = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public static readonly JsonSerializerOptions JsonWriteOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        IncludeFields = true,
        WriteIndented = true,
        NewLine = "\r\n",
    };
}
