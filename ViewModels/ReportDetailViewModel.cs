using System.Globalization;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FixIt.Models;
using FixIt.Services;

namespace FixIt.ViewModels;

/// <summary>Shows one report: change status, open in maps, share, edit, delete.</summary>
public partial class ReportDetailViewModel : BaseViewModel, IQueryAttributable
{
    private readonly DatabaseService _database;
    private readonly PhotoService _photos;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;
    private readonly IShare _share;
    private readonly IMap _map;

    private int _reportId;

    public ReportDetailViewModel(
        DatabaseService database,
        PhotoService photos,
        INavigationService navigation,
        IDialogService dialogs,
        IShare share,
        IMap map)
    {
        _database = database;
        _photos = photos;
        _navigation = navigation;
        _dialogs = dialogs;
        _share = share;
        _map = map;
        Title = "Report";
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPhoto), nameof(HasLocation), nameof(CanMarkInProgress),
        nameof(CanMarkFixed), nameof(CanReopen), nameof(HasDescription))]
    public partial IssueReport? Report { get; set; }

    public bool HasPhoto => Report?.HasPhoto ?? false;
    public bool HasLocation => Report?.HasLocation ?? false;
    public bool HasDescription => !string.IsNullOrWhiteSpace(Report?.Description);
    public bool CanMarkInProgress => Report?.Status == IssueStatus.Open;
    public bool CanMarkFixed => Report is not null && Report.Status != IssueStatus.Fixed;
    public bool CanReopen => Report?.Status == IssueStatus.Fixed;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(Routes.IdParameter, out var value) && value is int id)
            _reportId = id;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (_reportId == 0) return;

        try
        {
            IsBusy = true;
            var loaded = await _database.GetReportAsync(_reportId);
            if (loaded is null)
            {
                await _dialogs.ShowAlertAsync("Not found", "This report no longer exists.");
                await _navigation.GoBackAsync();
                return;
            }

            Report = loaded;
            Title = loaded.Title;
        }
        catch (Exception ex)
        {
            await _dialogs.ShowAlertAsync("Error", $"Could not load the report.\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SetStatusAsync(IssueStatus status)
    {
        if (Report is null || Report.Status == status) return;

        var previous = Report.Status;
        try
        {
            Report.Status = status;
            await _database.SaveReportAsync(Report);

            // Same instance, so raise change notification to refresh all "Report.*" bindings
            OnPropertyChanged(nameof(Report));
            OnPropertyChanged(nameof(CanMarkInProgress));
            OnPropertyChanged(nameof(CanMarkFixed));
            OnPropertyChanged(nameof(CanReopen));
        }
        catch (Exception ex)
        {
            Report.Status = previous;
            await _dialogs.ShowAlertAsync("Error", $"Could not update the status.\n{ex.Message}");
        }
    }

    [RelayCommand]
    private Task EditAsync()
        => _navigation.GoToAsync(Routes.ReportEdit,
            new Dictionary<string, object> { [Routes.IdParameter] = _reportId });

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (Report is null) return;

        var confirmed = await _dialogs.ConfirmAsync("Delete report",
            $"Delete \"{Report.Title}\"? This cannot be undone.", "Delete", "Cancel");
        if (!confirmed) return;

        try
        {
            await _database.DeleteReportAsync(Report);
            _photos.DeletePhoto(Report.PhotoPath);
            await _navigation.GoBackAsync();
        }
        catch (Exception ex)
        {
            await _dialogs.ShowAlertAsync("Error", $"Could not delete the report.\n{ex.Message}");
        }
    }

    [RelayCommand]
    private async Task OpenInMapsAsync()
    {
        if (Report is not { HasLocation: true }) return;

        try
        {
            await _map.OpenAsync(Report.Latitude!.Value, Report.Longitude!.Value,
                new MapLaunchOptions { Name = Report.Title });
        }
        catch (Exception ex)
        {
            await _dialogs.ShowAlertAsync("Maps", $"No maps app could be opened.\n{ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ShareAsync()
    {
        if (Report is null) return;

        try
        {
            var text = BuildShareText(Report);

            var choice = "Text";
            if (Report.HasPhoto)
                choice = await _dialogs.ShowActionSheetAsync("Share report", "Text", "Photo") ?? string.Empty;

            switch (choice)
            {
                case "Text":
                    await _share.RequestAsync(new ShareTextRequest
                    {
                        Title = "Share issue report",
                        Subject = $"Issue report: {Report.Title}",
                        Text = text
                    });
                    break;

                case "Photo":
                    await _share.RequestAsync(new ShareFileRequest
                    {
                        Title = $"Issue report: {Report.Title}",
                        File = new ShareFile(Report.PhotoPath!)
                    });
                    break;
            }
        }
        catch (Exception ex)
        {
            await _dialogs.ShowAlertAsync("Share", $"Could not share the report.\n{ex.Message}");
        }
    }

    private static string BuildShareText(IssueReport r)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"🚧 Issue report: {r.Title}");
        sb.AppendLine($"{ReportText.CategoryIcon(r.Category)} Category: {ReportText.Category(r.Category)}");
        sb.AppendLine($"Status: {ReportText.Status(r.Status)} · Severity: {ReportText.Severity(r.Severity)}");

        if (!string.IsNullOrWhiteSpace(r.Description))
        {
            sb.AppendLine();
            sb.AppendLine(r.Description);
        }

        if (r.HasLocation)
        {
            sb.AppendLine();
            if (!string.IsNullOrWhiteSpace(r.Address))
                sb.AppendLine($"📍 {r.Address}");

            var lat = r.Latitude!.Value.ToString("F6", CultureInfo.InvariantCulture);
            var lon = r.Longitude!.Value.ToString("F6", CultureInfo.InvariantCulture);
            sb.AppendLine($"https://www.openstreetmap.org/?mlat={lat}&mlon={lon}#map=18/{lat}/{lon}");
        }

        sb.AppendLine();
        sb.AppendLine($"Reported {r.CreatedAt:dd MMM yyyy, HH:mm} with FixIt");
        return sb.ToString();
    }
}
