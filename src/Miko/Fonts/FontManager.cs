using System.Collections.Concurrent;
using Miko.Common;
using Miko.Utils;
using SkiaSharp;

namespace Miko.Fonts;

/// <summary>
/// Central font management system for Miko rendering engine.
/// Handles font registration, lookup, caching, and fallback chains.
/// </summary>
public class FontManager : IDisposable
{
    private static FontManager? _instance;
    private static readonly object _instanceLock = new();

    /// <summary>
    /// Gets the singleton instance of FontManager
    /// </summary>
    public static FontManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_instanceLock)
                {
                    _instance ??= new FontManager();
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// Resets the singleton instance (useful for testing)
    /// </summary>
    public static void ResetInstance()
    {
        lock (_instanceLock)
        {
            _instance?.Dispose();
            _instance = null;
        }
    }

    private readonly ConcurrentDictionary<string, List<RegisteredFont>> _fontFamilies = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, SKTypeface> _typefaceCache = new();
    // 字形包含查询缓存：typeface 在 FontManager 内长生命周期、引用稳定，可安全作为 key。
    private static readonly ConcurrentDictionary<(SKTypeface Typeface, int Codepoint), bool> _glyphCache = new();
    private readonly Woff2Decoder _woff2Decoder = new();
    private readonly object _registrationLock = new();
    private bool _disposed;

    /// <summary>
    /// Default fallback fonts for different scripts
    /// </summary>
    public List<string> DefaultFallbackChain { get; } = new()
    {
        "Arial",
        "Segoe UI",
        "Microsoft YaHei",  // Chinese
        "SimSun",           // Chinese
        "MS Gothic",        // Japanese
        "Malgun Gothic",    // Korean
        "sans-serif"
    };

    public FontManager()
    {
    }

    /// <summary>
    /// Register a font from file path
    /// </summary>
    public void RegisterFont(string familyName, string filePath)
    {
        if (string.IsNullOrWhiteSpace(familyName))
            throw new ArgumentException("Family name cannot be empty", nameof(familyName));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Font file not found: {filePath}");

        byte[] fontData = File.ReadAllBytes(filePath);
        RegisterFont(familyName, fontData);
    }

    /// <summary>
    /// Register a font from byte array
    /// </summary>
    public void RegisterFont(string familyName, byte[] fontData)
    {
        if (string.IsNullOrWhiteSpace(familyName))
            throw new ArgumentException("Family name cannot be empty", nameof(familyName));

        if (fontData == null || fontData.Length == 0)
            throw new ArgumentException("Font data cannot be empty", nameof(fontData));

        var format = DetectFontFormat(fontData);

        // Convert WOFF2 to TTF
        byte[] processedData = fontData;
        if (format == FontFormat.WOFF2)
        {
            processedData = _woff2Decoder.Decode(fontData);
            format = FontFormat.TTF;
        }
        else if (format == FontFormat.WOFF)
        {
            processedData = DecodeWoff(fontData);
            format = FontFormat.TTF;
        }

        var registeredFont = new RegisteredFont(familyName, processedData, format);

        lock (_registrationLock)
        {
            if (!_fontFamilies.TryGetValue(familyName, out var fonts))
            {
                fonts = new List<RegisteredFont>();
                _fontFamilies[familyName] = fonts;
            }
            fonts.Add(registeredFont);
        }

        // Clear cache for this family
        ClearCacheForFamily(familyName);
    }

    /// <summary>
    /// Register a font from stream
    /// </summary>
    public void RegisterFont(string familyName, Stream fontStream)
    {
        using var ms = new MemoryStream();
        fontStream.CopyTo(ms);
        RegisterFont(familyName, ms.ToArray());
    }

    /// <summary>
    /// Get typeface for a font family with specified weight and style
    /// </summary>
    public SKTypeface? GetTypeface(string fontFamily, FontWeight weight, SKFontStyleSlant slant = SKFontStyleSlant.Upright)
    {
        var cacheKey = $"{fontFamily}|{(int)weight}|{slant}";

        if (_typefaceCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        SKTypeface? typeface = null;

        // Try registered fonts first
        if (_fontFamilies.TryGetValue(fontFamily, out var fonts))
        {
            // Find best match by weight
            var bestMatch = fonts
                .OrderBy(f => Math.Abs((int)f.Weight - (int)weight))
                .FirstOrDefault();

            if (bestMatch != null)
            {
                typeface = bestMatch.Typeface;
            }
        }

        // Fall back to system font
        typeface ??= SKTypeface.FromFamilyName(
            fontFamily,
            (SKFontStyleWeight)(int)weight,
            SKFontStyleWidth.Normal,
            slant);

        if (typeface != null)
        {
            _typefaceCache[cacheKey] = typeface;
        }

        return typeface;
    }

    /// <summary>
    /// Get typeface for rendering a specific character with fallback support
    /// </summary>
    public SKTypeface? GetTypefaceForCharacter(char character, string fontFamily, FontWeight weight)
    {
        var families = ParseFontFamilyChain(fontFamily);

        foreach (var family in families)
        {
            var typeface = GetTypeface(family, weight);
            if (typeface != null && ContainsGlyph(typeface, character))
            {
                return typeface;
            }
        }

        // Try default fallback chain
        foreach (var family in DefaultFallbackChain)
        {
            var typeface = GetTypeface(family, weight);
            if (typeface != null && ContainsGlyph(typeface, character))
            {
                return typeface;
            }
        }

        // Return default typeface even if it doesn't have the glyph
        return GetTypeface(families.FirstOrDefault() ?? "Arial", weight);
    }

    /// <summary>
    /// Parse font family string into ordered list
    /// </summary>
    public List<string> ParseFontFamilyChain(string fontFamilyString)
    {
        if (string.IsNullOrWhiteSpace(fontFamilyString))
        {
            return new List<string> { "Arial" };
        }

        return fontFamilyString
            .Split(',')
            .Select(f => f.Trim().Trim('"', '\''))
            .Where(f => !string.IsNullOrEmpty(f))
            .ToList();
    }

    /// <summary>
    /// Check if a font family is registered
    /// </summary>
    public bool IsFontRegistered(string familyName)
    {
        return _fontFamilies.ContainsKey(familyName);
    }

    /// <summary>
    /// Clear all cached typefaces
    /// </summary>
    public void ClearCache()
    {
        _typefaceCache.Clear();
        _glyphCache.Clear();
        // 字体变化使旧的文本测量结果失效
        TextMeasurer.ClearCache();
    }

    /// <summary>
    /// Unregister a font family
    /// </summary>
    public void UnregisterFont(string familyName)
    {
        lock (_registrationLock)
        {
            if (_fontFamilies.TryRemove(familyName, out var fonts))
            {
                foreach (var font in fonts)
                {
                    font.Dispose();
                }
            }
        }
        ClearCacheForFamily(familyName);
    }

    /// <summary>
    /// Check if a typeface contains a glyph for the specified character
    /// </summary>
    public static bool ContainsGlyph(SKTypeface typeface, char character)
    {
        return ContainsGlyph(typeface, (int)character);
    }

    /// <summary>
    /// Check if a typeface contains a glyph for the specified codepoint
    /// </summary>
    public static bool ContainsGlyph(SKTypeface typeface, int codepoint)
    {
        var key = (typeface, codepoint);
        if (_glyphCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        using var font = new SKFont(typeface);
        bool contains = font.GetGlyph(codepoint) != 0;
        _glyphCache[key] = contains;
        return contains;
    }

    private void ClearCacheForFamily(string familyName)
    {
        var keysToRemove = _typefaceCache.Keys
            .Where(k => k.StartsWith(familyName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _typefaceCache.TryRemove(key, out _);
        }

        // 字体注册/注销会改变回退链的解析结果，使旧的字形/测量缓存失效。
        _glyphCache.Clear();
        TextMeasurer.ClearCache();
    }

    private static FontFormat DetectFontFormat(byte[] data)
    {
        if (data.Length < 4)
            return FontFormat.Unknown;

        // WOFF2: 'wOF2' (0x774F4632)
        if (data[0] == 0x77 && data[1] == 0x4F && data[2] == 0x46 && data[3] == 0x32)
            return FontFormat.WOFF2;

        // WOFF: 'wOFF' (0x774F4646)
        if (data[0] == 0x77 && data[1] == 0x4F && data[2] == 0x46 && data[3] == 0x46)
            return FontFormat.WOFF;

        // OTF: 'OTTO'
        if (data[0] == 0x4F && data[1] == 0x54 && data[2] == 0x54 && data[3] == 0x4F)
            return FontFormat.OTF;

        // TTF: 0x00010000 or 'true'
        if ((data[0] == 0x00 && data[1] == 0x01 && data[2] == 0x00 && data[3] == 0x00) ||
            (data[0] == 0x74 && data[1] == 0x72 && data[2] == 0x75 && data[3] == 0x65))
            return FontFormat.TTF;

        return FontFormat.Unknown;
    }

    private static byte[] DecodeWoff(byte[] woffData)
    {
        // WOFF format is simpler than WOFF2 - tables are zlib compressed
        using var input = new MemoryStream(woffData);
        using var reader = new BinaryReader(input);

        // Read WOFF header
        uint signature = reader.ReadUInt32();
        if (signature != 0x774F4646 && signature != 0x46464F77) // 'wOFF' in big or little endian
        {
            // Try big-endian
            signature = ((signature & 0xFF) << 24) | ((signature & 0xFF00) << 8) |
                        ((signature >> 8) & 0xFF00) | ((signature >> 24) & 0xFF);
            if (signature != 0x774F4646)
                throw new InvalidDataException("Invalid WOFF signature");
        }

        uint flavor = ReadBigEndianUInt32(reader);
        uint length = ReadBigEndianUInt32(reader);
        ushort numTables = ReadBigEndianUInt16(reader);
        ushort reserved = ReadBigEndianUInt16(reader);
        uint totalSfntSize = ReadBigEndianUInt32(reader);
        ushort majorVersion = ReadBigEndianUInt16(reader);
        ushort minorVersion = ReadBigEndianUInt16(reader);
        uint metaOffset = ReadBigEndianUInt32(reader);
        uint metaLength = ReadBigEndianUInt32(reader);
        uint metaOrigLength = ReadBigEndianUInt32(reader);
        uint privOffset = ReadBigEndianUInt32(reader);
        uint privLength = ReadBigEndianUInt32(reader);

        // Read table directory
        var tables = new List<WoffTableEntry>();
        for (int i = 0; i < numTables; i++)
        {
            var entry = new WoffTableEntry
            {
                Tag = ReadBigEndianUInt32(reader),
                Offset = ReadBigEndianUInt32(reader),
                CompLength = ReadBigEndianUInt32(reader),
                OrigLength = ReadBigEndianUInt32(reader),
                OrigChecksum = ReadBigEndianUInt32(reader)
            };
            tables.Add(entry);
        }

        // Build output TTF/OTF
        using var output = new MemoryStream();
        using var writer = new BinaryWriter(output);

        // Write sfnt header
        WriteBigEndianUInt32(writer, flavor);
        WriteBigEndianUInt16(writer, numTables);

        // Calculate search range, entry selector, range shift
        int searchRange = 1;
        int entrySelector = 0;
        while (searchRange * 2 <= numTables)
        {
            searchRange *= 2;
            entrySelector++;
        }
        searchRange *= 16;
        int rangeShift = numTables * 16 - searchRange;

        WriteBigEndianUInt16(writer, (ushort)searchRange);
        WriteBigEndianUInt16(writer, (ushort)entrySelector);
        WriteBigEndianUInt16(writer, (ushort)rangeShift);

        // Calculate table offsets in output
        uint currentOffset = (uint)(12 + numTables * 16);
        var outputOffsets = new List<uint>();
        foreach (var table in tables)
        {
            outputOffsets.Add(currentOffset);
            currentOffset += table.OrigLength;
            // Align to 4 bytes
            currentOffset = (currentOffset + 3) & ~3u;
        }

        // Write table directory
        for (int i = 0; i < tables.Count; i++)
        {
            WriteBigEndianUInt32(writer, tables[i].Tag);
            WriteBigEndianUInt32(writer, tables[i].OrigChecksum);
            WriteBigEndianUInt32(writer, outputOffsets[i]);
            WriteBigEndianUInt32(writer, tables[i].OrigLength);
        }

        // Write table data
        for (int i = 0; i < tables.Count; i++)
        {
            var table = tables[i];
            input.Position = table.Offset;

            byte[] tableData;
            if (table.CompLength == table.OrigLength)
            {
                // Not compressed
                tableData = reader.ReadBytes((int)table.OrigLength);
            }
            else
            {
                // Zlib compressed (skip 2-byte zlib header)
                byte[] compressedData = reader.ReadBytes((int)table.CompLength);
                tableData = DecompressZlib(compressedData, (int)table.OrigLength);
            }

            writer.Write(tableData);

            // Pad to 4-byte boundary
            int padding = (4 - (int)(tableData.Length % 4)) % 4;
            for (int p = 0; p < padding; p++)
            {
                writer.Write((byte)0);
            }
        }

        return output.ToArray();
    }

    private static byte[] DecompressZlib(byte[] data, int expectedSize)
    {
        // Skip zlib header (2 bytes) and use DeflateStream
        using var input = new MemoryStream(data, 2, data.Length - 2);
        using var deflate = new System.IO.Compression.DeflateStream(input, System.IO.Compression.CompressionMode.Decompress);
        using var output = new MemoryStream(expectedSize);
        deflate.CopyTo(output);
        return output.ToArray();
    }

    private static uint ReadBigEndianUInt32(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(4);
        return (uint)((bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3]);
    }

    private static ushort ReadBigEndianUInt16(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(2);
        return (ushort)((bytes[0] << 8) | bytes[1]);
    }

    private static void WriteBigEndianUInt32(BinaryWriter writer, uint value)
    {
        writer.Write((byte)((value >> 24) & 0xFF));
        writer.Write((byte)((value >> 16) & 0xFF));
        writer.Write((byte)((value >> 8) & 0xFF));
        writer.Write((byte)(value & 0xFF));
    }

    private static void WriteBigEndianUInt16(BinaryWriter writer, ushort value)
    {
        writer.Write((byte)((value >> 8) & 0xFF));
        writer.Write((byte)(value & 0xFF));
    }

    private class WoffTableEntry
    {
        public uint Tag { get; set; }
        public uint Offset { get; set; }
        public uint CompLength { get; set; }
        public uint OrigLength { get; set; }
        public uint OrigChecksum { get; set; }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var fonts in _fontFamilies.Values)
            {
                foreach (var font in fonts)
                {
                    font.Dispose();
                }
            }
            _fontFamilies.Clear();
            _typefaceCache.Clear();
            _glyphCache.Clear();
            TextMeasurer.ClearCache();
            _disposed = true;
        }
    }
}
