using System.Collections.Generic;

namespace DbDesigner.Core.SchemaChanges;

public interface ISqlChangeScriptGenerator
{
    string GenerateScript(IEnumerable<SchemaChange> changes);
}
