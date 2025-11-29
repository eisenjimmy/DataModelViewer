namespace SchemaDiagramViewer.Models;

public class DatabaseSchema
{
    public List<Table> Tables { get; set; } = new();
    public List<View> Views { get; set; } = new();
    public List<Relationship> Relationships { get; set; } = new();
    public List<DiagramShape> Shapes { get; set; } = new();
    public List<DiagramLabel> Labels { get; set; } = new();
    public List<StoredProcedure> StoredProcedures { get; set; } = new();
    public List<DatabaseFunction> Functions { get; set; } = new();
}

public class Table
{
    public string Schema { get; set; } = "";
    public string Name { get; set; } = "";
    public string FullName => $"{Schema}.{Name}";
    public List<Column> Columns { get; set; } = new();
    public List<string> PrimaryKeys { get; set; } = new();
    public double X { get; set; }
    public double Y { get; set; }
    public int ZIndex { get; set; } = 10;
}

public class View
{
    public string Schema { get; set; } = "";
    public string Name { get; set; } = "";
    public string FullName => $"{Schema}.{Name}";
    public string Definition { get; set; } = "";
    public List<Column> Columns { get; set; } = new();
    public double X { get; set; }
    public double Y { get; set; }
    public int ZIndex { get; set; } = 10;
}

public class Column
{
    public string Name { get; set; } = "";
    public string DataType { get; set; } = "";
    public int? MaxLength { get; set; }
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsForeignKey { get; set; }
}

public class Relationship
{
    public string FromTable { get; set; } = "";
    public string FromColumn { get; set; } = "";
    public string ToTable { get; set; } = "";
    public string ToColumn { get; set; } = "";
    public string RelationshipName { get; set; } = "";
}

public class DiagramLabel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Text { get; set; } = "New Label";
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 150;
    public double Height { get; set; } = 40;
    public int ZIndex { get; set; } = 5;
    public string Color { get; set; } = "bg-yellow-100";
}

public class DiagramShape
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = "rectangle";
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 100;
    public double Height { get; set; } = 100;
    public int ZIndex { get; set; } = 1; // Shapes behind tables/views
    public string BorderColor { get; set; } = "#000000";
    public string FillColor { get; set; } = "transparent";
    public int BorderWidth { get; set; } = 1;
}

public class StoredProcedure
{
    public string Schema { get; set; } = "";
    public string Name { get; set; } = "";
    public string FullName => $"{Schema}.{Name}";
}

public class DatabaseFunction
{
    public string Schema { get; set; } = "";
    public string Name { get; set; } = "";
    public string FullName => $"{Schema}.{Name}";
}
