using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FixIt.Models;
using FixIt.Services;

namespace FixIt.ViewModels;

/// <summary>Create a new report or edit an existing one (title, category, photo, GPS location...).</summary>
public partial class ReportEditViewModel : BaseViewModel, IQueryAttributable
{
    private readonly DatabaseService _database;
    private readonly PhotoService _photos;
    private readonly LocationService _location;
    private readonly AddressSyncService _addressSync;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;
    private readonly IConnectivity _connectivity;

    private IssueReport? _existing;
    private string? _originalPhotoPath;

    public ReportEditViewModel(
        DatabaseService database,
        PhotoService photos,
        LocationService location,
        AddressSyncService addressSync,
        INavigationService navigation,
        IDialogService dialogs,
        IConnectivity connectivity)
    {
        _database = database;
        _photos = photos;
        _location = location;
        _addressSync = addressSync;
        _navigation = navigation;
        _dialogs = dialogs;
        _connectivity = connectivity;

        Title = "New report";
        ReportTitle = string.Empty;
        Description = string.Empty;
        SelectedCategory = Categories[0];
        SelectedSeverity = Severities[1];
        SelectedStatus = Statuses[0];
    }

    public IReadOnlyList<PickerOption<IssueCategory>> Categories { get; } = Enum.GetValues<IssueCategory>()
        .Select(c => new PickerOption<IssueCategory>(c, $"{ReportText.CategoryIcon(c)}  {ReportText.Category(c)}"))
        .ToList();

    public IReadOnlyList<PickerOption<int>> Severities { get; } = new[] { 1, 2, 3 }
        .Select(s => new PickerOption<int>(s, ReportText.Severity(s)))
        .ToList();

    public IReadOnlyList<PickerOption<IssueStatus>> Statuses { get; } = Enum.GetValues<IssueStatus>()
        .Select(s => new PickerOption<IssueStatus>(s, ReportText.Status(s)))
        .ToList();

    [ObservableProperty]
    public partial string ReportTitle { get; set; }

    [ObservableProperty]
    public partial string Description { get; set; }

    [ObservableProperty]
    public partial PickerOption<IssueCategory>? SelectedCategory { get; set; }

    [ObservableProperty]
    public partial PickerOption<int>? SelectedSeverity { get; set; }

