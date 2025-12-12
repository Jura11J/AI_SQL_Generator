using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using DbDesigner.App.Helpers;
using DbDesigner.App.ViewModels;

namespace DbDesigner.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var sqlHighlighting = SqlHighlightingLoader.GetSqlHighlighting();
        SqlPreviewEditor.SyntaxHighlighting = sqlHighlighting;
        ViewDefinitionEditor.SyntaxHighlighting = sqlHighlighting;

        if (DataContext is MainViewModel vm)
        {
            vm.DesignChanges.ProposedChanges.CollectionChanged += OnProposedChangesChanged;
        }
    }

    private void OnProposedChangesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add || ProposedChangesGrid == null)
        {
            return;
        }

        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
        {
            if (ProposedChangesGrid.Items.Count > 0)
            {
                var last = ProposedChangesGrid.Items[ProposedChangesGrid.Items.Count - 1];
                ProposedChangesGrid.ScrollIntoView(last);
            }
        }));
    }
}
