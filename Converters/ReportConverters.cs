using System.Globalization;
using FixIt.Models;

namespace FixIt.Converters;

/// <summary>IssueStatus -> colour (red / orange / green).</summary>
public class StatusToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is IssueStatus status ? Color.FromArgb(ReportText.StatusColorHex(status)) : Colors.Gray;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>IssueStatus -> "Open" / "In progress" / "Fixed".</summary>
public class StatusToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is IssueStatus status ? ReportText.Status(status) : string.Empty;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>IssueCategory -> emoji icon.</summary>
public class CategoryToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is IssueCategory category ? ReportText.CategoryIcon(category) : "📍";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>IssueCategory -> "🕳️ Pothole".</summary>
public class CategoryToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is IssueCategory category
            ? $"{ReportText.CategoryIcon(category)} {ReportText.Category(category)}"
            : string.Empty;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Severity 1..3 -> "Low severity" / "Medium severity" / "High severity".</summary>
public class SeverityToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int severity ? $"{ReportText.Severity(severity)} severity" : string.Empty;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>DateTime -> "just now", "5 min ago", "yesterday", "12 Mar 2026".</summary>
public class RelativeTimeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTime time || time == default) return string.Empty;

        var diff = DateTime.Now - time;
        if (diff.TotalMinutes < 1) return "just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} min ago";
        if (diff.TotalHours < 24 && time.Date == DateTime.Today) return $"{(int)diff.TotalHours} h ago";
        if (time.Date == DateTime.Today.AddDays(-1)) return "yesterday";
        if (diff.TotalDays < 7) return $"{(int)Math.Ceiling(diff.TotalDays)} days ago";
        return time.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>bool -> !bool (both directions).</summary>
public class InverseBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : value;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : value;
}

/// <summary>True when a string has text (or any non-string value is not null).</summary>
public class IsNotNullOrEmptyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string s ? !string.IsNullOrWhiteSpace(s) : value is not null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
