using System.Collections.Specialized;
using WorldSimulator.App.ViewModels;

namespace WorldSimulator.App;

public partial class LogWindow : System.Windows.Window
{
    public LogWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainWindowViewModel oldViewModel)
        {
            oldViewModel.TechnicalLogEntries.CollectionChanged -= TechnicalLogEntries_CollectionChanged;
        }

        if (e.NewValue is MainWindowViewModel newViewModel)
        {
            newViewModel.TechnicalLogEntries.CollectionChanged += TechnicalLogEntries_CollectionChanged;
            ScrollToLatestLogEntry();
        }
    }

    private void TechnicalLogEntries_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            ScrollToLatestLogEntry();
        }
    }

    private void ScrollToLatestLogEntry()
    {
        if (TechnicalLogList.Items.Count == 0)
        {
            return;
        }

        TechnicalLogList.ScrollIntoView(TechnicalLogList.Items[^1]);
    }

    protected override void OnClosed(System.EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.TechnicalLogEntries.CollectionChanged -= TechnicalLogEntries_CollectionChanged;
        }

        base.OnClosed(e);
    }
}
