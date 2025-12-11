using DbDesigner.AI;
using DbDesigner.Core.Schema;
using DbDesigner.Core.SchemaChanges;
using DbDesigner.Infrastructure.Crm;
using DbDesigner.Infrastructure.Sqlite;

namespace DbDesigner.App.ViewModels;

public class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
        var introspector = new SqliteDatabaseIntrospector();
        var proposalService = new StubChangeProposalService();
        var generator = new SqliteChangeScriptGenerator();
        var crmInitializer = new SampleCrmSchemaInitializer();

        Connection = new ConnectionViewModel(introspector, crmInitializer);
        Schema = new SchemaViewModel();
        DesignChanges = new DesignChangesViewModel(proposalService);
        ScriptPreview = new ScriptPreviewViewModel(generator);

        Connection.SchemaLoaded += OnSchemaLoaded;
        DesignChanges.SelectedChangesChanged += OnSelectedChangesChanged;
    }

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
}
