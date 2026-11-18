namespace FixIt;

/// <summary>Shell route names, so ViewModels navigate without referencing Views.</summary>
public static class Routes
{
    public const string ReportEdit = "reportedit";
    public const string ReportDetail = "reportdetail";

    /// <summary>Query parameter carrying the report id.</summary>
    public const string IdParameter = "id";
}
