namespace FixIt.Models;

/// <summary>Display texts, icons and colours for report values (shared by converters, map and sharing).</summary>
public static class ReportText
{
    public static string Category(IssueCategory category) => category.ToString();

    public static string CategoryIcon(IssueCategory category) => category switch
    {
        IssueCategory.Pothole => "🕳️",
        IssueCategory.Lighting => "💡",
        IssueCategory.Litter => "🗑️",
        IssueCategory.Graffiti => "🎨",
        IssueCategory.Vandalism => "🔨",
        IssueCategory.Traffic => "🚦",
        _ => "📍"
    };

    public static string Status(IssueStatus status) => status switch
    {
        IssueStatus.Open => "Open",
        IssueStatus.InProgress => "In progress",
        IssueStatus.Fixed => "Fixed",
        _ => status.ToString()
    };

    public static string StatusColorHex(IssueStatus status) => status switch
    {
        IssueStatus.Open => "#E03131",
        IssueStatus.InProgress => "#F08C00",
        IssueStatus.Fixed => "#2F9E44",
        _ => "#868E96"
    };

    public static string Severity(int severity) => severity switch
    {
        <= 1 => "Low",
        >= 3 => "High",
        _ => "Medium"
    };
}
