using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DbDesigner.App.Models;
using DbDesigner.AI;
using DbDesigner.Core.Schema;
using DbDesigner.Core.SchemaChanges;

namespace DbDesigner.App.ViewModels;

public class DesignChangesViewModel : ViewModelBase
{
    private readonly IChangeProposalService _localService;
    private readonly IChangeProposalService _apiService;
    private string _specificationText = "Przykladowa specyfikacja: dodaj tabele Opportunity z Name (text), Amount (real) i powiaz z Account.";
    private string _statusMessageBase = "Brak wygenerowanych zmian.";
    private string _statusMessage = string.Empty;
    private bool _isBusy;
    private ChatBackendMode _chatBackendMode = ChatBackendMode.LocalStub;
    private bool _isStatusError;

    public event Action? SelectedChangesChanged;

    public DesignChangesViewModel(IChangeProposalService localService, IChangeProposalService apiService)
    {
        _localService = localService;
        _apiService = apiService;
        ProposedChanges = new ObservableCollection<SelectableSchemaChange>();
        ProposeChangesCommand = new RelayCommand(async _ => await ProposeChangesAsync(), _ => !IsBusy);
        RefreshStatusMessage();
    }

    public DatabaseSchema? CurrentSchema { get; set; }

    public ObservableCollection<SelectableSchemaChange> ProposedChanges { get; }

    public ICommand ProposeChangesCommand { get; }

    public ChatBackendMode ChatBackendMode
    {
        get => _chatBackendMode;
        set
        {
            if (SetProperty(ref _chatBackendMode, value))
            {
                RefreshStatusMessage();
            }
        }
    }

    public string SpecificationText
    {
        get => _specificationText;
        set => SetProperty(ref _specificationText, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsStatusError
    {
        get => _isStatusError;
        private set => SetProperty(ref _isStatusError, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                (ProposeChangesCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public SchemaChange[] GetSelectedChanges() =>
        ProposedChanges.Where(c => c.IsSelected).Select(c => c.Change).ToArray();

    private async Task ProposeChangesAsync()
    {
        try
        {
            IsBusy = true;
            UpdateStatusMessage("Analiza specyfikacji...", false);

            var service = ChatBackendMode == ChatBackendMode.ExternalApi ? _apiService : _localService;
            var proposals = await service.ProposeChangesAsync(SpecificationText, CurrentSchema);
            var fallbackUsed = false;

            if (ChatBackendMode == ChatBackendMode.ExternalApi && (proposals == null || proposals.Count == 0))
            {
                UpdateStatusMessage("Blad API lub brak poprawnej konfiguracji - uzywam lokalnego stuba.", true);
                proposals = await _localService.ProposeChangesAsync(SpecificationText, CurrentSchema);
                fallbackUsed = true;
            }
            else
            {
                UpdateStatusMessage("Analiza specyfikacji...", false);
            }

            proposals ??= Array.Empty<SchemaChange>();

            foreach (var change in ProposedChanges)
            {
                change.PropertyChanged -= OnChangeSelectionChanged;
            }

            ProposedChanges.Clear();

            foreach (var proposal in proposals)
            {
                var selectable = new SelectableSchemaChange(proposal);
                selectable.PropertyChanged += OnChangeSelectionChanged;
                ProposedChanges.Add(selectable);
            }

            if (fallbackUsed)
            {
                UpdateStatusMessage($"Brak poprawnej konfiguracji API - uzywam lokalnego stuba. Zaproponowano {ProposedChanges.Count} zmian.", true);
            }
            else
            {
                UpdateStatusMessage($"Zaproponowano {ProposedChanges.Count} zmian.", false);
            }
            SelectedChangesChanged?.Invoke();
        }
        catch (Exception ex)
        {
            UpdateStatusMessage($"Blad generowania: {ex.Message}", true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnChangeSelectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectableSchemaChange.IsSelected))
        {
            SelectedChangesChanged?.Invoke();
        }
    }

    private string BackendSuffix => ChatBackendMode == ChatBackendMode.ExternalApi ? "(Tryb: API)" : "(Tryb: lokalny)";

    private void UpdateStatusMessage(string message, bool isError)
    {
        _statusMessageBase = message;
        IsStatusError = isError;
        StatusMessage = $"{message} {BackendSuffix}";
    }

    private void RefreshStatusMessage() =>
        StatusMessage = $"{_statusMessageBase} {BackendSuffix}";
}
