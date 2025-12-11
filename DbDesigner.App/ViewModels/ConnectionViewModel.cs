using System;
using System.Threading.Tasks;
using System.Windows.Input;
using DbDesigner.Core.Introspection;
using DbDesigner.Core.Schema;
using DbDesigner.Infrastructure.Crm;
using Microsoft.Data.Sqlite;

namespace DbDesigner.App.ViewModels;

public class ConnectionViewModel : ViewModelBase
{
    private readonly IDatabaseIntrospector _introspector;
    private readonly SampleCrmSchemaInitializer _crmInitializer;
    private string _connectionString = "Data Source=sample.db";
    private bool _isBusy;
    private string _statusMessage = "Not connected";

    public event Action<DatabaseSchema>? SchemaLoaded;

    public ConnectionViewModel(IDatabaseIntrospector introspector, SampleCrmSchemaInitializer crmInitializer)
    {
        _introspector = introspector;
        _crmInitializer = crmInitializer;
        LoadSchemaCommand = new RelayCommand(async _ => await LoadSchemaAsync(), _ => CanLoad);
        CreateSampleCrmDatabaseCommand = new RelayCommand(async _ => await CreateSampleCrmDatabaseAsync(), _ => CanLoad);
    }

    public ICommand LoadSchemaCommand { get; }
    public ICommand CreateSampleCrmDatabaseCommand { get; }

    public string ConnectionString
    {
        get => _connectionString;
        set
        {
            if (SetProperty(ref _connectionString, value))
            {
                RaiseCanExecute();
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RaiseCanExecute();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    private bool CanLoad => !IsBusy && !string.IsNullOrWhiteSpace(ConnectionString);

    private async Task LoadSchemaAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Ladowanie schematu...";

            var schema = await LoadSchemaInternalAsync();

            if (schema != null)
            {
                StatusMessage = $"Wczytano schemat ({schema.Tables.Count} tabel, {schema.Views.Count} widokow).";
                SchemaLoaded?.Invoke(schema);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Blad polaczenia: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RaiseCanExecute()
    {
        (LoadSchemaCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (CreateSampleCrmDatabaseCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private async Task CreateSampleCrmDatabaseAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Tworzenie przykladowej bazy CRM...";

            await _crmInitializer.InitializeAsync(ConnectionString);

            var schema = await LoadSchemaInternalAsync();
            if (schema != null)
            {
                SchemaLoaded?.Invoke(schema);
                StatusMessage = $"Utworzono przykladowa baze CRM i wczytano schemat ({schema.Tables.Count} tabel, {schema.Views.Count} widokow).";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Blad tworzenia bazy CRM: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            RaiseCanExecute();
        }
    }

    private async Task<DatabaseSchema?> LoadSchemaInternalAsync()
    {
        await using var connection = new SqliteConnection(ConnectionString);
        return await _introspector.LoadSchemaAsync(connection);
    }
}
