using System.Text.Json;
using FixIt.Models;

namespace FixIt.Services;

/// <summary>
/// Builds a Leaflet page (Esri World Street Map tiles) showing all reports as coloured pins.
/// Works on every platform without a Google/Bing Maps API key.
/// Pins link to <see cref="ReportLinkPrefix"/>{id}; the MapPage intercepts that navigation.
/// </summary>
public static class MapHtmlBuilder
{
    public const string BaseUrl = "https://fixit.local/";
    public const string ReportLinkPrefix = BaseUrl + "report/";

    // Default view when there is nothing to show (Budapest)
    private const double DefaultLat = 47.4979;
    private const double DefaultLon = 19.0402;

    public static string Build(IEnumerable<IssueReport> reports, Location? me)
    {
        var pins = reports
            .Where(r => r.HasLocation)
            .Select(r => new
            {
                id = r.Id,
                lat = r.Latitude!.Value,
                lon = r.Longitude!.Value,
                title = r.Title,
                status = ReportText.Status(r.Status),
                category = $"{ReportText.CategoryIcon(r.Category)} {ReportText.Category(r.Category)}",
                color = ReportText.StatusColorHex(r.Status)
            });

        // System.Text.Json escapes <, >, & and quotes, so user text is safe inside the script block.
        var pinsJson = JsonSerializer.Serialize(pins);
        var meJson = me is null ? "null" : JsonSerializer.Serialize(new { lat = me.Latitude, lon = me.Longitude });
        var defaultJson = JsonSerializer.Serialize(new[] { DefaultLat, DefaultLon });

        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0" />
              <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" />
              <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
              <style>
                html, body, #map { height: 100%; margin: 0; padding: 0; }
                body { font-family: -apple-system, 'Segoe UI', Roboto, sans-serif; }
                .popup-title { font-weight: 700; font-size: 15px; }
                .popup-meta { color: #555; margin: 2px 0 6px; }
                .leaflet-container a.popup-link { font-weight: 600; color: #E8590C; text-decoration: none; }
                #error { display: none; padding: 24px; text-align: center; color: #555; }
              </style>
            </head>
            <body>
              <div id="map"></div>
              <div id="error">The map could not be loaded. Check your internet connection.</div>
              <script>
                (function () {
                  if (typeof L === 'undefined') {
                    document.getElementById('map').style.display = 'none';
                    document.getElementById('error').style.display = 'block';
                    return;
                  }
                  var reports = {{pinsJson}};
                  var me = {{meJson}};
                  var map = L.map('map', { zoomControl: false });
                  L.control.zoom({ position: 'topright' }).addTo(map);
                  L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Street_Map/MapServer/tile/{z}/{y}/{x}', {
                    maxZoom: 19,
                    attribution: 'Tiles &copy; Esri &mdash; Source: Esri, HERE, Garmin, &copy; OpenStreetMap contributors, and the GIS User Community'
                  }).addTo(map);

                  var bounds = [];
                  reports.forEach(function (r) {
                    var marker = L.circleMarker([r.lat, r.lon], {
                      radius: 11, color: '#ffffff', weight: 3, fillColor: r.color, fillOpacity: 0.95
                    }).addTo(map);

                    // Build popup with textContent so user-entered text is never parsed as HTML
                    var div = document.createElement('div');
                    var t = document.createElement('div'); t.className = 'popup-title'; t.textContent = r.title;
                    var m = document.createElement('div'); m.className = 'popup-meta'; m.textContent = r.category + ' · ' + r.status;
                    var a = document.createElement('a'); a.className = 'popup-link';
                    a.href = '{{ReportLinkPrefix}}' + r.id; a.textContent = 'Open report ›';
                    div.appendChild(t); div.appendChild(m); div.appendChild(a);
                    marker.bindPopup(div);
                    bounds.push([r.lat, r.lon]);
                  });

                  if (me) {
                    L.circleMarker([me.lat, me.lon], {
                      radius: 9, color: '#1971C2', weight: 3, fillColor: '#4DABF7', fillOpacity: 0.8
                    }).addTo(map).bindPopup('You are here');
                    bounds.push([me.lat, me.lon]);
                  }

                  if (me) map.setView([me.lat, me.lon], 16);
                  else if (bounds.length === 1) map.setView(bounds[0], 16);
                  else if (bounds.length > 1) map.fitBounds(bounds, { padding: [40, 40], maxZoom: 17 });
                  else map.setView({{defaultJson}}, 12);
                })();
              </script>
            </body>
            </html>
            """;
    }
}
