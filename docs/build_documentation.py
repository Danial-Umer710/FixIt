"""Builds Documentation.pdf (2 pages) for the FixIt semester project.

Run from the project folder:  python docs/build_documentation.py
Edit AUTHOR below before submitting.
"""
from pathlib import Path

from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import cm
from reportlab.platypus import (Image, PageBreak, Paragraph, SimpleDocTemplate, Spacer, Table,
                                TableStyle)

AUTHOR = "Your Name"  # <-- put your name here

ROOT = Path(__file__).resolve().parent.parent
SHOTS = ROOT / "Screenshots"
OUT = ROOT / "Documentation.pdf"

ORANGE = colors.HexColor("#E8590C")
GREY = colors.HexColor("#495057")

ss = getSampleStyleSheet()
body = ParagraphStyle("body", parent=ss["BodyText"], fontSize=9.2, leading=12.2, spaceAfter=3)
small = ParagraphStyle("small", parent=body, fontSize=8, leading=10, textColor=GREY)
h1 = ParagraphStyle("h1", parent=ss["Title"], fontSize=19, leading=22, textColor=ORANGE, spaceAfter=2, alignment=0)
h2 = ParagraphStyle("h2", parent=ss["Heading2"], fontSize=11.5, leading=14, textColor=ORANGE, spaceBefore=7, spaceAfter=3)
cell = ParagraphStyle("cell", parent=body, fontSize=8.4, leading=10.4, spaceAfter=0)
cap = ParagraphStyle("cap", parent=small, alignment=TA_CENTER, fontSize=7.8, leading=9.5)


def bullets(items):
    return [Paragraph(f"&bull;&nbsp; {t}", body) for t in items]


def table(rows, widths):
    t = Table([[Paragraph(c, cell) for c in r] for r in rows], colWidths=widths, hAlign="LEFT")
    t.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#FFE8CC")),
        ("FONTNAME", (0, 0), (-1, 0), "Helvetica-Bold"),
        ("GRID", (0, 0), (-1, -1), 0.4, colors.HexColor("#DEE2E6")),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("TOPPADDING", (0, 0), (-1, -1), 2.5), ("BOTTOMPADDING", (0, 0), (-1, -1), 2.5),
    ]))
    return t