    [ObservableProperty]
    public partial PickerOption<IssueStatus>? SelectedStatus { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPhoto))]
    public partial string? PhotoPath { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLocation), nameof(LocationText))]
    public partial double? Latitude { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLocation), nameof(LocationText))]
    public partial double? Longitude { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LocationText))]
    public partial string? Address { get; set; }

    [ObservableProperty]
    public partial bool IsLocating { get; set; }

    [ObservableProperty]
    public partial string? ValidationMessage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SaveButtonText))]
    public partial bool IsEditMode { get; set; }

    public bool HasPhoto => !string.IsNullOrEmpty(PhotoPath);

    public bool HasLocation => Latitude.HasValue && Longitude.HasValue;

    public string LocationText
    {
        get
        {
            if (!HasLocation) return "No location attached yet";
            if (!string.IsNullOrWhiteSpace(Address)) return Address!;
            var coords = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{Latitude:F5}, {Longitude:F5}");
            return _connectivity.NetworkAccess == NetworkAccess.Internet
                ? coords
                : $"{coords}\nAddress will be added when you're back online";
        }
    }

    public string SaveButtonText => IsEditMode ? "Save changes" : "Submit report";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(Routes.IdParameter, out var value) && value is int id && id > 0)
            _ = LoadAsync(id);
    }

    private async Task LoadAsync(int id)
    {
        try
        {
            IsBusy = true;
            var report = await _database.GetReportAsync(id);
            if (report is null)
            {
                await _dialogs.ShowAlertAsync("Not found", "This report no longer exists.");
                await _navigation.GoBackAsync();
                return;
            }

            _existing = report;
            _originalPhotoPath = report.PhotoPath;
            IsEditMode = true;
            Title = "Edit report";

            ReportTitle = report.Title;
            Description = report.Description;
            SelectedCategory = Categories.FirstOrDefault(o => o.Value == report.Category) ?? Categories[0];
            SelectedSeverity = Severities.FirstOrDefault(o => o.Value == report.Severity) ?? Severities[1];
            SelectedStatus = Statuses.FirstOrDefault(o => o.Value == report.Status) ?? Statuses[0];
            PhotoPath = report.HasPhoto ? report.PhotoPath : null;
            Latitude = report.Latitude;
            Longitude = report.Longitude;
            Address = report.Address;
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

    // ---------- Camera ----------

    [RelayCommand]
    private async Task TakePhotoAsync()
    {
        try
        {
            if (!_photos.IsCaptureSupported)
            {
                await _dialogs.ShowAlertAsync("Camera unavailable",
                    "This device has no camera. Choose a photo from the gallery instead.");
                return;
            }

            var path = await _photos.CapturePhotoAsync();
            if (path is not null) ReplacePhoto(path);
        }
        catch (PermissionException)
        {
            await _dialogs.ShowAlertAsync("Permission needed",
                "Allow camera access in your device settings to take photos of issues.");
        }
        catch (Exception ex)
        {
            await _dialogs.ShowAlertAsync("Camera error",
                $"Could not take a photo. You can choose one from the gallery instead.\n{ex.Message}");
        }
    }

    [RelayCommand]
    private async Task PickPhotoAsync()
    {
        try
        {
            var path = await _photos.PickPhotoAsync();
            if (path is not null) ReplacePhoto(path);
        }
        catch (PermissionException)
        {
            await _dialogs.ShowAlertAsync("Permission needed", "Allow photo access in your device settings.");
        }
        catch (Exception ex)
        {
            await _dialogs.ShowAlertAsync("Error", $"Could not load the photo.\n{ex.Message}");
        }
    }

    [RelayCommand]
    private void RemovePhoto()
    {
        DeleteIfNew(PhotoPath);
        PhotoPath = null;
    }

    private void ReplacePhoto(string newPath)
    {
        DeleteIfNew(PhotoPath);
        PhotoPath = newPath;
    }

    /// <summary>Deletes a photo taken during this edit session (the original is only deleted after saving).</summary>
    private void DeleteIfNew(string? path)
    {
        if (path is not null && path != _originalPhotoPath)
            _photos.DeletePhoto(path);
    }

    // ---------- Geolocation ----------

    [RelayCommand]
    private async Task GetLocationAsync()
    {
        if (IsLocating) return;

        try
        {
            IsLocating = true;
            var location = await _location.GetCurrentLocationAsync();
            if (location is null)
            {
                await _dialogs.ShowAlertAsync("Location", "Could not determine your location. Please try again.");
                return;
            }

            Latitude = location.Latitude;
            Longitude = location.Longitude;
            Address = null;

            if (_connectivity.NetworkAccess == NetworkAccess.Internet)
                Address = await _location.GetAddressAsync(location.Latitude, location.Longitude);
        }
        catch (PermissionException)
        {
            await _dialogs.ShowAlertAsync("Permission needed",
                "Allow location access so FixIt can pin the issue on the map.");
        }
        catch (FeatureNotEnabledException)
        {
            await _dialogs.ShowAlertAsync("Location is off", "Turn on location services and try again.");
        }
        catch (FeatureNotSupportedException)
        {
            await _dialogs.ShowAlertAsync("Not supported", "This device does not support geolocation.");
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

    [RelayCommand]
    private void ClearLocation()
    {
        Latitude = null;
        Longitude = null;
        Address = null;
    }

    // ---------- Save / Cancel ----------

    [RelayCommand]
    private async Task SaveAsync()
    {
        ValidationMessage = null;

        var title = ReportTitle?.Trim() ?? string.Empty;
        if (title.Length == 0)
        {
            ValidationMessage = "Please give the report a short title.";
            return;
        }
        if (title.Length < 3)
        {
            ValidationMessage = "The title should be at least 3 characters long.";
            return;
        }

        var report = _existing ?? new IssueReport();
        report.Title = title;
        report.Description = Description?.Trim() ?? string.Empty;
        report.Category = SelectedCategory?.Value ?? IssueCategory.Other;
        report.Severity = SelectedSeverity?.Value ?? 2;
        report.Status = IsEditMode ? SelectedStatus?.Value ?? IssueStatus.Open : IssueStatus.Open;
        report.PhotoPath = PhotoPath;
        report.Latitude = Latitude;
        report.Longitude = Longitude;
        report.Address = Address;
        report.AddressPending = HasLocation && string.IsNullOrWhiteSpace(Address);

        try
        {
            IsBusy = true;
            await _database.SaveReportAsync(report);

            // The old photo was replaced or removed – clean it up now that the change is saved.
            if (_originalPhotoPath is not null && _originalPhotoPath != PhotoPath)
                _photos.DeletePhoto(_originalPhotoPath);
            _originalPhotoPath = PhotoPath;

            if (report.AddressPending)
                _ = _addressSync.SyncPendingAsync();

            await _navigation.GoBackAsync();
        }
        catch (Exception ex)
        {
            await _dialogs.ShowAlertAsync("Error", $"Could not save the report.\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        DiscardUnsavedPhoto();
        await _navigation.GoBackAsync();
    }

    /// <summary>
    /// Called when the page is left by any route (Cancel, toolbar back, hardware back):
    /// removes a photo taken in this session that was never saved. Safe to call twice.
    /// </summary>
    public void DiscardUnsavedPhoto()
    {
        if (PhotoPath is not null && PhotoPath != _originalPhotoPath)
        {
            _photos.DeletePhoto(PhotoPath);
            PhotoPath = null;
        }
    }
}
