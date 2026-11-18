namespace FixIt.Services;

/// <summary>GPS position and reverse geocoding (coordinates -> street address).</summary>
public class LocationService
{
    private readonly IGeolocation _geolocation;
    private readonly IGeocoding _geocoding;

    public LocationService(IGeolocation geolocation, IGeocoding geocoding)
    {
        _geolocation = geolocation;
        _geocoding = geocoding;
    }

    /// <exception cref="PermissionException">Location permission denied.</exception>
    /// <exception cref="FeatureNotEnabledException">Location services are switched off.</exception>
    /// <exception cref="FeatureNotSupportedException">Device has no location support.</exception>
    public async Task<Location?> GetCurrentLocationAsync(CancellationToken cancellationToken = default)
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
            throw new PermissionException("Location permission was denied.");

        var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
        var location = await _geolocation.GetLocationAsync(request, cancellationToken);
        return location ?? await _geolocation.GetLastKnownLocationAsync();
    }

    /// <summary>
    /// Returns a short address, or null when it cannot be resolved (offline, no result).
    /// Uses the platform geocoder first; falls back to OpenStreetMap Nominatim
    /// (e.g. on Windows, where the platform geocoder needs a Bing Maps key).
    /// </summary>
    public async Task<string?> GetAddressAsync(double latitude, double longitude)
        => await GetPlatformAddressAsync(latitude, longitude)
           ?? await GetNominatimAddressAsync(latitude, longitude);

    private async Task<string?> GetPlatformAddressAsync(double latitude, double longitude)
    {
        try
        {
            var placemarks = await _geocoding.GetPlacemarksAsync(latitude, longitude);
            var place = placemarks?.FirstOrDefault();
            if (place is null) return null;

            return FormatAddress(place.Thoroughfare, place.SubThoroughfare, place.Locality, place.CountryName);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Platform reverse geocoding failed: {ex.Message}");
            return null;
        }
    }

    private static readonly HttpClient Http = CreateHttpClient();

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        // Nominatim's usage policy requires an identifying User-Agent.
        client.DefaultRequestHeaders.UserAgent.ParseAdd("FixIt-StudentProject/1.0 (.NET MAUI)");
        return client;
    }

    private static async Task<string?> GetNominatimAddressAsync(double latitude, double longitude)
    {
        try
        {
            var lat = latitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
            var lon = longitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
            var url = $"https://nominatim.openstreetmap.org/reverse?format=jsonv2&zoom=18&addressdetails=1&lat={lat}&lon={lon}";

            using var doc = System.Text.Json.JsonDocument.Parse(await Http.GetStringAsync(url));
            if (!doc.RootElement.TryGetProperty("address", out var a)) return null;

            string? Get(params string[] keys)
            {
                foreach (var k in keys)
                    if (a.TryGetProperty(k, out var v) && v.GetString() is { Length: > 0 } s)
                        return s;
                return null;
            }

            return FormatAddress(
                Get("road", "pedestrian", "footway", "square"),
                Get("house_number"),
                Get("city", "town", "village", "suburb"),
                Get("country"));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Nominatim reverse geocoding failed: {ex.Message}");
            return null;
        }
    }

    private static string? FormatAddress(string? street, string? number, string? locality, string? country)
    {
        var streetPart = string.Join(" ", new[] { street, number }.Where(s => !string.IsNullOrWhiteSpace(s)));
        var address = string.Join(", ", new[] { streetPart, locality, country }
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct());
        return string.IsNullOrWhiteSpace(address) ? null : address;
    }
}
