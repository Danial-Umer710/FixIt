namespace FixIt.Services;

/// <summary>Camera / gallery access. Photos are copied into the app's private data folder.</summary>
public class PhotoService
{
    private readonly IMediaPicker _mediaPicker;

    public PhotoService(IMediaPicker mediaPicker) => _mediaPicker = mediaPicker;

    private static string PhotoFolder => Path.Combine(FileSystem.AppDataDirectory, "photos");

    public bool IsCaptureSupported => _mediaPicker.IsCaptureSupported;

    /// <returns>Path of the stored photo, or null if the user cancelled.</returns>
    public async Task<string?> CapturePhotoAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
            status = await Permissions.RequestAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
            throw new PermissionException("Camera permission was denied.");

        var photo = await _mediaPicker.CapturePhotoAsync(new MediaPickerOptions { Title = "Photo of the issue" });
        return photo is null ? null : await SaveLocalCopyAsync(photo);
    }

    /// <returns>Path of the stored photo, or null if the user cancelled.</returns>
    public async Task<string?> PickPhotoAsync()
    {
        var photos = await _mediaPicker.PickPhotosAsync(new MediaPickerOptions
        {
            Title = "Choose a photo",
            SelectionLimit = 1
        });
        var photo = photos?.FirstOrDefault();
        return photo is null ? null : await SaveLocalCopyAsync(photo);
    }

    /// <summary>Deletes a photo that belongs to the app (never touches files outside the app folder).</summary>
    public void DeletePhoto(string? path)
    {
        try
        {
            if (!string.IsNullOrEmpty(path)
                && path.StartsWith(PhotoFolder, StringComparison.OrdinalIgnoreCase)
                && File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Could not delete photo: {ex}");
        }
    }

    private static async Task<string> SaveLocalCopyAsync(FileResult photo)
    {
        Directory.CreateDirectory(PhotoFolder);

        var extension = Path.GetExtension(photo.FileName);
        if (string.IsNullOrEmpty(extension)) extension = ".jpg";

        var targetPath = Path.Combine(PhotoFolder, $"{Guid.NewGuid():N}{extension}");

        await using var source = await photo.OpenReadAsync();
        await using var target = File.Create(targetPath);
        await source.CopyToAsync(target);

        return targetPath;
    }
}
