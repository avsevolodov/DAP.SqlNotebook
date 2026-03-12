namespace DAP.SqlNotebook.Service.Client.V1;

public class SchemaAutocompleteRequest
{
    public string? Sql { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }
}

public class SchemaAutocompleteItem
{
    public string Label { get; set; } = "";
    public string Kind { get; set; } = "";
    public string? Detail { get; set; }
    public string InsertText { get; set; } = "";
}

public class SchemaDto
{
    public List<SchemaEntityDto> Entities { get; set; } = new();
    public List<SchemaRelationDto> Relations { get; set; } = new();
}

public class SchemaEntityDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public List<SchemaFieldDto> Fields { get; set; } = new();
}

public class SchemaFieldDto
{
    public string Name { get; set; } = "";
    public string? DataType { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsNullable { get; set; }
}

public class SchemaRelationDto
{
    public Guid? FromEntityId { get; set; }
    public Guid? ToEntityId { get; set; }
    public string? FromFieldName { get; set; }
    public string? ToFieldName { get; set; }
    public string? Name { get; set; }
}

