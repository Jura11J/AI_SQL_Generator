using System.Data.Common;
using System.Threading.Tasks;
using DbDesigner.Core.Schema;

namespace DbDesigner.Core.Introspection;

public interface IDatabaseIntrospector
{
    Task<DatabaseSchema> LoadSchemaAsync(DbConnection connection);
}
