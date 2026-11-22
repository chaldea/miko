namespace Miko.MockServer;

/// <summary>Crops the original Beijing drawing to the Web Mercator bounds of a requested frame.</summary>
internal sealed class MapFixture
{
    private const double ReferenceWorld = 256 << 14;
    private const double CenterLongitude = 116.397;
    private static readonly double CenterY = MercatorY(39.916);
    private readonly string _drawing;

    public MapFixture()
    {
        using var stream = typeof(MapFixture).Assembly.GetManifestResourceStream("Miko.Map.Beijing.svg")!;
        using var reader = new StreamReader(stream);
        _drawing = reader.ReadToEnd();
    }

    public IResult Render(double longitude, double latitude, int zoom, int width, int height)
    {
        if (!double.IsFinite(longitude) || longitude is < -180 or > 180 ||
            !double.IsFinite(latitude) || Math.Abs(latitude) > 85.05112878 ||
            zoom is < 3 or > 18 || width is < 1 or > 1024 || height is < 1 or > 1024)
            return Results.BadRequest("Expected Web Mercator coordinates, zoom 3–18 and dimensions 1–1024.");

        double scale = ReferenceWorld / (256 << zoom);
        double w = width * scale, h = height * scale;
        double x = 800 + Math.IEEERemainder(longitude - CenterLongitude, 360) * ReferenceWorld / 360 - w / 2;
        double y = 500 + (MercatorY(latitude) - CenterY) * ReferenceWorld - h / 2;
        string content = x >= 1600 || x + w <= 0 || y >= 1000 || y + h <= 0
            ? FormattableString.Invariant($"<text x='{x + w / 2}' y='{y + h / 2}' text-anchor='middle' font-family='sans-serif' font-size='{14 * scale}' fill='#616e67'>Outside the Beijing sample area</text>")
            : _drawing;

        // Width/height are the requested pixels. The viewBox selects that region of the reference drawing.
        string svg = FormattableString.Invariant($"""
            <svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="{x} {y} {w} {h}">
              <rect x="{x}" y="{y}" width="{w}" height="{h}" fill="#ecefe9"/>
              {content}
            </svg>
            """);
        return Results.Text(svg, "image/svg+xml");
    }

    private static double MercatorY(double latitude) => (1 - Math.Asinh(Math.Tan(latitude * Math.PI / 180)) / Math.PI) / 2;
}
