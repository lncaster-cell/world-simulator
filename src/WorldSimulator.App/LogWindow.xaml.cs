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
            oldViewModel.TechnicalLogEntries.CollectionChanged -= TechnicalLogEntries_CollectionChanged;
        }

        if (e.NewValue is MainWindowViewModel newViewModel)
        {
            newViewModel.TechnicalLogEntries.CollectionChanged += TechnicalLogEntries_CollectionChanged;
            ScheduleScrollToLatestLogEntry();
        }
    }

    private void TechnicalLogEntries_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
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
        if (TechnicalLogList.Items.Count == 0)
        {
            return;
        }

        var lastItem = TechnicalLogList.Items[^1];

        try
        {
            TechnicalLogList.ScrollIntoView(lastItem);
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
            viewModel.TechnicalLogEntries.CollectionChanged -= TechnicalLogEntries_CollectionChanged;
        }

        base.OnClosed(e);
    }
}
