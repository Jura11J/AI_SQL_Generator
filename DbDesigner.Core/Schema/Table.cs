using System.Collections.Generic;

namespace DbDesigner.Core.Schema;

public class Table
{
    public string Name { get; set; } = string.Empty;
    public List<Column> Columns { get; } = new();
    public List<ForeignKey> ForeignKeys { get; } = new();
    public List<Index> Indexes { get; } = new();
}
