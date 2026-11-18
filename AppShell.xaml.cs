using FixIt.Views;

namespace FixIt;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(Routes.ReportEdit, typeof(ReportEditPage));
        Routing.RegisterRoute(Routes.ReportDetail, typeof(ReportDetailPage));
    }
}
