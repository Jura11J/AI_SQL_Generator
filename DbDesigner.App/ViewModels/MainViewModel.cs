using System.ComponentModel;
using System.Net.Http;
using DbDesigner.AI;
using DbDesigner.App.Theme;
using DbDesigner.Core.Schema;
using DbDesigner.Core.SchemaChanges;
using DbDesigner.Infrastructure.Crm;
using DbDesigner.Infrastructure.Sqlite;

namespace DbDesigner.App.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly HttpClient _httpClient = new();

    public MainViewModel()
    {
        Settings = new SettingsViewModel();
        ThemeManager.ApplyTheme(Settings.Theme);

        var introspector = new SqliteDatabaseIntrospector();
        var localService = new StubChangeProposalService();
        var apiService = new ApiChangeProposalService(
            _httpClient,
            () => new ApiSettings { BaseUrl = Settings.ApiBaseUrl, ApiKey = Settings.ApiKey });
        var generator = new SqliteChangeScriptGenerator();
        var crmInitializer = new SampleCrmSchemaInitializer();

        Connection = new ConnectionViewModel(introspector, crmInitializer);
        Schema = new SchemaViewModel();
        DesignChanges = new DesignChangesViewModel(localService, apiService);
        ScriptPreview = new ScriptPreviewViewModel(generator);
        DesignChanges.ChatBackendMode = Settings.ChatBackend;

        Connection.SchemaLoaded += OnSchemaLoaded;
        DesignChanges.SelectedChangesChanged += OnSelectedChangesChanged;
        Settings.PropertyChanged += OnSettingsChanged;
    }

    public SettingsViewModel Settings { get; }
    public ConnectionViewModel Connection { get; }
    public SchemaViewModel Schema { get; }
    public DesignChangesViewModel DesignChanges { get; }
    public ScriptPreviewViewModel ScriptPreview { get; }

    private void OnSchemaLoaded(DatabaseSchema schema)
    {
        Schema.UpdateSchema(schema);
        DesignChanges.CurrentSchema = schema;
        ScriptPreview.ConnectionString = Connection.ConnectionString;
    }

    private void OnSelectedChangesChanged()
    {
        var selected = DesignChanges.GetSelectedChanges();
        ScriptPreview.RefreshScript(selected);
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.ChatBackend))
        {
            DesignChanges.ChatBackendMode = Settings.ChatBackend;
        }
        else if (e.PropertyName == nameof(SettingsViewModel.Theme))
        {
            ThemeManager.ApplyTheme(Settings.Theme);
        }
    }
}
