using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FixIt.Messages;
using FixIt.Services;

namespace FixIt.ViewModels;

/// <summary>Map of all reports with a location, coloured by status; tap a pin to open the report.</summary>
public partial class MapViewModel : BaseViewModel, IRecipient<ReportsChangedMessage>
{
    private readonly DatabaseService _database;
    private readonly LocationService _location;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;

    private Location? _myLocation;

    public MapViewModel(
        DatabaseService database,
        LocationService location,
        INavigationService navigation,
        IDialogService dialogs,
        IConnectivity connectivity)
    {
        _database = database;
        _location = location;
        _navigation = navigation;
        _dialogs = dialogs;
        Title = "Map";

        IsOffline = connectivity.NetworkAccess != NetworkAccess.Internet;
        connectivity.ConnectivityChanged += (_, e) => MainThread.BeginInvokeOnMainThread(() =>
        {
            var wasOffline = IsOffline;
            IsOffline = e.NetworkAccess != NetworkAccess.Internet;

            // Back online: reload so the map tiles appear
            if (wasOffline && !IsOffline)
                LoadCommand.Execute(null);
        });

        WeakReferenceMessenger.Default.Register(this);
    }

    [ObservableProperty]
    public partial HtmlWebViewSource? MapSource { get; set; }

    [ObservableProperty]
    public partial bool IsOffline { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MapSummary))]
    public partial int MappedCount { get; set; }

    [ObservableProperty]
    public partial bool IsLocating { get; set; }

    public string MapSummary => MappedCount switch
    {
        0 => "No reports with a location yet",
        1 => "1 report on the map",
        _ => $"{MappedCount} reports on the map"
    };

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            var reports = (await _database.GetReportsAsync()).Where(r => r.HasLocation).ToList();
            MappedCount = reports.Count;
            MapSource = new HtmlWebViewSource
            {
                Html = MapHtmlBuilder.Build(reports, _myLocation),
                BaseUrl = MapHtmlBuilder.BaseUrl
            };
        }
        catch (Exception ex)
        {
            await _dialogs.ShowAlertAsync("Error", $"Could not load the map.\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ShowMyLocationAsync()
    {
        if (IsLocating) return;

        try
        {
            IsLocating = true;
            _myLocation = await _location.GetCurrentLocationAsync();
            if (_myLocation is null)
            {
                await _dialogs.ShowAlertAsync("Location", "Could not determine your location.");
                return;
            }
            await LoadAsync();
        }
        catch (PermissionException)
        {
            await _dialogs.ShowAlertAsync("Permission needed", "Allow location access to see yourself on the map.");
        }
        catch (FeatureNotEnabledException)
        {
            await _dialogs.ShowAlertAsync("Location is off", "Turn on location services and try again.");
        }
        catch (Exception ex)
        {
            await _dialogs.ShowAlertAsync("Location error", ex.Message);
        }
        finally
        {
            IsLocating = false;
        }
    }

    /// <summary>Called by the page when the WebView navigates; returns true if it was a pin link.</summary>
    public bool TryHandleNavigation(string? url)
    {
        if (url is null || !url.StartsWith(MapHtmlBuilder.ReportLinkPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var idText = url[MapHtmlBuilder.ReportLinkPrefix.Length..].TrimEnd('/');
        if (int.TryParse(idText, out var id))
        {
            MainThread.BeginInvokeOnMainThread(async () =>
                await _navigation.GoToAsync(Routes.ReportDetail,
                    new Dictionary<string, object> { [Routes.IdParameter] = id }));
        }
        return true;
    }

    public void Receive(ReportsChangedMessage message)
        => MainThread.BeginInvokeOnMainThread(() => LoadCommand.Execute(null));
}
