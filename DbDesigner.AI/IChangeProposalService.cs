using System.Collections.Generic;
using System.Threading.Tasks;
using DbDesigner.Core.Schema;
using DbDesigner.Core.SchemaChanges;

namespace DbDesigner.AI;

public interface IChangeProposalService
{
    Task<IReadOnlyList<SchemaChange>> ProposeChangesAsync(string specification, DatabaseSchema? currentSchema);
}
