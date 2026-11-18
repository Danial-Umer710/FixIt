using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FixIt.Messages;
using FixIt.Models;
using FixIt.Services;

namespace FixIt.ViewModels;

/// <summary>List of all reports with search, status filter, statistics and delete.</summary>
public partial class ReportsViewModel : BaseViewModel, IRecipient<ReportsChangedMessage>
{
    private const string FilterAll = "All";

    private readonly DatabaseService _database;
    private readonly PhotoService _photos;
    private readonly AddressSyncService _addressSync;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;

    private List<IssueReport> _allReports = new();

    public ReportsViewModel(
        DatabaseService database,
        PhotoService photos,
        AddressSyncService addressSync,
        INavigationService navigation,
        IDialogService dialogs,
        IConnectivity connectivity)
    {
        _database = database;
        _photos = photos;
        _addressSync = addressSync;
        _navigation = navigation;
        _dialogs = dialogs;

        Title = "FixIt Reports";
        SearchText = string.Empty;
        SelectedFilter = FilterAll;

        IsOffline = connectivity.NetworkAccess != NetworkAccess.Internet;
        connectivity.ConnectivityChanged += (_, e) =>
            MainThread.BeginInvokeOnMainThread(() => IsOffline = e.NetworkAccess != NetworkAccess.Internet);

        WeakReferenceMessenger.Default.Register(this);
    }

    public ObservableCollection<IssueReport> Reports { get; } = new();

    public IReadOnlyList<string> Filters { get; } = new[]
    {
        FilterAll,
        ReportText.Status(IssueStatus.Open),
        ReportText.Status(IssueStatus.InProgress),
        ReportText.Status(IssueStatus.Fixed)
    };

    [ObservableProperty]
    public partial string SelectedFilter { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; }

    [ObservableProperty]
    public partial bool IsOffline { get; set; }

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    [ObservableProperty]
    public partial int OpenCount { get; set; }

    [ObservableProperty]
    public partial int InProgressCount { get; set; }

    [ObservableProperty]
    public partial int FixedCount { get; set; }

    partial void OnSelectedFilterChanged(string value) => ApplyFilter();

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    /// <summary>Selecting a report in the list opens it, then the selection is cleared.</summary>
    [ObservableProperty]
    public partial IssueReport? SelectedReport { get; set; }

    partial void OnSelectedReportChanged(IssueReport? value)
    {
        if (value is null) return;
        SelectedReport = null;
        OpenReportCommand.Execute(value);
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            _allReports = await _database.GetReportsAsync();
            UpdateCounts();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            await _dialogs.ShowAlertAsync("Error", $"Could not load reports.\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        // Pull-to-refresh also retries resolving addresses of reports created offline.
        await _addressSync.SyncPendingAsync();
        await LoadAsync();
    }

    [RelayCommand]
    private Task AddReportAsync() => _navigation.GoToAsync(Routes.ReportEdit);

    [RelayCommand]
    private Task OpenReportAsync(IssueReport? report)
    {
        if (report is null) return Task.CompletedTask;

        return _navigation.GoToAsync(Routes.ReportDetail,
            new Dictionary<string, object> { [Routes.IdParameter] = report.Id });
    }

    [RelayCommand]
    private async Task DeleteReportAsync(IssueReport? report)
    {
        if (report is null) return;

        var confirmed = await _dialogs.ConfirmAsync("Delete report",
            $"Delete \"{report.Title}\"? This cannot be undone.", "Delete", "Cancel");
        if (!confirmed) return;

        try
        {
            await _database.DeleteReportAsync(report);
            _photos.DeletePhoto(report.PhotoPath);

            _allReports.Remove(report);
            Reports.Remove(report);
            UpdateCounts();
            UpdateEmptyState();
        }
        catch (Exception ex)
        {
            await _dialogs.ShowAlertAsync("Error", $"Could not delete the report.\n{ex.Message}");
        }
    }

    public void Receive(ReportsChangedMessage message)
        => MainThread.BeginInvokeOnMainThread(() => LoadCommand.Execute(null));

    private void ApplyFilter()
    {
        IEnumerable<IssueReport> query = _allReports;

        if (SelectedFilter is not null && SelectedFilter != FilterAll)
            query = query.Where(r => ReportText.Status(r.Status) == SelectedFilter);

        var search = SearchText?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(r =>
                r.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                r.Description.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (r.Address?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                ReportText.Category(r.Category).Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        Reports.Clear();
        foreach (var report in query)
            Reports.Add(report);

        UpdateEmptyState();
    }

    /// <summary>True when the (filtered) list is empty – drives the empty-state panel.</summary>
    public bool IsEmpty => Reports.Count == 0;

    public string EmptyTitle => _allReports.Count == 0 ? "No reports yet" : "No matching reports";

    public string EmptyMessage => _allReports.Count == 0
        ? "Tap + to report a pothole, a broken streetlight or anything else in your neighbourhood that needs fixing."
        : "Try another filter or search term.";

    private void UpdateEmptyState()
    {
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(EmptyTitle));
        OnPropertyChanged(nameof(EmptyMessage));
    }

    private void UpdateCounts()
    {
        OpenCount = _allReports.Count(r => r.Status == IssueStatus.Open);
        InProgressCount = _allReports.Count(r => r.Status == IssueStatus.InProgress);
        FixedCount = _allReports.Count(r => r.Status == IssueStatus.Fixed);
    }
}
