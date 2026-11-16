using System.Text;

namespace Miko.MockServer;

/// <summary>Small RGB raster used to author the demo assets without a native graphics dependency.</summary>
public sealed class Raster(int width, int height)
{
    public int Width { get; } = width;
    public int Height { get; } = height;
    public byte[] Pixels { get; } = new byte[width * height * 3];

    public Raster Clone() { var copy = new Raster(Width, Height); Pixels.CopyTo(copy.Pixels, 0); return copy; }

    public void Fill(int x, int y, int w, int h, uint color)
    {
        for (int row = Math.Max(0, y); row < Math.Min(Height, y + h); row++)
            for (int col = Math.Max(0, x); col < Math.Min(Width, x + w); col++)
            {
                int offset = (row * Width + col) * 3;
                Pixels[offset] = (byte)(color >> 16); Pixels[offset + 1] = (byte)(color >> 8); Pixels[offset + 2] = (byte)color;
            }
    }

    public void Text(int x, int y, string text, uint color = 0xffffff, int scale = 2)
    {
        foreach (char c in text.ToUpperInvariant())
        {
            string glyph = Glyphs.GetValueOrDefault(c, c == ' ' ? "00000000000000000000000000000000000" : "11111100010001000100001000000000100");
            for (int i = 0; i < 35; i++) if (glyph[i] == '1') Fill(x + i % 5 * scale, y + i / 5 * scale, scale, scale, color);
            x += 6 * scale;
        }
    }

    public void Save(string path)
    {
        using var stream = File.Create(path);
        stream.Write(Encoding.ASCII.GetBytes($"P6\n{Width} {Height}\n255\n"));
        stream.Write(Pixels);
    }

    public static Raster Load(string path, int width, int height)
    {
        byte[] bytes = File.ReadAllBytes(path);
        byte[] header = Encoding.ASCII.GetBytes($"P6\n{width} {height}\n255\n");
        if (!bytes.AsSpan().StartsWith(header) || bytes.Length != header.Length + width * height * 3)
            throw new InvalidDataException("Unexpected PPM asset. Regenerate media using --generate-media.");
        var raster = new Raster(width, height);
        bytes.AsSpan(header.Length).CopyTo(raster.Pixels);
        return raster;
    }

    private static readonly Dictionary<char, string> Glyphs = new()
    {
        ['A']="01110100011000111111100011000110001", ['B']="11110100011000111110100011000111110",
        ['C']="01111100001000010000100001000001111", ['D']="11110100011000110001100011000111110",
        ['E']="11111100001000011110100001000011111", ['F']="11111100001000011110100001000010000",
        ['G']="01111100001000010111100011000101111", ['H']="10001100011000111111100011000110001",
        ['I']="11111001000010000100001000010011111", ['J']="00111000100001000010100101001001100",
        ['K']="10001100101010011000101001001010001", ['L']="10000100001000010000100001000011111",
        ['M']="10001110111010110101100011000110001", ['N']="10001110011010110011100011000110001",
        ['O']="01110100011000110001100011000101110", ['P']="11110100011000111110100001000010000",
        ['Q']="01110100011000110001101011001001101", ['R']="11110100011000111110101001001010001",
        ['S']="01111100001000001110000010000111110", ['T']="11111001000010000100001000010000100",
        ['U']="10001100011000110001100011000101110", ['V']="10001100011000110001100010101000100",
        ['W']="10001100011000110101101011101110001", ['X']="10001100010101000100010101000110001",
        ['Y']="10001100010101000100001000010000100", ['Z']="11111000010001000100010001000011111",
        ['0']="01110100011001110101110011000101110", ['1']="00100011000010000100001000010001110",
        ['2']="01110100010000100010001000100011111", ['3']="11110000010000101110000010000111110",
        ['4']="00010001100101010010111110001000010", ['5']="11111100001000011110000010000111110",
        ['6']="01110100001000011110100011000101110", ['7']="11111000010001000100010000100001000",
        ['8']="01110100011000101110100011000101110", ['9']="01110100011000101111000010000101110",
        ['-']="00000000000000011111000000000000000", [':']="00000001000010000000001000010000000",
        ['/']="00001000100001000100010000100010000", ['.']="00000000000000000000000000011000110"
    };
}
