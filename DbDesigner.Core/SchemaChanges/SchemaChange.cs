using System;

namespace DbDesigner.Core.SchemaChanges;

public abstract class SchemaChange
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Description { get; set; } = string.Empty;
}
