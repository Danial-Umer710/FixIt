namespace FixIt.Services;

/// <summary>Lets ViewModels show alerts without depending on Pages.</summary>
public interface IDialogService
{
    Task ShowAlertAsync(string title, string message, string cancel = "OK");
    Task<bool> ConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No");
    Task<string?> ShowActionSheetAsync(string title, params string[] buttons);
}

public class DialogService : IDialogService
{
    private static Page? CurrentPage =>
        Shell.Current?.CurrentPage ?? Application.Current?.Windows.FirstOrDefault()?.Page;

    public Task ShowAlertAsync(string title, string message, string cancel = "OK")
        => CurrentPage?.DisplayAlertAsync(title, message, cancel) ?? Task.CompletedTask;

    public async Task<bool> ConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No")
        => CurrentPage is { } page && await page.DisplayAlertAsync(title, message, accept, cancel);

    public async Task<string?> ShowActionSheetAsync(string title, params string[] buttons)
        => CurrentPage is { } page ? await page.DisplayActionSheetAsync(title, "Cancel", null, buttons) : null;
}
