using System.ComponentModel;
using System.Runtime.CompilerServices;
using DbDesigner.Core.SchemaChanges;

namespace DbDesigner.App.Models;

public class SelectableSchemaChange : INotifyPropertyChanged
{
    private bool _isSelected = true;

    public SchemaChange Change { get; }
    public string? ObjectName { get; }
    public string? SchemaName { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public SelectableSchemaChange(SchemaChange change, string? schemaName = "main")
    {
        Change = change;
        ObjectName = ResolveObjectName(change);
        SchemaName = schemaName;
    }

    private static string? ResolveObjectName(SchemaChange change) =>
        change switch
        {
            CreateTableChange table => table.TableName,
            AddColumnChange column => column.TableName,
            CreateViewChange view => view.ViewName,
            _ => null
        };

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
