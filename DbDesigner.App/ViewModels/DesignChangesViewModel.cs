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
    private readonly IChangeProposalService _proposalService;
    private string _specificationText = "Przykladowa specyfikacja: dodaj tabele Opportunity z Name (text), Amount (real) i powiaz z Account.";
    private string _statusMessage = "Brak wygenerowanych zmian.";
    private bool _isBusy;

    public event Action? SelectedChangesChanged;

    public DesignChangesViewModel(IChangeProposalService proposalService)
    {
        _proposalService = proposalService;
        ProposedChanges = new ObservableCollection<SelectableSchemaChange>();
        ProposeChangesCommand = new RelayCommand(async _ => await ProposeChangesAsync(), _ => !IsBusy);
    }

    public DatabaseSchema? CurrentSchema { get; set; }

    public ObservableCollection<SelectableSchemaChange> ProposedChanges { get; }

    public ICommand ProposeChangesCommand { get; }

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
            StatusMessage = "Analiza specyfikacji...";

            var proposals = await _proposalService.ProposeChangesAsync(SpecificationText, CurrentSchema);

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

            StatusMessage = $"Zaproponowano {ProposedChanges.Count} zmian.";
            SelectedChangesChanged?.Invoke();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Blad generowania: {ex.Message}";
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
}
