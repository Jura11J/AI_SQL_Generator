using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DbDesigner.Core.SchemaChanges;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;

namespace DbDesigner.App.ViewModels;

public class ScriptPreviewViewModel : ViewModelBase
{
    private readonly ISqlChangeScriptGenerator _generator;
    private string _generatedScript = string.Empty;
    private string _statusMessage = "Brak wygenerowanego skryptu.";
    private bool _isBusy;
    private string? _connectionString;

    public ScriptPreviewViewModel(ISqlChangeScriptGenerator generator)
    {
        _generator = generator;
        SaveToFileCommand = new RelayCommand(_ => SaveToFile(), _ => CanUseScript);
        ExecuteOnDatabaseCommand = new RelayCommand(async _ => await ExecuteAsync(), _ => CanExecuteScript);
    }

    public ICommand SaveToFileCommand { get; }
    public ICommand ExecuteOnDatabaseCommand { get; }

    public string GeneratedScript
    {
        get => _generatedScript;
        private set
        {
            if (SetProperty(ref _generatedScript, value))
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

    public string? ConnectionString
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

    public void RefreshScript(IEnumerable<SchemaChange> changes)
    {
        var selected = changes.ToList();
        if (selected.Count == 0)
        {
            GeneratedScript = string.Empty;
            StatusMessage = "Brak zaznaczonych zmian.";
            return;
        }

        GeneratedScript = _generator.GenerateScript(selected);
        StatusMessage = $"Wygenerowano skrypt dla {selected.Count} zmian.";
    }

    private bool CanUseScript => !string.IsNullOrWhiteSpace(GeneratedScript);
    private bool CanExecuteScript => CanUseScript && !IsBusy && !string.IsNullOrWhiteSpace(ConnectionString);

    private void SaveToFile()
    {
        if (!CanUseScript)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "SQL file (*.sql)|*.sql|All files (*.*)|*.*",
            FileName = "changes.sql"
        };

        var result = dialog.ShowDialog();
        if (result == true)
        {
            File.WriteAllText(dialog.FileName, GeneratedScript);
            StatusMessage = $"Zapisano skrypt do pliku: {dialog.FileName}";
        }
    }

    private async Task ExecuteAsync()
    {
        if (!CanExecuteScript || ConnectionString == null)
        {
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "Wykonywanie skryptu...";

            await using var connection = new SqliteConnection(ConnectionString);
            await connection.OpenAsync();

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = GeneratedScript;
            var affected = await cmd.ExecuteNonQueryAsync();

            StatusMessage = $"Skrypt wykonany. Zmieniono {affected} wierszy (liczba zalezy od SQLite).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Blad wykonania: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RaiseCanExecute()
    {
        (SaveToFileCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (ExecuteOnDatabaseCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }
}
