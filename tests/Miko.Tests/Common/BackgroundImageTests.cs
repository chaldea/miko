using Miko.Common;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Common;

public class BackgroundImageTests
{
    [Fact]
    public void FromBytes_ShouldCreateFromPngData()
    {
        var bitmap = CreateTestBitmap(10, 10);
        var pngData = bitmap.Encode(SKEncodedImageFormat.Png, 100).ToArray();

        var bg = BackgroundImage.FromBytes(pngData);

        bg.Bitmap.ShouldNotBeNull();
        bg.Bitmap.Width.ShouldBe(10);
        bg.Bitmap.Height.ShouldBe(10);
    }

    [Fact]
    public void FromBytes_ShouldCreateFromJpegData()
    {
        var bitmap = CreateTestBitmap(20, 15);
        var jpegData = bitmap.Encode(SKEncodedImageFormat.Jpeg, 90).ToArray();

        var bg = BackgroundImage.FromBytes(jpegData);

        bg.Bitmap.ShouldNotBeNull();
        bg.Bitmap.Width.ShouldBe(20);
        bg.Bitmap.Height.ShouldBe(15);
    }

    [Fact]
    public void FromBase64_ShouldDecodeAndCreateBitmap()
    {
        var bitmap = CreateTestBitmap(8, 8);
        var pngData = bitmap.Encode(SKEncodedImageFormat.Png, 100).ToArray();
        var base64 = Convert.ToBase64String(pngData);

        var bg = BackgroundImage.FromBase64(base64);

        bg.Bitmap.ShouldNotBeNull();
        bg.Bitmap.Width.ShouldBe(8);
        bg.Bitmap.Height.ShouldBe(8);
    }

    [Fact]
    public void FromStream_ShouldCreateFromStream()
    {
        var bitmap = CreateTestBitmap(12, 12);
        var pngData = bitmap.Encode(SKEncodedImageFormat.Png, 100).ToArray();
        using var stream = new MemoryStream(pngData);

        var bg = BackgroundImage.FromStream(stream);

        bg.Bitmap.ShouldNotBeNull();
        bg.Bitmap.Width.ShouldBe(12);
        bg.Bitmap.Height.ShouldBe(12);
    }

    [Fact]
    public void FromBitmap_ShouldWrapExistingBitmap()
    {
        var bitmap = CreateTestBitmap(5, 5);

        var bg = BackgroundImage.FromBitmap(bitmap);

        bg.Bitmap.ShouldBe(bitmap);
    }

    [Fact]
    public void Style_BackgroundImage_ShouldBeNullByDefault()
    {
        var style = new Style();
        style.BackgroundImage.ShouldBeNull();
    }

    [Fact]
    public void Style_BackgroundImage_ShouldMerge()
    {
        var bitmap = CreateTestBitmap(4, 4);
        var bg = BackgroundImage.FromBitmap(bitmap);

        var style1 = new Style();
        var style2 = new Style { BackgroundImage = bg };

        style1.Merge(style2);

        style1.BackgroundImage.ShouldBe(bg);
    }

    [Fact]
    public void Style_BackgroundImage_ShouldNotOverrideExisting()
    {
        var bitmap1 = CreateTestBitmap(4, 4);
        var bitmap2 = CreateTestBitmap(8, 8);
        var bg1 = BackgroundImage.FromBitmap(bitmap1);
        var bg2 = BackgroundImage.FromBitmap(bitmap2);

        var style1 = new Style { BackgroundImage = bg1 };
        var style2 = new Style { BackgroundImage = bg2 };

        style1.Merge(style2);

        style1.BackgroundImage.ShouldBe(bg1);
    }

    private static SKBitmap CreateTestBitmap(int width, int height)
    {
        var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Red);
        return bitmap;
    }
}
