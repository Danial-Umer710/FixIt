using FixIt.ViewModels;

namespace FixIt.Views;

public partial class ReportsPage : ContentPage
{
    private readonly ReportsViewModel _viewModel;

    public ReportsPage(ReportsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Reload every time so changes made on the edit/detail pages are shown
        _viewModel.LoadCommand.Execute(null);
    }
}
