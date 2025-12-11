using System.ComponentModel;
using System.Runtime.CompilerServices;
using DbDesigner.Core.SchemaChanges;

namespace DbDesigner.App.Models;

public class SelectableSchemaChange : INotifyPropertyChanged
{
    private bool _isSelected = true;

    public SchemaChange Change { get; init; }

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

    public SelectableSchemaChange(SchemaChange change)
    {
        Change = change;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
