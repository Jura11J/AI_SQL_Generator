using System.Collections.Generic;

namespace DbDesigner.Core.Schema;

public class Index
{
    public string Name { get; set; } = string.Empty;
    public List<string> Columns { get; } = new();
    public bool IsUnique { get; set; }
}
