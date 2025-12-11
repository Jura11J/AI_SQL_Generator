using System;
using System.Collections.ObjectModel;
using System.Linq;
using DbDesigner.Core.Schema;

namespace DbDesigner.App.ViewModels;

public class SchemaViewModel : ViewModelBase
{
    private DatabaseSchema? _schema;
    private Table? _selectedTable;
    private View? _selectedView;
    private string _summary = "Schemat niezaladowany.";

    public ObservableCollection<Table> Tables { get; } = new();
    public ObservableCollection<View> Views { get; } = new();
    public ObservableCollection<Column> Columns { get; } = new();
    public ObservableCollection<ForeignKey> ForeignKeys { get; } = new();
    public ObservableCollection<ForeignKey> IncomingForeignKeys { get; } = new();

    public Table? SelectedTable
    {
        get => _selectedTable;
        set
        {
            if (SetProperty(ref _selectedTable, value))
            {
                RefreshTableDetails();
            }
        }
    }

    public View? SelectedView
    {
        get => _selectedView;
        set => SetProperty(ref _selectedView, value);
    }

    public string Summary
    {
        get => _summary;
        private set => SetProperty(ref _summary, value);
    }

    public void UpdateSchema(DatabaseSchema schema)
    {
        _schema = schema;

        Tables.Clear();
        Views.Clear();

        foreach (var table in schema.Tables.OrderBy(t => t.Name))
        {
            Tables.Add(table);
        }

        foreach (var view in schema.Views.OrderBy(v => v.Name))
        {
            Views.Add(view);
        }

        SelectedTable = Tables.FirstOrDefault();
        SelectedView = Views.FirstOrDefault();
        Summary = $"Tabele: {Tables.Count}, widoki: {Views.Count}.";
    }

    private void RefreshTableDetails()
    {
        Columns.Clear();
        ForeignKeys.Clear();
        IncomingForeignKeys.Clear();

        if (SelectedTable == null)
        {
            return;
        }

        foreach (var column in SelectedTable.Columns.OrderBy(c => c.Name))
        {
            Columns.Add(column);
        }

        foreach (var fk in SelectedTable.ForeignKeys.OrderBy(f => f.Name))
        {
            ForeignKeys.Add(fk);
        }

        if (_schema != null)
        {
            foreach (var fk in _schema.Tables.SelectMany(t => t.ForeignKeys))
            {
                if (fk.ToTable.Equals(SelectedTable.Name, StringComparison.OrdinalIgnoreCase))
                {
                    IncomingForeignKeys.Add(fk);
                }
            }
        }
    }
}
