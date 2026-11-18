# FixIt – Neighbourhood Issue Reporter (.NET MAUI)

Report potholes, broken streetlights, litter, graffiti and other problems in your neighbourhood.
Each report gets a photo, a GPS position on a map, a severity and a status (Open → In progress → Fixed).
Reports work fully offline; street addresses are filled in automatically once the device reconnects.

## Requirements checklist

| Requirement | Where |
|---|---|
| MVVM | `Models/`, `ViewModels/`, `Views/` – CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]`) |
| ≥ 3 interactive pages | `ReportsPage` (list/search/filter/delete), `ReportEditPage` (create/edit), `ReportDetailPage` (status/share/maps/delete), `MapPage` (map, pins, my location) |
| Persistent storage | SQLite via `sqlite-net-pcl` – `Services/DatabaseService.cs` |
| Full CRUD | `IssueReport`: Create & Update (edit page), Read (list/detail/map), Delete (swipe on list / button on detail) |
| Device feature 1 – Camera | Take or pick a photo of the issue, stored with the report – `Services/PhotoService.cs` |
| Device feature 2 – Geolocation + map | GPS position of the issue, reverse-geocoded address, all reports on a map, open in native Maps – `Services/LocationService.cs`, `MapPage` |
| Device feature 3 – Connectivity | Offline banner, offline reports queued for address lookup and synced on reconnect – `Services/AddressSyncService.cs` |
| Device feature 4 – Sharing | Share a report as text (with map link) or share its photo – `ReportDetailViewModel.ShareAsync` |
| Data binding, converters, ObservableCollection | `Converters/ReportConverters.cs` (8 converters), `ReportsViewModel.Reports` |

## Building

Requires the .NET 10 SDK with the MAUI workload.

```bash
dotnet workload install maui-android maui-windows
```

Run on Windows:

```bash
dotnet build -t:Run -f net10.0-windows10.0.19041.0
```

Run on Android (emulator or device; first time install the Android SDK/JDK):

```bash
dotnet build -t:InstallAndroidDependencies -f net10.0-android "-p:AndroidSdkDirectory=C:\Android\sdk" "-p:JavaSdkDirectory=C:\Android\jdk" "-p:AcceptAndroidSDKLicenses=True"
```

```bash
dotnet build -t:Run -f net10.0-android
```

Or simply open `FixIt.csproj` in Visual Studio 2022/2026 (with the ".NET Multi-platform App UI development" workload) and press F5.

> Tip: the Android emulator lets you set a fake GPS position (… → Location) and toggle Wi-Fi/mobile data to demo the offline behaviour.

## Third-party components

- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) – MIT
- [sqlite-net-pcl](https://github.com/praeclarum/sqlite-net) – MIT
- [Leaflet](https://leafletjs.com/) 1.9.4 (loaded from unpkg CDN) – BSD-2-Clause
- Map tiles © [Esri](https://www.esri.com/) World Street Map (sources: Esri, HERE, Garmin, © [OpenStreetMap](https://www.openstreetmap.org/copyright) contributors)
