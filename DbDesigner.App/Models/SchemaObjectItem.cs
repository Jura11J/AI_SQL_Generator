namespace DbDesigner.App.Models;

public class SchemaObjectItem
{
    public SchemaObjectItem(string schemaName, string objectName, string objectType, string dependencies)
    {
        SchemaName = schemaName;
        ObjectName = objectName;
        ObjectType = objectType;
        Dependencies = dependencies;
    }

    public string SchemaName { get; }
    public string ObjectName { get; }
    public string ObjectType { get; }
    public string Dependencies { get; }
}
