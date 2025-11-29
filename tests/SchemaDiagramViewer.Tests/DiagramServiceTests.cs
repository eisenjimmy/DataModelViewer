using FluentAssertions;
using SchemaDiagramViewer.Models;
using SchemaDiagramViewer.Services;
using Xunit;
using System.Collections.Generic;

namespace SchemaDiagramViewer.Tests;

public class DiagramServiceTests
{
    [Fact]
    public void SerializeDiagram_ShouldReturnJsonString()
    {
        // Arrange
        var service = new DiagramService();
        var schema = new DatabaseSchema
        {
            Tables = new List<Table>
            {
                new Table { Name = "TestTable", Schema = "dbo", X = 10, Y = 20 }
            }
        };

        // Act
        var result = service.SerializeDiagram(schema);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("TestTable");
        result.Should().Contain("dbo");
    }

    [Fact]
    public void DeserializeDiagram_ShouldReturnSchemaObject()
    {
        // Arrange
        var service = new DiagramService();
        var json = "{\"Tables\":[{\"Schema\":\"dbo\",\"Name\":\"TestTable\",\"X\":10,\"Y\":20}],\"Views\":[],\"Relationships\":[],\"Labels\":[]}";

        // Act
        var result = service.DeserializeDiagram(json);

        // Assert
        result.Should().NotBeNull();
        result.Tables.Should().HaveCount(1);
        result.Tables[0].Name.Should().Be("TestTable");
        result.Tables[0].X.Should().Be(10);
    }
}
