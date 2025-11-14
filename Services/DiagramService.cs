using System.Text.Json;
using SchemaDiagramViewer.Models;

namespace SchemaDiagramViewer.Services;

public class DiagramService
{
    public string SerializeDiagram(DatabaseSchema schema)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        return JsonSerializer.Serialize(schema, options);
    }

    public DatabaseSchema DeserializeDiagram(string diagramData)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        return JsonSerializer.Deserialize<DatabaseSchema>(diagramData, options) ?? new DatabaseSchema();
    }
}

