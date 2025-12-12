using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Data;
using DbDesigner.App.Models;
using DbDesigner.Core.Schema;

namespace DbDesigner.App.ViewModels;

public class SchemaViewModel : ViewModelBase
{
    private DatabaseSchema? _schema;
    private Table? _selectedTable;
    private View? _selectedView;
    private string _summary = "Schemat niezaladowany.";
    private bool _isTableSelected;
    private bool _isViewSelected;
    private readonly ICollectionView _tablesView;
    private readonly ICollectionView _viewsView;
    private readonly ICollectionView _schemaObjectsView;
    private string _filterObjectName = string.Empty;
    private string _filterSchemaName = string.Empty;

    public ObservableCollection<Table> Tables { get; } = new();
    public ObservableCollection<View> Views { get; } = new();
    public ObservableCollection<Column> Columns { get; } = new();
    public ObservableCollection<ForeignKey> ForeignKeys { get; } = new();
    public ObservableCollection<ForeignKey> IncomingForeignKeys { get; } = new();
    public ObservableCollection<string> ViewTables { get; } = new();
    public ObservableCollection<string> ViewDependencies { get; } = new();
    public ObservableCollection<SchemaObjectItem> SchemaObjects { get; } = new();

    public SchemaViewModel()
    {
        _tablesView = CollectionViewSource.GetDefaultView(Tables);
        _viewsView = CollectionViewSource.GetDefaultView(Views);
        _schemaObjectsView = CollectionViewSource.GetDefaultView(SchemaObjects);

        _tablesView.Filter = FilterPredicate;
        _viewsView.Filter = FilterPredicate;
        _schemaObjectsView.Filter = FilterPredicate;
    }

    public Table? SelectedTable
    {
        get => _selectedTable;
        set
        {
            if (SetProperty(ref _selectedTable, value))
            {
                IsTableSelected = value != null;
                if (value != null)
                {
                    SelectedView = null;
                }
                RefreshTableDetails();
            }
        }
    }

    public View? SelectedView
    {
        get => _selectedView;
        set
        {
            if (SetProperty(ref _selectedView, value))
            {
                IsViewSelected = value != null;
                if (value != null)
                {
                    SelectedTable = null;
                }
                RefreshViewDetails();
            }
        }
    }

    public string Summary
    {
        get => _summary;
        private set => SetProperty(ref _summary, value);
    }

    public bool IsTableSelected
    {
        get => _isTableSelected;
        private set => SetProperty(ref _isTableSelected, value);
    }

    public bool IsViewSelected
    {
        get => _isViewSelected;
        private set => SetProperty(ref _isViewSelected, value);
    }

    public string FilterObjectName
    {
        get => _filterObjectName;
        set
        {
            if (SetProperty(ref _filterObjectName, value))
            {
                RefreshFilters();
            }
        }
    }

    public string FilterSchemaName
    {
        get => _filterSchemaName;
        set
        {
            if (SetProperty(ref _filterSchemaName, value))
            {
                RefreshFilters();
            }
        }
    }

    public void UpdateSchema(DatabaseSchema schema)
    {
        _schema = schema;

        Tables.Clear();
        Views.Clear();
        SchemaObjects.Clear();

        foreach (var table in schema.Tables.OrderBy(t => t.Name))
        {
            Tables.Add(table);
            SchemaObjects.Add(new SchemaObjectItem("main", table.Name, "Table", GetTableDependencies(table)));
        }

        foreach (var view in schema.Views.OrderBy(v => v.Name))
        {
            Views.Add(view);
            SchemaObjects.Add(new SchemaObjectItem("main", view.Name, "View", string.Join(", ", ExtractViewReferences(view, schema))));
        }

        SelectedTable = Tables.FirstOrDefault();
        SelectedView = Views.FirstOrDefault();
        Summary = $"Tabele: {Tables.Count}, widoki: {Views.Count}.";
        RefreshFilters();
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

    private void RefreshViewDetails()
    {
        ViewTables.Clear();
        ViewDependencies.Clear();

        if (_schema == null || SelectedView?.Definition is not { Length: > 0 } definition)
        {
            return;
        }

        var tableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var dependencyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var matches = Regex.Matches(definition, @"\b(from|join)\s+([\w\[\]\.]+)", RegexOptions.IgnoreCase);

        foreach (Match match in matches.Cast<Match>())
        {
            var rawName = match.Groups[2].Value;
            var normalized = NormalizeIdentifier(rawName);

            if (string.IsNullOrWhiteSpace(normalized))
            {
                continue;
            }

            var table = _schema.Tables.FirstOrDefault(t => t.Name.Equals(normalized, StringComparison.OrdinalIgnoreCase));
            if (table != null)
            {
                tableNames.Add(table.Name);
                continue;
            }

            var view = _schema.Views.FirstOrDefault(v => v.Name.Equals(normalized, StringComparison.OrdinalIgnoreCase));
            if (view != null)
            {
                dependencyNames.Add(view.Name);
            }
        }

        foreach (var name in tableNames.OrderBy(n => n))
        {
            ViewTables.Add(name);
        }

        foreach (var name in dependencyNames.OrderBy(n => n))
        {
            ViewDependencies.Add(name);
        }
    }

    private static string NormalizeIdentifier(string identifier)
    {
        var candidate = identifier.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
        candidate = candidate.Trim(',', ';', '(', ')');
        candidate = candidate.Trim('[', ']');

        if (candidate.Contains('.'))
        {
            candidate = candidate.Split('.').Last();
        }

        return candidate;
    }

    private string GetTableDependencies(Table table)
    {
        if (table.ForeignKeys.Count == 0)
        {
            return string.Empty;
        }

        var deps = table.ForeignKeys
            .Select(fk => fk.ToTable)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n);

        return string.Join(", ", deps);
    }

    private IEnumerable<string> ExtractViewReferences(View view, DatabaseSchema schema)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(view.Definition))
        {
            return names;
        }

        var matches = Regex.Matches(view.Definition, @"\b(from|join)\s+([\w\[\]\.]+)", RegexOptions.IgnoreCase);

        foreach (Match match in matches.Cast<Match>())
        {
            var rawName = match.Groups[2].Value;
            var normalized = NormalizeIdentifier(rawName);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                continue;
            }

            if (schema.Tables.Any(t => t.Name.Equals(normalized, StringComparison.OrdinalIgnoreCase)) ||
                schema.Views.Any(v => v.Name.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
            {
                names.Add(normalized);
            }
        }

        return names;
    }

    private bool FilterPredicate(object obj)
    {
        return obj switch
        {
            Table table => Matches(table.Name) && MatchesSchema("main"),
            View view => Matches(view.Name) && MatchesSchema("main"),
            SchemaObjectItem item => Matches(item.ObjectName) && MatchesSchema(item.SchemaName),
            _ => true
        };
    }

    private bool Matches(string? name) =>
        string.IsNullOrWhiteSpace(FilterObjectName) ||
        (name ?? string.Empty).IndexOf(FilterObjectName, StringComparison.OrdinalIgnoreCase) >= 0;

    private bool MatchesSchema(string? schema) =>
        string.IsNullOrWhiteSpace(FilterSchemaName) ||
        (schema ?? string.Empty).IndexOf(FilterSchemaName, StringComparison.OrdinalIgnoreCase) >= 0;

    private void RefreshFilters()
    {
        _tablesView.Refresh();
        _viewsView.Refresh();
        _schemaObjectsView.Refresh();
    }
}
