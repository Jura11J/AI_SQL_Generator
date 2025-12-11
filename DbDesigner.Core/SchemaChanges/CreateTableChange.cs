using System.Collections.Generic;

namespace DbDesigner.Core.SchemaChanges;

public class CreateTableChange : SchemaChange
{
    public string TableName { get; set; } = string.Empty;
    public List<ColumnDefinition> Columns { get; } = new();
}
