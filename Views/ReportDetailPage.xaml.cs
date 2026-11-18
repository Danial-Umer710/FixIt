using FixIt.ViewModels;

namespace FixIt.Views;

public partial class ReportDetailPage : ContentPage
{
    private readonly ReportDetailViewModel _viewModel;

    public ReportDetailPage(ReportDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Reload when coming back from the edit page
        _viewModel.LoadCommand.Execute(null);
    }
}
