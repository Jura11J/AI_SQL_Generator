using System;
using System.Threading.Tasks;
using System.Windows.Input;
using DbDesigner.Core.Introspection;
using DbDesigner.Core.Schema;
using DbDesigner.Infrastructure.Crm;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

namespace DbDesigner.App.ViewModels;

public enum ConnectionMode
{
    DemoSqlite,
    SqlServer
}

public enum SqlAuthenticationMode
{
    SqlAuthentication,
    WindowsAuthentication
}

public class ConnectionViewModel : ViewModelBase
{
    private readonly IDatabaseIntrospector _introspector;
    private readonly SampleCrmSchemaInitializer _crmInitializer;
    private string _connectionString = "Data Source=sample.db";
    private bool _isBusy;
    private string _statusMessage = "Not connected";
    private ConnectionMode _mode = ConnectionMode.DemoSqlite;
    private SqlAuthenticationMode _authenticationMode = SqlAuthenticationMode.SqlAuthentication;
    private bool _encrypt = true;
    private bool _trustServerCertificate = true;

    private string _sqlServerName = "localhost";
    private string _sqlServerDatabase = "master";
    private string _sqlServerUser = "sa";
    private string _sqlServerPassword = string.Empty;

    public event Action<DatabaseSchema>? SchemaLoaded;

    public ConnectionViewModel(IDatabaseIntrospector introspector, SampleCrmSchemaInitializer crmInitializer)
    {
        _introspector = introspector;
        _crmInitializer = crmInitializer;
        LoadSchemaCommand = new RelayCommand(async _ => await LoadSchemaAsync(), _ => CanLoadDemo);
        CreateSampleCrmDatabaseCommand = new RelayCommand(async _ => await CreateSampleCrmDatabaseAsync(), _ => CanLoadDemo);
        TestSqlServerConnectionCommand = new RelayCommand(async _ => await TestSqlServerConnectionAsync(), _ => CanUseSqlServer);
        ConnectSqlServerCommand = new RelayCommand(async _ => await ConnectSqlServerAsync(), _ => CanUseSqlServer);
    }

    public ICommand LoadSchemaCommand { get; }
    public ICommand CreateSampleCrmDatabaseCommand { get; }
    public ICommand TestSqlServerConnectionCommand { get; }
    public ICommand ConnectSqlServerCommand { get; }

    public ConnectionMode Mode
    {
        get => _mode;
        set
        {
            if (SetProperty(ref _mode, value))
            {
                OnPropertyChanged(nameof(IsDemoMode));
                OnPropertyChanged(nameof(IsSqlServerMode));
                OnPropertyChanged(nameof(HintText));
                RaiseCanExecute();
            }
        }
    }

    public bool IsDemoMode
    {
        get => Mode == ConnectionMode.DemoSqlite;
        set
        {
            if (value)
            {
                Mode = ConnectionMode.DemoSqlite;
            }
        }
    }

    public bool IsSqlServerMode
    {
        get => Mode == ConnectionMode.SqlServer;
        set
        {
            if (value)
            {
                Mode = ConnectionMode.SqlServer;
            }
        }
    }

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

    public string SqlServerName
    {
        get => _sqlServerName;
        set
        {
            if (SetProperty(ref _sqlServerName, value))
            {
                RaiseCanExecute();
            }
        }
    }

    public string SqlServerDatabase
    {
        get => _sqlServerDatabase;
        set
        {
            if (SetProperty(ref _sqlServerDatabase, value))
            {
                RaiseCanExecute();
            }
        }
    }

    public string SqlServerUser
    {
        get => _sqlServerUser;
        set
        {
            if (SetProperty(ref _sqlServerUser, value))
            {
                RaiseCanExecute();
            }
        }
    }

    public string SqlServerPassword
    {
        get => _sqlServerPassword;
        set
        {
            if (SetProperty(ref _sqlServerPassword, value))
            {
                RaiseCanExecute();
            }
        }
    }

    public SqlAuthenticationMode AuthenticationMode
    {
        get => _authenticationMode;
        set
        {
            if (SetProperty(ref _authenticationMode, value))
            {
                RaiseCanExecute();
            }
        }
    }

    public bool Encrypt
    {
        get => _encrypt;
        set
        {
            if (SetProperty(ref _encrypt, value))
            {
                RaiseCanExecute();
            }
        }
    }

    public bool TrustServerCertificate
    {
        get => _trustServerCertificate;
        set
        {
            if (SetProperty(ref _trustServerCertificate, value))
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

    public string HintText =>
        Mode == ConnectionMode.DemoSqlite
            ? "Tryb demo: SQLite (sample.db). Mozesz stworzyc przykladowa baze CRM i wczytac schemat."
            : "Tryb MSSQL: podaj dane serwera i przetestuj polaczenie. Introspektor MSSQL w przygotowaniu.";

    private bool CanLoadDemo => !IsBusy && Mode == ConnectionMode.DemoSqlite && !string.IsNullOrWhiteSpace(ConnectionString);
    private bool CanUseSqlServer =>
        !IsBusy &&
        Mode == ConnectionMode.SqlServer &&
        !string.IsNullOrWhiteSpace(SqlServerName) &&
        !string.IsNullOrWhiteSpace(SqlServerDatabase) &&
        (AuthenticationMode == SqlAuthenticationMode.WindowsAuthentication || !string.IsNullOrWhiteSpace(SqlServerUser));

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
        (TestSqlServerConnectionCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (ConnectSqlServerCommand as RelayCommand)?.RaiseCanExecuteChanged();
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

    private async Task TestSqlServerConnectionAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Testowanie polaczenia MSSQL...";

            await using var connection = new SqlConnection(BuildSqlConnectionString());
            await connection.OpenAsync();
            StatusMessage = "Test polaczenia MSSQL udany.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Blad testu polaczenia MSSQL: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            RaiseCanExecute();
        }
    }

    private async Task ConnectSqlServerAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Laczenie z MSSQL...";

            await using var connection = new SqlConnection(BuildSqlConnectionString());
            await connection.OpenAsync();
            StatusMessage = "Polaczono (MSSQL) - introspekcja w toku.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Blad polaczenia MSSQL: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            RaiseCanExecute();
        }
    }

    private string BuildSqlConnectionString()
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = SqlServerName,
            InitialCatalog = SqlServerDatabase,
            Encrypt = Encrypt,
            TrustServerCertificate = TrustServerCertificate
        };

        if (AuthenticationMode == SqlAuthenticationMode.WindowsAuthentication)
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            builder.UserID = SqlServerUser;
            builder.Password = SqlServerPassword;
        }

        return builder.ConnectionString;
    }
}
