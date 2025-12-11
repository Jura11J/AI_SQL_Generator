using System.Collections.Generic;
using System.Linq;

namespace DbDesigner.Core.Schema;

public class DatabaseSchema
{
    public List<Table> Tables { get; } = new();
    public List<View> Views { get; } = new();

    public Table? FindTable(string name) =>
        Tables.FirstOrDefault(t => string.Equals(t.Name, name));

    public View? FindView(string name) =>
        Views.FirstOrDefault(v => string.Equals(v.Name, name));
}
