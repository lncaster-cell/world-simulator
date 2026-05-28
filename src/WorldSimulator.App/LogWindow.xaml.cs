using System.Collections.Specialized;
using System.Windows.Threading;
using WorldSimulator.App.ViewModels;

namespace WorldSimulator.App;

public partial class LogWindow : System.Windows.Window
{
    private bool _pendingScrollToLatest;

    public LogWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainWindowViewModel oldViewModel)
        {
            oldViewModel.Journal.FilteredSimulationJournalEntries.CollectionChanged -= JournalEntries_CollectionChanged;
        }

        if (e.NewValue is MainWindowViewModel newViewModel)
        {
            newViewModel.Journal.FilteredSimulationJournalEntries.CollectionChanged += JournalEntries_CollectionChanged;
            ScheduleScrollToLatestLogEntry();
        }
    }

    private void JournalEntries_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action is NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Reset)
        {
            ScheduleScrollToLatestLogEntry();
        }
    }

    private void ScheduleScrollToLatestLogEntry()
    {
        if (_pendingScrollToLatest)
        {
            return;
        }

        _pendingScrollToLatest = true;
        Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new System.Action(() =>
            {
                _pendingScrollToLatest = false;
                ScrollToLatestLogEntry();
            }));
    }

    private void ScrollToLatestLogEntry()
    {
        if (SimulationJournalGrid.Items.Count == 0)
        {
            return;
        }

        var lastItem = SimulationJournalGrid.Items[^1];

        try
        {
            SimulationJournalGrid.ScrollIntoView(lastItem);
        }
        catch
        {
            // Ignore auto-scroll failures to avoid crashing the application.
        }
    }

    protected override void OnClosed(System.EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.Journal.FilteredSimulationJournalEntries.CollectionChanged -= JournalEntries_CollectionChanged;
        }

        base.OnClosed(e);
    }
}
