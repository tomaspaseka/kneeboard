using System.Text.Json;
using Kneeboard.Models;
using Xunit;

namespace Kneeboard.Tests.Models;

public class DocumentDeserializationTests
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public void Deserialize_PdfSection_ParsesCorrectly()
    {
        const string json = """
            {
              "title": "EPKK 2026-06-10",
              "sections": [{
                "id": "datacard",
                "label": "Mission Datacard",
                "source": { "type": "pdf", "path": "/flights/datacard.pdf" }
              }]
            }
            """;

        var doc = JsonSerializer.Deserialize<KneeboardDocument>(json, Options)!;

        Assert.Equal("EPKK 2026-06-10", doc.Title);
        Assert.Single(doc.Sections);
        Assert.Equal("datacard", doc.Sections[0].Id);
        Assert.Equal("Mission Datacard", doc.Sections[0].Label);
        var src = Assert.IsType<PdfSource>(doc.Sections[0].Source);
        Assert.Equal("/flights/datacard.pdf", src.Path);
    }

    [Fact]
    public void Deserialize_ImageFolderSection_ParsesCorrectly()
    {
        const string json = """
            {
              "title": "T",
              "sections": [{
                "id": "airfields",
                "label": "Airfields Map",
                "source": { "type": "images", "folder": "/flights/airfields" }
              }]
            }
            """;

        var doc = JsonSerializer.Deserialize<KneeboardDocument>(json, Options)!;

        var src = Assert.IsType<ImageFolderSource>(doc.Sections[0].Source);
        Assert.Equal("/flights/airfields", src.Folder);
    }

    [Fact]
    public void Deserialize_MultipleSections_PreservesOrder()
    {
        const string json = """
            {
              "title": "T",
              "sections": [
                { "id":"a","label":"A","source":{"type":"images","folder":"/a"} },
                { "id":"b","label":"B","source":{"type":"pdf","path":"/b.pdf"} }
              ]
            }
            """;

        var doc = JsonSerializer.Deserialize<KneeboardDocument>(json, Options)!;

        Assert.Equal(2, doc.Sections.Count);
        Assert.IsType<ImageFolderSource>(doc.Sections[0].Source);
        Assert.IsType<PdfSource>(doc.Sections[1].Source);
    }

    [Fact]
    public void Deserialize_UnknownSourceType_ThrowsJsonException()
    {
        const string json = """
            {
              "title": "T",
              "sections": [{
                "id":"x","label":"X",
                "source":{"type":"video","url":"/x.mp4"}
              }]
            }
            """;

        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<KneeboardDocument>(json, Options));
    }
}
