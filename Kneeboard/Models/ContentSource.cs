using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kneeboard.Models;

[JsonConverter(typeof(ContentSourceConverter))]
public abstract class ContentSource { }

public class PdfSource : ContentSource
{
    public string Path { get; set; } = string.Empty;
}

public class ImageFolderSource : ContentSource
{
    public string Folder { get; set; } = string.Empty;
}

public class ContentSourceConverter : JsonConverter<ContentSource>
{
    public override ContentSource Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeProp))
            throw new JsonException("ContentSource is missing required field 'type'.");

        var type = typeProp.GetString();
        return type switch
        {
            "pdf" => new PdfSource
            {
                Path = root.TryGetProperty("path", out var path)
                    ? path.GetString() ?? throw new JsonException("ContentSource 'pdf' has null 'path'.")
                    : throw new JsonException("ContentSource 'pdf' is missing required field 'path'.")
            },
            "images" => new ImageFolderSource
            {
                Folder = root.TryGetProperty("folder", out var folder)
                    ? folder.GetString() ?? throw new JsonException("ContentSource 'images' has null 'folder'.")
                    : throw new JsonException("ContentSource 'images' is missing required field 'folder'.")
            },
            _ => throw new JsonException($"Unknown source type: '{type}'")
        };
    }

    public override void Write(Utf8JsonWriter writer, ContentSource value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        switch (value)
        {
            case PdfSource pdf:
                writer.WriteString("type", "pdf");
                writer.WriteString("path", pdf.Path);
                break;
            case ImageFolderSource img:
                writer.WriteString("type", "images");
                writer.WriteString("folder", img.Folder);
                break;
            default:
                throw new JsonException($"Unknown ContentSource subtype: {value.GetType().Name}");
        }
        writer.WriteEndObject();
    }
}
