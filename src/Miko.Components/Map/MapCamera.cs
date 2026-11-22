namespace Miko.Components.Map;

/// <summary>Longitude and latitude in the provider's coordinate system (GCJ-02 for AMap).</summary>
public readonly record struct MapPoint
{
    public const double MaxLatitude = 85.0511287798066;

    public MapPoint(double longitude, double latitude)
    {
        if (!double.IsFinite(longitude)) throw new ArgumentOutOfRangeException(nameof(longitude));
        if (!double.IsFinite(latitude)) throw new ArgumentOutOfRangeException(nameof(latitude));
        longitude %= 360;
        if (longitude >= 180) longitude -= 360;
        if (longitude < -180) longitude += 360;
        Longitude = longitude;
        Latitude = Math.Clamp(latitude, -MaxLatitude, MaxLatitude);
    }

    public double Longitude { get; }
    public double Latitude { get; }

    internal double MercatorY => (1 - Math.Asinh(Math.Tan(Latitude * Math.PI / 180)) / Math.PI) / 2;

    internal static MapPoint FromMercator(double longitude, double y) =>
        new(longitude, Math.Atan(Math.Sinh(Math.PI * (1 - 2 * Math.Clamp(y, 0, 1)))) * 180 / Math.PI);
}

/// <summary>A Web Mercator camera. Offsets are logical pixels relative to its center, positive down/right.</summary>
public sealed record MapCamera
{
    public const int MinZoom = 3;
    public const int MaxZoom = 18;
    public static MapCamera Beijing { get; } = new(new(116.397, 39.916), 14);

    public MapCamera(MapPoint center, int zoom)
    {
        Center = center;
        Zoom = Math.Clamp(zoom, MinZoom, MaxZoom);
    }

    public MapPoint Center { get; }
    public int Zoom { get; }
    internal int WorldSize => 256 << Zoom;

    public MapPoint PointAt(double x, double y) =>
        MapPoint.FromMercator(Center.Longitude + x * 360 / WorldSize, Center.MercatorY + y / WorldSize);

    public (double X, double Y) OffsetOf(MapPoint point)
    {
        double longitude = Math.IEEERemainder(point.Longitude - Center.Longitude, 360);
        return (longitude * WorldSize / 360, (point.MercatorY - Center.MercatorY) * WorldSize);
    }

    public MapCamera PanBy(double dx, double dy) => new(PointAt(-dx, -dy), Zoom);

    public MapCamera ZoomAt(int zoom, double x = 0, double y = 0)
    {
        zoom = Math.Clamp(zoom, MinZoom, MaxZoom);
        if (zoom == Zoom) return this;
        var anchor = new MapCamera(PointAt(x, y), zoom);
        return anchor.PanBy(x, y);
    }
}

public sealed record MapMarker(MapPoint Position, string? Label = null);

/// <summary>A centered, north-up image at 256 logical pixels per world at zoom zero.</summary>
public sealed record MapImageRequest
{
    public const int MaxSize = 1024;

    public MapImageRequest(MapCamera camera, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(camera);
        if (width is < 1 or > MaxSize) throw new ArgumentOutOfRangeException(nameof(width));
        if (height is < 1 or > MaxSize) throw new ArgumentOutOfRangeException(nameof(height));
        Camera = camera;
        Width = width;
        Height = height;
    }

    public MapCamera Camera { get; }
    public int Width { get; }
    public int Height { get; }

    // Prefer one frame. At the lowest level a large viewport may need up to four frames
    // to cover the 2048-pixel world without exceeding the static map service's image limit.
    internal static MapImageRequest[] CoverViewport(MapCamera camera, float width, float height)
    {
        int zoom = camera.Zoom;
        while (Math.Max(width, height) > MaxSize && zoom > MapCamera.MinZoom)
        {
            width /= 2;
            height /= 2;
            zoom--;
        }

        int world = 256 << zoom;
        int w = (int)Math.Clamp(Math.Ceiling(width), 1, world);
        int h = (int)Math.Clamp(Math.Ceiling(height), 1, world);
        double halfHeight = h / (2d * world);
        double cy = camera.Center.MercatorY;
        double y = Math.Clamp(cy, halfHeight, 1 - halfHeight);
        var center = y == cy ? camera.Center : MapPoint.FromMercator(camera.Center.Longitude, y);
        var frameCamera = new MapCamera(center, zoom);
        if (w <= MaxSize && h <= MaxSize) return [new(frameCamera, w, h)];

        int cols = (w + MaxSize - 1) / MaxSize;
        int rows = (h + MaxSize - 1) / MaxSize;
        var frames = new MapImageRequest[rows * cols];
        for (int row = 0; row < rows; row++)
        {
            int top = h * row / rows, bottom = h * (row + 1) / rows;
            for (int col = 0; col < cols; col++)
            {
                int left = w * col / cols, right = w * (col + 1) / cols;
                var origin = frameCamera.PointAt((left + right - w) / 2d, (top + bottom - h) / 2d);
                frames[row * cols + col] = new(new(origin, zoom), right - left, bottom - top);
            }
        }
        return frames;
    }
}
