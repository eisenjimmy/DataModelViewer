using System.Data;
using Microsoft.Data.SqlClient;
using SchemaDiagramViewer.Models;

namespace SchemaDiagramViewer.Services;

public class SchemaReaderService
{
    public async Task<DatabaseSchema> ReadSchemaAsync(SqlConnection connection)
    {
        var schema = new DatabaseSchema();
        
        await connection.OpenAsync();
        
        try
        {
            schema.Tables = await ReadTablesAsync(connection);
            schema.Views = await ReadViewsAsync(connection);
            schema.Relationships = await ReadRelationshipsAsync(connection, schema.Tables);
            
            // Set initial positions in a grid layout
            SetInitialPositions(schema);
        }
        finally
        {
            if (connection.State == ConnectionState.Open)
                connection.Close();
        }
        
        return schema;
    }

    private async Task<List<Table>> ReadTablesAsync(SqlConnection connection)
    {
        var tables = new List<Table>();
        
        var query = @"
            SELECT 
                t.TABLE_SCHEMA,
                t.TABLE_NAME,
                c.COLUMN_NAME,
                c.DATA_TYPE,
                c.CHARACTER_MAXIMUM_LENGTH,
                c.IS_NULLABLE,
                CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS IS_PRIMARY_KEY
            FROM INFORMATION_SCHEMA.TABLES t
            INNER JOIN INFORMATION_SCHEMA.COLUMNS c ON t.TABLE_SCHEMA = c.TABLE_SCHEMA AND t.TABLE_NAME = c.TABLE_NAME
            LEFT JOIN (
                SELECT ku.TABLE_SCHEMA, ku.TABLE_NAME, ku.COLUMN_NAME
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                    ON tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                    AND tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
            ) pk ON c.TABLE_SCHEMA = pk.TABLE_SCHEMA 
                AND c.TABLE_NAME = pk.TABLE_NAME 
                AND c.COLUMN_NAME = pk.COLUMN_NAME
            WHERE t.TABLE_TYPE = 'BASE TABLE'
            ORDER BY t.TABLE_SCHEMA, t.TABLE_NAME, c.ORDINAL_POSITION";

        using var command = new SqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();
        
        Table? currentTable = null;
        
        while (await reader.ReadAsync())
        {
            var schemaName = reader.GetString(0);
            var tableName = reader.GetString(1);
            var fullName = $"{schemaName}.{tableName}";
            
            if (currentTable == null || currentTable.FullName != fullName)
            {
                if (currentTable != null)
                    tables.Add(currentTable);
                
                currentTable = new Table
                {
                    Schema = schemaName,
                    Name = tableName
                };
            }
            
            var column = new Column
            {
                Name = reader.GetString(2),
                DataType = reader.GetString(3),
                IsNullable = reader.GetString(5) == "YES",
                IsPrimaryKey = reader.GetInt32(6) == 1
            };
            
            if (!reader.IsDBNull(4))
            {
                var maxLength = reader.GetInt32(4);
                // SQL Server uses -1 for MAX length
                column.MaxLength = maxLength == -1 ? null : maxLength;
            }
            
            currentTable.Columns.Add(column);
            
            if (column.IsPrimaryKey)
                currentTable.PrimaryKeys.Add(column.Name);
        }
        
        if (currentTable != null)
            tables.Add(currentTable);
        
        return tables;
    }

    private async Task<List<View>> ReadViewsAsync(SqlConnection connection)
    {
        var views = new List<View>();
        
        var query = @"
            SELECT 
                v.TABLE_SCHEMA,
                v.TABLE_NAME,
                c.COLUMN_NAME,
                c.DATA_TYPE,
                c.CHARACTER_MAXIMUM_LENGTH,
                c.IS_NULLABLE
            FROM INFORMATION_SCHEMA.VIEWS v
            INNER JOIN INFORMATION_SCHEMA.COLUMNS c ON v.TABLE_SCHEMA = c.TABLE_SCHEMA AND v.TABLE_NAME = c.TABLE_NAME
            ORDER BY v.TABLE_SCHEMA, v.TABLE_NAME, c.ORDINAL_POSITION";

        using var command = new SqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();
        
        View? currentView = null;
        
        while (await reader.ReadAsync())
        {
            var schemaName = reader.GetString(0);
            var viewName = reader.GetString(1);
            var fullName = $"{schemaName}.{viewName}";
            
            if (currentView == null || currentView.FullName != fullName)
            {
                if (currentView != null)
                    views.Add(currentView);
                
                currentView = new View
                {
                    Schema = schemaName,
                    Name = viewName
                };
            }
            
            var column = new Column
            {
                Name = reader.GetString(2),
                DataType = reader.GetString(3),
                IsNullable = reader.GetString(5) == "YES"
            };
            
            if (!reader.IsDBNull(4))
            {
                var maxLength = reader.GetInt32(4);
                // SQL Server uses -1 for MAX length
                column.MaxLength = maxLength == -1 ? null : maxLength;
            }
            
            currentView.Columns.Add(column);
        }
        
        if (currentView != null)
            views.Add(currentView);
        
        return views;
    }

    private async Task<List<Relationship>> ReadRelationshipsAsync(SqlConnection connection, List<Table> tables)
    {
        var relationships = new List<Relationship>();
        
        var query = @"
            SELECT 
                fk.name AS FK_Name,
                OBJECT_SCHEMA_NAME(fk.parent_object_id) AS FK_Schema,
                OBJECT_NAME(fk.parent_object_id) AS FK_Table,
                COL_NAME(fc.parent_object_id, fc.parent_column_id) AS FK_Column,
                OBJECT_SCHEMA_NAME(fk.referenced_object_id) AS PK_Schema,
                OBJECT_NAME(fk.referenced_object_id) AS PK_Table,
                COL_NAME(fc.referenced_object_id, fc.referenced_column_id) AS PK_Column
            FROM sys.foreign_keys AS fk
            INNER JOIN sys.foreign_key_columns AS fc
                ON fk.object_id = fc.constraint_object_id
            ORDER BY FK_Schema, FK_Table, FK_Column";

        using var command = new SqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            var relationship = new Relationship
            {
                RelationshipName = reader.GetString(0),
                FromTable = $"{reader.GetString(1)}.{reader.GetString(2)}",
                FromColumn = reader.GetString(3),
                ToTable = $"{reader.GetString(4)}.{reader.GetString(5)}",
                ToColumn = reader.GetString(6)
            };
            
            relationships.Add(relationship);
        }
        
        return relationships;
    }

    private void SetInitialPositions(DatabaseSchema schema)
    {
        const double startX = 30;
        const double startY = 30;
        const double spacingX = 280;
        const double spacingY = 350;
        const int itemsPerRow = 5;
        
        int index = 0;
        
        foreach (var table in schema.Tables)
        {
            int row = index / itemsPerRow;
            int col = index % itemsPerRow;
            table.X = startX + col * spacingX;
            table.Y = startY + row * spacingY;
            index++;
        }
        
        foreach (var view in schema.Views)
        {
            int row = index / itemsPerRow;
            int col = index % itemsPerRow;
            view.X = startX + col * spacingX;
            view.Y = startY + row * spacingY;
            index++;
        }
    }
}

