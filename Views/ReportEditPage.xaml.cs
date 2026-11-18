using FixIt.ViewModels;

namespace FixIt.Views;

public partial class ReportEditPage : ContentPage
{
    private readonly ReportEditViewModel _viewModel;

    public ReportEditPage(ReportEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);
        // Leaving without saving (e.g. hardware back button) must not leave orphan photo files.
        _viewModel.DiscardUnsavedPhoto();
    }
}
