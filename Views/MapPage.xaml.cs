using FixIt.ViewModels;

namespace FixIt.Views;

public partial class MapPage : ContentPage
{
    private readonly MapViewModel _viewModel;

    public MapPage(MapViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadCommand.Execute(null);
    }

    // Pins in the web map link to https://fixit.local/report/{id}.
    // Intercept that navigation and open the report page instead.
    private void OnMapNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (_viewModel.TryHandleNavigation(e.Url))
            e.Cancel = true;
    }
}
