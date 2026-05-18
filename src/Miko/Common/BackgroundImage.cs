using SkiaSharp;
using System.Reflection;

namespace Miko.Common;

public class BackgroundImage
{
    public SKBitmap? Bitmap { get; private set; }

    private BackgroundImage() { }

    public static BackgroundImage FromFile(string path)
    {
        var image = new BackgroundImage();
        using var stream = System.IO.File.OpenRead(path);
        image.Bitmap = SKBitmap.Decode(stream);
        return image;
    }

    public static BackgroundImage FromBase64(string base64)
    {
        var image = new BackgroundImage();
        var bytes = Convert.FromBase64String(base64);
        image.Bitmap = SKBitmap.Decode(bytes);
        return image;
    }

    public static BackgroundImage FromResource(Assembly assembly, string resourceName)
    {
        var image = new BackgroundImage();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new InvalidOperationException($"Resource '{resourceName}' not found in assembly '{assembly.FullName}'.");
        image.Bitmap = SKBitmap.Decode(stream);
        return image;
    }

    public static BackgroundImage FromStream(Stream stream)
    {
        var image = new BackgroundImage();
        image.Bitmap = SKBitmap.Decode(stream);
        return image;
    }

    public static BackgroundImage FromBytes(byte[] data)
    {
        var image = new BackgroundImage();
        image.Bitmap = SKBitmap.Decode(data);
        return image;
    }

    public static BackgroundImage FromBitmap(SKBitmap bitmap)
    {
        return new BackgroundImage { Bitmap = bitmap };
    }
}
