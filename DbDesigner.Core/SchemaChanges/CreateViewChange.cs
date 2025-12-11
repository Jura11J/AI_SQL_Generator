namespace DbDesigner.Core.SchemaChanges;

public class CreateViewChange : SchemaChange
{
    public string ViewName { get; set; } = string.Empty;
    public string SqlDefinition { get; set; } = string.Empty;
}
