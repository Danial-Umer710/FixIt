using System.Globalization;
using SQLite;

namespace FixIt.Models;

/// <summary>
/// A neighbourhood problem reported by the user (pothole, broken light, litter...).
/// This is the entity with full CRUD support, persisted in SQLite.
/// </summary>
[Table("IssueReports")]
public class IssueReport
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull, MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    public IssueCategory Category { get; set; } = IssueCategory.Pothole;

    [Indexed]
    public IssueStatus Status { get; set; } = IssueStatus.Open;

    /// <summary>1 = Low, 2 = Medium, 3 = High.</summary>
    public int Severity { get; set; } = 2;

    /// <summary>Absolute path of the photo copied into the app's data folder.</summary>
    public string? PhotoPath { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    /// <summary>Human readable address from reverse geocoding (needs internet).</summary>
    public string? Address { get; set; }

    /// <summary>True when the report has coordinates but the address could not be resolved yet (e.g. offline).</summary>
    public bool AddressPending { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    [Ignore]
    public bool HasLocation => Latitude.HasValue && Longitude.HasValue;

    [Ignore]
    public bool HasPhoto => !string.IsNullOrEmpty(PhotoPath) && File.Exists(PhotoPath);

    [Ignore]
    public string Coordinates => HasLocation
        ? string.Create(CultureInfo.InvariantCulture, $"{Latitude:F5}, {Longitude:F5}")
        : string.Empty;

    [Ignore]
    public string LocationSummary
    {
        get
        {
            if (!HasLocation) return "No location attached";
            if (!string.IsNullOrWhiteSpace(Address)) return Address!;
            return AddressPending ? $"{Coordinates} · address pending" : Coordinates;
        }
    }
}
