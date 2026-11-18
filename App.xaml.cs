using FixIt.Services;

namespace FixIt;

public partial class App : Application
{
    private readonly AppShell _shell;

    public App(AppShell shell, AddressSyncService addressSync)
    {
        InitializeComponent();
        _shell = shell;

        // Watches connectivity and fills in addresses for reports created offline.
        addressSync.Start();
    }

    protected override Window CreateWindow(IActivationState? activationState)
        => new(_shell) { Title = "FixIt" };
}
