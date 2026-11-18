using CommunityToolkit.Mvvm.Messaging;
using FixIt.Messages;

namespace FixIt.Services;

/// <summary>
/// Reports saved while offline keep their GPS coordinates but have no street address.
/// This service listens for connectivity changes and resolves those addresses as soon as
/// the device is back online, then notifies the UI.
/// </summary>
public class AddressSyncService
{
    private readonly IConnectivity _connectivity;
    private readonly DatabaseService _database;
    private readonly LocationService _location;
    private int _isRunning;
    private bool _started;

    public AddressSyncService(IConnectivity connectivity, DatabaseService database, LocationService location)
    {
        _connectivity = connectivity;
        _database = database;
        _location = location;
    }

    public void Start()
    {
        if (_started) return;
        _started = true;

        _connectivity.ConnectivityChanged += OnConnectivityChanged;
        _ = SyncPendingAsync();
    }

    private async void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        if (e.NetworkAccess == NetworkAccess.Internet)
            await SyncPendingAsync();
    }

    /// <returns>Number of reports whose address was filled in.</returns>
    public async Task<int> SyncPendingAsync()
    {
        if (_connectivity.NetworkAccess != NetworkAccess.Internet)
            return 0;

        // Only one sync at a time
        if (Interlocked.Exchange(ref _isRunning, 1) == 1)
            return 0;

        var updated = 0;
        try
        {
            var pending = await _database.GetReportsPendingAddressAsync();
            foreach (var report in pending)
            {
                if (!report.HasLocation)
                {
                    report.AddressPending = false;
                    await _database.SaveReportAsync(report, touch: false);
                    continue;
                }

                var address = await _location.GetAddressAsync(report.Latitude!.Value, report.Longitude!.Value);
                if (address is null) continue;

                report.Address = address;
                report.AddressPending = false;
                await _database.SaveReportAsync(report, touch: false);
                updated++;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Address sync failed: {ex}");
        }
        finally
        {
            Interlocked.Exchange(ref _isRunning, 0);
        }

        if (updated > 0)
            WeakReferenceMessenger.Default.Send(new ReportsChangedMessage(updated));

        return updated;
    }
}
