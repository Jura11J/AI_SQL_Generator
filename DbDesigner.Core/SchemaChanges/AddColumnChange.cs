namespace DbDesigner.Core.SchemaChanges;

public class AddColumnChange : SchemaChange
{
    public string TableName { get; set; } = string.Empty;
    public ColumnDefinition Column { get; set; } = new();
}
