using CommunityToolkit.Mvvm.ComponentModel;

namespace FixIt.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; }

    public bool IsNotBusy => !IsBusy;

    protected BaseViewModel()
    {
        Title = string.Empty;
    }
}