story = [
    Paragraph("FixIt &ndash; Neighbourhood Issue Reporter", h1),
    Paragraph(f".NET MAUI Semester Project &nbsp;&middot;&nbsp; {AUTHOR} &nbsp;&middot;&nbsp; "
              "Platforms: Android (primary), Windows", small),

    Paragraph("1. Purpose and functionality", h2),
    Paragraph("FixIt lets citizens report problems in public spaces &ndash; potholes, broken streetlights, "
              "litter, graffiti, vandalism or traffic issues. On the spot the user takes a photo, attaches the GPS "
              "position, picks a category and severity and adds a description. Every report has a status "
              "(<b>Open &rarr; In progress &rarr; Fixed</b>), so the app also tracks what has been reported to "
              "the municipality.", body),
    *bullets([
        "<b>Reports page</b> &ndash; statistics per status, full-text search, status filter, pull-to-refresh, swipe-to-delete.",
        "<b>Create / edit page</b> &ndash; title, category, severity, status, description, photo (camera or gallery), "
        "current GPS location with street address, input validation.",
        "<b>Detail page</b> &ndash; photo, one-tap status workflow, <i>Open in Maps</i>, share, edit, delete.",
        "<b>Map page</b> &ndash; all reports as pins coloured by status; a pin opens its report; shows the user's position.",
        "<b>Offline support</b> &ndash; works without internet; addresses of offline reports are resolved automatically on reconnect.",
    ]),

    Paragraph("2. Technologies and APIs", h2),
    table([
        ["Area", "Technology"],
        ["Framework", ".NET 10, .NET MAUI single project, Shell navigation (tabs + registered routes)"],
        ["Architecture", "MVVM with CommunityToolkit.Mvvm (source-generated [ObservableProperty] / [RelayCommand], "
                         "WeakReferenceMessenger); dependency injection in MauiProgram"],
        ["Persistence", "SQLite via sqlite-net-pcl (async API)"],
        ["MAUI Essentials", "MediaPicker, Geolocation, Geocoding, Connectivity, Share, Map, Permissions, FileSystem"],
        ["Map", "WebView hosting Leaflet with Esri World Street Map tiles (no API key)"],
        ["UI", "XAML with compiled bindings (x:DataType), 8 value converters, ObservableCollection, "
               "styles and light/dark theme"],
    ], [3.2 * cm, 13.8 * cm]),
    Spacer(1, 3),
    Paragraph("Folders: <b>Models</b> (entity, enums) &middot; <b>ViewModels</b> (logic and state) &middot; "
              "<b>Views</b> (XAML pages) &middot; <b>Services</b> (data access, device features) &middot; "
              "<b>Converters</b>. ViewModels never reference pages: navigation and dialogs go through "
              "INavigationService / IDialogService.", body),

    Paragraph("3. Data model", h2),
    Paragraph("Entity <b>IssueReport</b> (SQLite table IssueReports): Id (PK, auto-increment), Title (required, max 100), "
              "Description, Category (enum), Status (enum), Severity (1&ndash;3), PhotoPath, Latitude, Longitude, "
              "Address, AddressPending, CreatedAt, UpdatedAt.", body),
    Paragraph("<b>Full CRUD through the UI:</b> <i>Create</i> &ndash; the + button and form; <i>Read</i> &ndash; list, "
              "detail and map; <i>Update</i> &ndash; edit page and status buttons; <i>Delete</i> &ndash; swipe on the "
              "list or button on the detail page, both with confirmation (the photo file is removed too).", body),

    Paragraph("4. Device features", h2),
    *bullets([
        "<b>Camera</b> &ndash; photo of the issue (or gallery pick), copied to app storage and attached to the report; "
        "shown in list, detail and shareable.",
        "<b>Geolocation and map</b> &ndash; GPS position plus reverse-geocoded address; interactive map of all reports; "
        "<i>Open in Maps</i> for navigation.",
        "<b>Network connectivity</b> &ndash; offline banner; reports saved offline are marked AddressPending and "
        "AddressSyncService resolves them when ConnectivityChanged reports internet again.",
        "<b>Sharing</b> &ndash; report as formatted text with a map link, or its photo, via the system share sheet.",
    ]),
    Paragraph("Camera and location permissions are requested on first use; denials and missing hardware are "
              "handled with friendly messages instead of exceptions.", body),

    PageBreak(),
    Paragraph("5. Screenshots", h2),
    Paragraph("Android 15 emulator (Pixel 7). The camera photo is the emulator's virtual scene.", small),
    Spacer(1, 4),
]

shots = [
    ("1-report-list.png", "Reports list"),
    ("2-new-report-photo.png", "New report with photo"),
    ("3-report-detail.png", "Report detail"),
    ("4-issue-map.png", "Issue map with pin popup"),
    ("5-share.png", "Sharing a report"),
    ("6-empty-state.png", "Empty state"),
]
w, h = 4.4 * cm, 4.4 * cm * 2400 / 1080
cells = [[Image(str(SHOTS / f), width=w, height=h) for f, _ in shots[i:i + 3]] for i in (0, 3)]
caps = [[Paragraph(c, cap) for _, c in shots[i:i + 3]] for i in (0, 3)]
grid = Table([cells[0], caps[0], cells[1], caps[1]], colWidths=[5.7 * cm] * 3)
grid.setStyle(TableStyle([("ALIGN", (0, 0), (-1, -1), "CENTER"),
                          ("TOPPADDING", (0, 0), (-1, -1), 1), ("BOTTOMPADDING", (0, 0), (-1, -1), 3)]))
story += [grid, Spacer(1, 6),
          Paragraph("6. Third-party components", h2),
          Paragraph("CommunityToolkit.Mvvm (MIT) &middot; sqlite-net-pcl (MIT) &middot; Leaflet 1.9.4 (BSD-2-Clause) "
                    "&middot; Map tiles &copy; Esri World Street Map (sources: Esri, HERE, Garmin, &copy; OpenStreetMap "
                    "contributors) &middot; Fallback reverse geocoding: OpenStreetMap Nominatim.", body)]

SimpleDocTemplate(str(OUT), pagesize=A4, leftMargin=2 * cm, rightMargin=2 * cm, topMargin=1.5 * cm,
                  bottomMargin=1.4 * cm, title="FixIt - Project Documentation", author=AUTHOR).build(story)
print(f"wrote {OUT}")
