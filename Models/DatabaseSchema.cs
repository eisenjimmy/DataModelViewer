namespace SchemaDiagramViewer.Models;

public class DatabaseSchema
{
    public List<Table> Tables { get; set; } = new();
    public List<View> Views { get; set; } = new();
    public List<Relationship> Relationships { get; set; } = new();
}

public class Table
{
    public string Schema { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<Column> Columns { get; set; } = new();
    public List<string> PrimaryKeys { get; set; } = new();
    public double X { get; set; }
    public double Y { get; set; }
    public string FullName => $"{Schema}.{Name}";
}

public class View
{
    public string Schema { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<Column> Columns { get; set; } = new();
    public double X { get; set; }
    public double Y { get; set; }
    public string FullName => $"{Schema}.{Name}";
}

public class Column
{
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int? MaxLength { get; set; }
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsForeignKey { get; set; }
}

public class Relationship
{
    public string FromTable { get; set; } = string.Empty;
    public string FromColumn { get; set; } = string.Empty;
    public string ToTable { get; set; } = string.Empty;
    public string ToColumn { get; set; } = string.Empty;
    public string RelationshipName { get; set; } = string.Empty;
}

