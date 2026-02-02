using System.IO.Compression;

namespace Miko.Fonts;

/// <summary>
/// Decodes WOFF2 font format to TTF/OTF for SkiaSharp consumption.
/// WOFF2 uses Brotli compression and has a more complex table transformation.
/// Reference: https://www.w3.org/TR/WOFF2/
/// </summary>
public class Woff2Decoder
{
    // WOFF2 signature: 'wOF2' (0x774F4632)
    private const uint WOFF2_SIGNATURE = 0x774F4632;

    // Known table tags for WOFF2
    private static readonly uint[] KnownTags = new uint[]
    {
        0x636D6170, // cmap
        0x68656164, // head
        0x68686561, // hhea
        0x686D7478, // hmtx
        0x6D617870, // maxp
        0x6E616D65, // name
        0x4F532F32, // OS/2
        0x706F7374, // post
        0x63767420, // cvt
        0x6670676D, // fpgm
        0x676C7966, // glyf
        0x6C6F6361, // loca
        0x70726570, // prep
        0x43464620, // CFF
        0x564F5247, // VORG
        0x45424454, // EBDT
        0x45424C43, // EBLC
        0x67617370, // gasp
        0x68646D78, // hdmx
        0x6B65726E, // kern
        0x4C545348, // LTSH
        0x50434C54, // PCLT
        0x56444D58, // VDMX
        0x76686561, // vhea
        0x766D7478, // vmtx
        0x42415345, // BASE
        0x47444546, // GDEF
        0x47504F53, // GPOS
        0x47535542, // GSUB
        0x45425343, // EBSC
        0x4A535446, // JSTF
        0x4D415448, // MATH
        0x43424454, // CBDT
        0x43424C43, // CBLC
        0x434F4C52, // COLR
        0x4350414C, // CPAL
        0x53564720, // SVG
        0x7362697A, // sbix
        0x61636E74, // acnt
        0x61766172, // avar
        0x62646174, // bdat
        0x626C6F63, // bloc
        0x62736C6E, // bsln
        0x63766172, // cvar
        0x66646363, // fdsc
        0x666D7478, // fmtx
        0x66766172, // fvar
        0x67766172, // gvar
        0x68737479, // hsty
        0x6A757374, // just
        0x6C636172, // lcar
        0x6D6F7274, // mort
        0x6D6F7278, // morx
        0x6F706264, // opbd
        0x70726F70, // prop
        0x74726163, // trac
        0x5A617066, // Zapf
        0x53696C66, // Silf
        0x476C6174, // Glat
        0x476C6F63, // Gloc
        0x46656174, // Feat
        0x53696C6C, // Sill
    };

    /// <summary>
    /// Check if data is valid WOFF2 format
    /// </summary>
    public static bool IsWoff2(byte[] data)
    {
        if (data == null || data.Length < 4)
            return false;

        uint signature = (uint)((data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3]);
        return signature == WOFF2_SIGNATURE;
    }

    /// <summary>
    /// Check if stream contains valid WOFF2 format
    /// </summary>
    public static bool IsWoff2(Stream stream)
    {
        if (stream == null || !stream.CanRead)
            return false;

        long originalPosition = stream.Position;
        try
        {
            byte[] header = new byte[4];
            if (stream.Read(header, 0, 4) < 4)
                return false;

            return IsWoff2(header);
        }
        finally
        {
            stream.Position = originalPosition;
        }
    }

    /// <summary>
    /// Decode WOFF2 data to TTF format
    /// </summary>
    public byte[] Decode(byte[] woff2Data)
    {
        if (woff2Data == null || woff2Data.Length == 0)
            throw new ArgumentException("WOFF2 data cannot be empty", nameof(woff2Data));

        using var input = new MemoryStream(woff2Data);
        return Decode(input);
    }

    /// <summary>
    /// Decode WOFF2 stream to TTF format
    /// </summary>
    public byte[] Decode(Stream woff2Stream)
    {
        using var reader = new BinaryReader(woff2Stream);

        // Read and validate signature
        uint signature = ReadUInt32BE(reader);
        if (signature != WOFF2_SIGNATURE)
            throw new InvalidDataException($"Invalid WOFF2 signature: 0x{signature:X8}");

        // Read header
        uint flavor = ReadUInt32BE(reader);
        uint length = ReadUInt32BE(reader);
        ushort numTables = ReadUInt16BE(reader);
        ushort reserved = ReadUInt16BE(reader);
        uint totalSfntSize = ReadUInt32BE(reader);
        uint totalCompressedSize = ReadUInt32BE(reader);
        ushort majorVersion = ReadUInt16BE(reader);
        ushort minorVersion = ReadUInt16BE(reader);
        uint metaOffset = ReadUInt32BE(reader);
        uint metaLength = ReadUInt32BE(reader);
        uint metaOrigLength = ReadUInt32BE(reader);
        uint privOffset = ReadUInt32BE(reader);
        uint privLength = ReadUInt32BE(reader);

        // Read table directory
        var tables = new List<Woff2TableEntry>();
        for (int i = 0; i < numTables; i++)
        {
            var entry = ReadTableEntry(reader);
            tables.Add(entry);
        }

        // Calculate table sizes and offsets
        uint uncompressedOffset = 0;
        foreach (var table in tables)
        {
            table.UncompressedOffset = uncompressedOffset;
            uncompressedOffset += table.TransformLength;
        }

        // Read and decompress the compressed data
        byte[] compressedData = reader.ReadBytes((int)totalCompressedSize);
        byte[] decompressedData = DecompressBrotli(compressedData);

        // Reconstruct the font
        return ReconstructFont(flavor, tables, decompressedData);
    }

    private Woff2TableEntry ReadTableEntry(BinaryReader reader)
    {
        var entry = new Woff2TableEntry();

        byte flags = reader.ReadByte();
        int tagIndex = flags & 0x3F;

        if (tagIndex == 63)
        {
            entry.Tag = ReadUInt32BE(reader);
        }
        else if (tagIndex < KnownTags.Length)
        {
            entry.Tag = KnownTags[tagIndex];
        }
        else
        {
            throw new InvalidDataException($"Invalid table tag index: {tagIndex}");
        }

        int transformVersion = (flags >> 6) & 0x03;
        entry.TransformVersion = transformVersion;

        entry.OrigLength = ReadUIntBase128(reader);

        // For glyf and loca tables, transform version 0 means transformed
        // For other tables, transform version != 0 means transformed
        bool hasTransform;
        if (entry.Tag == 0x676C7966 || entry.Tag == 0x6C6F6361) // glyf or loca
        {
            hasTransform = transformVersion == 0;
        }
        else
        {
            hasTransform = transformVersion != 0;
        }

        if (hasTransform)
        {
            entry.TransformLength = ReadUIntBase128(reader);
        }
        else
        {
            entry.TransformLength = entry.OrigLength;
        }

        return entry;
    }

    private static uint ReadUIntBase128(BinaryReader reader)
    {
        uint result = 0;
        for (int i = 0; i < 5; i++)
        {
            byte b = reader.ReadByte();
            if (i == 0 && b == 0x80)
                throw new InvalidDataException("Invalid UIntBase128: leading zero");

            if ((result & 0xFE000000) != 0)
                throw new InvalidDataException("UIntBase128 overflow");

            result = (result << 7) | (uint)(b & 0x7F);

            if ((b & 0x80) == 0)
                return result;
        }
        throw new InvalidDataException("UIntBase128 too long");
    }

    private static byte[] DecompressBrotli(byte[] compressedData)
    {
        using var input = new MemoryStream(compressedData);
        using var brotli = new BrotliStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        brotli.CopyTo(output);
        return output.ToArray();
    }

    private byte[] ReconstructFont(uint flavor, List<Woff2TableEntry> tables, byte[] decompressedData)
    {
        var glyfTable = tables.FirstOrDefault(t => t.Tag == 0x676C7966); // glyf
        var locaTable = tables.FirstOrDefault(t => t.Tag == 0x6C6F6361); // loca
        var maxpTable = tables.FirstOrDefault(t => t.Tag == 0x6D617870); // maxp

        byte[]? reconstructedGlyf = null;
        byte[]? reconstructedLoca = null;
        short indexToLocFormat = 0;

        if (glyfTable != null && glyfTable.TransformVersion == 0 && locaTable != null)
        {
            int numGlyphs = 0;
            if (maxpTable != null)
            {
                int maxpOffset = (int)maxpTable.UncompressedOffset;
                if (maxpOffset + 6 <= decompressedData.Length)
                {
                    numGlyphs = (decompressedData[maxpOffset + 4] << 8) | decompressedData[maxpOffset + 5];
                }
            }

            if (numGlyphs > 0)
            {
                (reconstructedGlyf, reconstructedLoca, indexToLocFormat) = ReconstructGlyfLoca(
                    decompressedData,
                    (int)glyfTable.UncompressedOffset,
                    (int)glyfTable.TransformLength,
                    numGlyphs);
            }
        }

        using var output = new MemoryStream();
        using var writer = new BinaryWriter(output);

        int numTables = tables.Count;

        // Write sfnt header
        WriteUInt32BE(writer, flavor);
        WriteUInt16BE(writer, (ushort)numTables);

        int searchRange = 1;
        int entrySelector = 0;
        while (searchRange * 2 <= numTables)
        {
            searchRange *= 2;
            entrySelector++;
        }
        searchRange *= 16;
        int rangeShift = numTables * 16 - searchRange;

        WriteUInt16BE(writer, (ushort)searchRange);
        WriteUInt16BE(writer, (ushort)entrySelector);
        WriteUInt16BE(writer, (ushort)rangeShift);

        uint headerSize = (uint)(12 + numTables * 16);
        uint currentOffset = headerSize;

        var outputOffsets = new List<uint>();
        var outputLengths = new List<uint>();
        var tableData = new List<byte[]>();

        foreach (var table in tables)
        {
            outputOffsets.Add(currentOffset);

            byte[] data;
            if (table.Tag == 0x676C7966 && reconstructedGlyf != null)
            {
                data = reconstructedGlyf;
            }
            else if (table.Tag == 0x6C6F6361 && reconstructedLoca != null)
            {
                data = reconstructedLoca;
            }
            else
            {
                int srcOffset = (int)table.UncompressedOffset;
                int srcLength = (int)table.TransformLength;
                if (srcOffset + srcLength > decompressedData.Length)
                {
                    srcLength = Math.Max(0, decompressedData.Length - srcOffset);
                }
                data = new byte[table.OrigLength];
                if (srcLength > 0)
                {
                    Array.Copy(decompressedData, srcOffset, data, 0, Math.Min(srcLength, (int)table.OrigLength));
                }
            }

            tableData.Add(data);
            outputLengths.Add((uint)data.Length);

            currentOffset += (uint)data.Length;
            currentOffset = (currentOffset + 3) & ~3u;
        }

        // Write table directory
        for (int i = 0; i < tables.Count; i++)
        {
            var table = tables[i];
            WriteUInt32BE(writer, table.Tag);
            WriteUInt32BE(writer, CalculateChecksum(tableData[i], 0, tableData[i].Length));
            WriteUInt32BE(writer, outputOffsets[i]);
            WriteUInt32BE(writer, outputLengths[i]);
        }

        // Write table data
        for (int i = 0; i < tables.Count; i++)
        {
            var table = tables[i];
            var data = tableData[i];

            if (table.Tag == 0x68656164 && reconstructedLoca != null && data.Length >= 52)
            {
                data[50] = (byte)(indexToLocFormat >> 8);
                data[51] = (byte)(indexToLocFormat & 0xFF);
            }

            writer.Write(data);

            int padding = (4 - (data.Length % 4)) % 4;
            for (int p = 0; p < padding; p++)
            {
                writer.Write((byte)0);
            }
        }

        return output.ToArray();
    }

    private (byte[] glyf, byte[] loca, short indexToLocFormat) ReconstructGlyfLoca(
        byte[] data, int offset, int length, int numGlyphs)
    {
        using var ms = new MemoryStream(data, offset, length);
        using var reader = new BinaryReader(ms);

        // Read transformed glyf header
        reader.ReadBytes(4); // version (must be 0)

        ushort numGlyphsInTable = ReadUInt16BE(reader);
        ushort indexFormat = ReadUInt16BE(reader);

        uint nContourStreamSize = ReadUInt32BE(reader);
        uint nPointsStreamSize = ReadUInt32BE(reader);
        uint flagStreamSize = ReadUInt32BE(reader);
        uint glyphStreamSize = ReadUInt32BE(reader);
        uint compositeStreamSize = ReadUInt32BE(reader);
        uint bboxStreamSize = ReadUInt32BE(reader);
        uint instructionStreamSize = ReadUInt32BE(reader);

        // Read all substreams
        byte[] nContourStream = reader.ReadBytes((int)nContourStreamSize);
        byte[] nPointsStream = reader.ReadBytes((int)nPointsStreamSize);
        byte[] flagStream = reader.ReadBytes((int)flagStreamSize);
        byte[] glyphStream = reader.ReadBytes((int)glyphStreamSize);
        byte[] compositeStream = reader.ReadBytes((int)compositeStreamSize);
        byte[] bboxStream = reader.ReadBytes((int)bboxStreamSize);
        byte[] instructionStream = reader.ReadBytes((int)instructionStreamSize);

        // First pass: read nContours for all glyphs to know types
        var nContoursArr = new short[numGlyphsInTable];
        int nContourIdx = 0;
        for (int i = 0; i < numGlyphsInTable; i++)
        {
            if (nContourIdx + 1 < nContourStream.Length)
            {
                nContoursArr[i] = (short)((nContourStream[nContourIdx] << 8) | nContourStream[nContourIdx + 1]);
                nContourIdx += 2;
            }
        }

        // Parse bbox bitmap from the start of bboxStream
        int bboxBitmapBytes = (numGlyphsInTable + 7) / 8;
        byte[] bboxBitmap = ExpandBitmap(bboxStream, bboxBitmapBytes);
        int bboxDataIdx = bboxBitmapBytes; // bbox data starts after bitmap

        // Stream indices
        int nPointsIdx = 0;
        int flagIdx = 0;
        int glyphIdx = 0;
        int compositeIdx = 0;
        int instructionIdx = 0;

        // Build the TripleEncodingTable
        var encTable = TripleEncodingTable.Instance;

        // Reconstruct glyphs
        var glyfData = new MemoryStream();
        var glyfWriter = new BinaryWriter(glyfData);
        var offsets = new List<uint>();

        // Store per-glyph data for bbox computation
        var glyphPointsX = new List<short[]>();
        var glyphPointsY = new List<short[]>();

        for (int i = 0; i < numGlyphsInTable; i++)
        {
            offsets.Add((uint)glyfData.Position);

            short nContours = nContoursArr[i];

            if (nContours == 0)
            {
                // Empty glyph - no data
                glyphPointsX.Add(Array.Empty<short>());
                glyphPointsY.Add(Array.Empty<short>());
                continue;
            }
            else if (nContours < 0)
            {
                // Composite glyph - bbox MUST be present (bitmap bit must be set)
                short xMin = 0, yMin = 0, xMax = 0, yMax = 0;
                if (bboxBitmap.Length > i && bboxBitmap[i] == 1)
                {
                    if (bboxDataIdx + 7 < bboxStream.Length)
                    {
                        xMin = (short)((bboxStream[bboxDataIdx] << 8) | bboxStream[bboxDataIdx + 1]);
                        yMin = (short)((bboxStream[bboxDataIdx + 2] << 8) | bboxStream[bboxDataIdx + 3]);
                        xMax = (short)((bboxStream[bboxDataIdx + 4] << 8) | bboxStream[bboxDataIdx + 5]);
                        yMax = (short)((bboxStream[bboxDataIdx + 6] << 8) | bboxStream[bboxDataIdx + 7]);
                        bboxDataIdx += 8;
                    }
                }

                WriteInt16BE(glyfWriter, nContours);
                WriteInt16BE(glyfWriter, xMin);
                WriteInt16BE(glyfWriter, yMin);
                WriteInt16BE(glyfWriter, xMax);
                WriteInt16BE(glyfWriter, yMax);

                // Copy composite data from compositeStream
                bool hasMoreComponents = true;
                bool hasInstructions = false;
                while (hasMoreComponents && compositeIdx + 3 < compositeStream.Length)
                {
                    ushort flags = (ushort)((compositeStream[compositeIdx] << 8) | compositeStream[compositeIdx + 1]);
                    ushort glyphIndex = (ushort)((compositeStream[compositeIdx + 2] << 8) | compositeStream[compositeIdx + 3]);

                    WriteUInt16BE(glyfWriter, flags);
                    WriteUInt16BE(glyfWriter, glyphIndex);
                    compositeIdx += 4;

                    // Determine argument size based on ARG_1_AND_2_ARE_WORDS flag
                    int argSize = 2;
                    if ((flags & 0x0001) != 0) // ARG_1_AND_2_ARE_WORDS
                        argSize = 4;

                    if (compositeIdx + argSize <= compositeStream.Length)
                    {
                        glyfWriter.Write(compositeStream, compositeIdx, argSize);
                        compositeIdx += argSize;
                    }

                    // Handle transformation matrix
                    if ((flags & 0x0008) != 0) // WE_HAVE_A_SCALE
                    {
                        if (compositeIdx + 2 <= compositeStream.Length)
                        {
                            glyfWriter.Write(compositeStream, compositeIdx, 2);
                            compositeIdx += 2;
                        }
                    }
                    else if ((flags & 0x0040) != 0) // WE_HAVE_AN_X_AND_Y_SCALE
                    {
                        if (compositeIdx + 4 <= compositeStream.Length)
                        {
                            glyfWriter.Write(compositeStream, compositeIdx, 4);
                            compositeIdx += 4;
                        }
                    }
                    else if ((flags & 0x0080) != 0) // WE_HAVE_A_TWO_BY_TWO
                    {
                        if (compositeIdx + 8 <= compositeStream.Length)
                        {
                            glyfWriter.Write(compositeStream, compositeIdx, 8);
                            compositeIdx += 8;
                        }
                    }

                    hasMoreComponents = (flags & 0x0020) != 0; // MORE_COMPONENTS
                    if ((flags & 0x0100) != 0) // WE_HAVE_INSTRUCTIONS
                        hasInstructions = true;
                }

                // Handle composite glyph instructions
                if (hasInstructions)
                {
                    // Instruction length is encoded as 255UInt16 in glyphStream
                    ushort instrLen = Read255UInt16(glyphStream, ref glyphIdx);
                    WriteUInt16BE(glyfWriter, instrLen);
                    if (instrLen > 0 && instructionIdx + instrLen <= instructionStream.Length)
                    {
                        glyfWriter.Write(instructionStream, instructionIdx, instrLen);
                        instructionIdx += instrLen;
                    }
                }

                glyphPointsX.Add(Array.Empty<short>());
                glyphPointsY.Add(Array.Empty<short>());
            }
            else
            {
                // Simple glyph

                // Step 1: Read end points of contours from nPointsStream
                var endPtsOfContours = new ushort[nContours];
                int totalPoints = 0;
                for (int c = 0; c < nContours; c++)
                {
                    ushort nPoints = Read255UInt16(nPointsStream, ref nPointsIdx);
                    totalPoints += nPoints;
                    endPtsOfContours[c] = (ushort)(totalPoints - 1);
                }

                // Step 2: Read flags from flagStream
                var flags = new byte[totalPoints];
                for (int p = 0; p < totalPoints && flagIdx < flagStream.Length; p++)
                {
                    flags[p] = flagStream[flagIdx++];
                }

                // Step 3: Decode coordinates using triplet encoding
                // X and Y are decoded together from glyphStream using the flag
                var xCoords = new short[totalPoints];
                var yCoords = new short[totalPoints];
                int xAccum = 0;
                int yAccum = 0;

                for (int p = 0; p < totalPoints; p++)
                {
                    int xyFormat = flags[p] & 0x7F; // Lower 7 bits = encoding index
                    var enc = encTable[xyFormat];

                    // Read ByteCount - 1 bytes from glyphStream (ByteCount includes the flag byte)
                    int coordBytes = enc.ByteCount - 1;
                    byte[] packedXY = new byte[coordBytes];
                    for (int b = 0; b < coordBytes && glyphIdx < glyphStream.Length; b++)
                    {
                        packedXY[b] = glyphStream[glyphIdx++];
                    }

                    int dx = 0;
                    int dy = 0;

                    switch (enc.XBits)
                    {
                        case 0: // 0,8 format - Y only
                            dx = 0;
                            dy = enc.Ty(packedXY[0]);
                            break;
                        case 4: // 4,4 format - nibbles
                            dx = enc.Tx(packedXY[0] >> 4);
                            dy = enc.Ty(packedXY[0] & 0xF);
                            break;
                        case 8: // 8,0 or 8,8 format
                            dx = enc.Tx(packedXY[0]);
                            dy = (enc.YBits == 8) ? enc.Ty(packedXY[1]) : 0;
                            break;
                        case 12: // 12,12 format - nibble-packed
                            dx = enc.Tx((packedXY[0] << 4) | (packedXY[1] >> 4));
                            dy = enc.Ty(((packedXY[1] & 0xF) << 8) | packedXY[2]);
                            break;
                        case 16: // 16,16 format
                            dx = enc.Tx((packedXY[0] << 8) | packedXY[1]);
                            dy = enc.Ty((packedXY[2] << 8) | packedXY[3]);
                            break;
                    }

                    xAccum += dx;
                    yAccum += dy;
                    xCoords[p] = (short)xAccum;
                    yCoords[p] = (short)yAccum;
                }

                // Step 4: Read instruction length from glyphStream AFTER coordinates
                // (per WOFF2 spec, instruction length follows coordinate data in glyphStream)
                ushort instructionLength = Read255UInt16(glyphStream, ref glyphIdx);

                // Read bbox - check bitmap to see if explicit bbox is present
                short xMin, yMin, xMax, yMax;
                if (bboxBitmap.Length > i && bboxBitmap[i] == 1)
                {
                    // Explicit bbox from bboxStream
                    xMin = (short)((bboxStream[bboxDataIdx] << 8) | bboxStream[bboxDataIdx + 1]);
                    yMin = (short)((bboxStream[bboxDataIdx + 2] << 8) | bboxStream[bboxDataIdx + 3]);
                    xMax = (short)((bboxStream[bboxDataIdx + 4] << 8) | bboxStream[bboxDataIdx + 5]);
                    yMax = (short)((bboxStream[bboxDataIdx + 6] << 8) | bboxStream[bboxDataIdx + 7]);
                    bboxDataIdx += 8;
                }
                else
                {
                    // Compute bbox from glyph points
                    xMin = short.MaxValue;
                    yMin = short.MaxValue;
                    xMax = short.MinValue;
                    yMax = short.MinValue;
                    for (int p = 0; p < totalPoints; p++)
                    {
                        if (xCoords[p] < xMin) xMin = xCoords[p];
                        if (xCoords[p] > xMax) xMax = xCoords[p];
                        if (yCoords[p] < yMin) yMin = yCoords[p];
                        if (yCoords[p] > yMax) yMax = yCoords[p];
                    }
                    if (totalPoints == 0)
                    {
                        xMin = yMin = xMax = yMax = 0;
                    }
                }

                // Write glyph header
                WriteInt16BE(glyfWriter, nContours);
                WriteInt16BE(glyfWriter, xMin);
                WriteInt16BE(glyfWriter, yMin);
                WriteInt16BE(glyfWriter, xMax);
                WriteInt16BE(glyfWriter, yMax);

                // Write end points
                for (int c = 0; c < nContours; c++)
                {
                    WriteUInt16BE(glyfWriter, endPtsOfContours[c]);
                }

                // Write instruction length and instructions
                WriteUInt16BE(glyfWriter, instructionLength);
                if (instructionLength > 0 && instructionIdx + instructionLength <= instructionStream.Length)
                {
                    glyfWriter.Write(instructionStream, instructionIdx, instructionLength);
                    instructionIdx += instructionLength;
                }

                // Convert to standard TrueType format and write flags + coordinates
                WriteSimpleGlyphData(glyfWriter, flags, xCoords, yCoords, totalPoints);

                glyphPointsX.Add(xCoords);
                glyphPointsY.Add(yCoords);
            }

            // Align to 2-byte boundary (per WOFF2 spec, glyph data is padded to even)
            if (glyfData.Position % 2 != 0)
            {
                glyfWriter.Write((byte)0);
            }
        }

        // Add final offset
        offsets.Add((uint)glyfData.Position);

        // Build loca table
        byte[] locaData;
        short indexToLocFormat;

        uint maxOffset = offsets.Count > 0 ? offsets.Max() : 0;
        if (maxOffset > 0xFFFF * 2)
        {
            indexToLocFormat = 1;
            locaData = new byte[(numGlyphs + 1) * 4];
            for (int i = 0; i <= numGlyphs && i < offsets.Count; i++)
            {
                uint off = offsets[i];
                locaData[i * 4] = (byte)(off >> 24);
                locaData[i * 4 + 1] = (byte)(off >> 16);
                locaData[i * 4 + 2] = (byte)(off >> 8);
                locaData[i * 4 + 3] = (byte)off;
            }
        }
        else
        {
            indexToLocFormat = 0;
            locaData = new byte[(numGlyphs + 1) * 2];
            for (int i = 0; i <= numGlyphs && i < offsets.Count; i++)
            {
                ushort off = (ushort)(offsets[i] / 2);
                locaData[i * 2] = (byte)(off >> 8);
                locaData[i * 2 + 1] = (byte)off;
            }
        }

        return (glyfData.ToArray(), locaData, indexToLocFormat);
    }

    /// <summary>
    /// Expand packed bitmap to 1 byte per glyph
    /// </summary>
    private static byte[] ExpandBitmap(byte[] bboxStream, int bitmapByteCount)
    {
        int actualBytes = Math.Min(bitmapByteCount, bboxStream.Length);
        byte[] expanded = new byte[actualBytes * 8];

        int index = 0;
        for (int i = 0; i < actualBytes; i++)
        {
            byte b = bboxStream[i];
            expanded[index++] = (byte)((b >> 7) & 0x1);
            expanded[index++] = (byte)((b >> 6) & 0x1);
            expanded[index++] = (byte)((b >> 5) & 0x1);
            expanded[index++] = (byte)((b >> 4) & 0x1);
            expanded[index++] = (byte)((b >> 3) & 0x1);
            expanded[index++] = (byte)((b >> 2) & 0x1);
            expanded[index++] = (byte)((b >> 1) & 0x1);
            expanded[index++] = (byte)(b & 0x1);
        }
        return expanded;
    }

    /// <summary>
    /// Read a 255UInt16 value from a byte array.
    /// Per WOFF2 spec:
    ///   code 253: read 2 more bytes as big-endian UInt16
    ///   code 254: read 1 more byte, add 506
    ///   code 255: read 1 more byte, add 253
    ///   code 0-252: direct value
    /// </summary>
    private static ushort Read255UInt16(byte[] data, ref int index)
    {
        if (index >= data.Length) return 0;

        byte code = data[index++];
        if (code == 253) // WORD_CODE
        {
            if (index + 1 >= data.Length) return 0;
            int value = data[index++];
            value = (value << 8) & 0xFF00;
            int value2 = data[index++];
            return (ushort)(value | (value2 & 0xFF));
        }
        else if (code == 255) // ONE_MORE_BYTE_CODE1
        {
            if (index >= data.Length) return 0;
            return (ushort)(data[index++] + 253);
        }
        else if (code == 254) // ONE_MORE_BYTE_CODE2
        {
            if (index >= data.Length) return 0;
            return (ushort)(data[index++] + 253 * 2);
        }
        else
        {
            return code;
        }
    }

    private static void WriteSimpleGlyphData(BinaryWriter writer, byte[] woff2Flags, short[] xCoords, short[] yCoords, int numPoints)
    {
        // Convert WOFF2 flags to TrueType flags and write delta-encoded coordinates
        var ttFlags = new List<byte>();
        var xDeltas = new List<byte[]>();
        var yDeltas = new List<byte[]>();

        short prevX = 0;
        short prevY = 0;

        for (int i = 0; i < numPoints; i++)
        {
            short dx = (short)(xCoords[i] - prevX);
            short dy = (short)(yCoords[i] - prevY);
            prevX = xCoords[i];
            prevY = yCoords[i];

            byte ttFlag = 0;

            // On-curve: WOFF2 flag bit 7 = 0 means on-curve
            if ((woff2Flags[i] & 0x80) == 0)
            {
                ttFlag |= 0x01; // ON_CURVE_POINT
            }

            // X coordinate encoding
            if (dx == 0)
            {
                ttFlag |= 0x10; // X_IS_SAME
                xDeltas.Add(Array.Empty<byte>());
            }
            else if (dx >= -255 && dx <= 255)
            {
                ttFlag |= 0x02; // X_SHORT_VECTOR
                if (dx > 0)
                {
                    ttFlag |= 0x10; // Positive
                    xDeltas.Add(new byte[] { (byte)dx });
                }
                else
                {
                    xDeltas.Add(new byte[] { (byte)(-dx) });
                }
            }
            else
            {
                xDeltas.Add(new byte[] { (byte)(dx >> 8), (byte)(dx & 0xFF) });
            }

            // Y coordinate encoding
            if (dy == 0)
            {
                ttFlag |= 0x20; // Y_IS_SAME
                yDeltas.Add(Array.Empty<byte>());
            }
            else if (dy >= -255 && dy <= 255)
            {
                ttFlag |= 0x04; // Y_SHORT_VECTOR
                if (dy > 0)
                {
                    ttFlag |= 0x20; // Positive
                    yDeltas.Add(new byte[] { (byte)dy });
                }
                else
                {
                    yDeltas.Add(new byte[] { (byte)(-dy) });
                }
            }
            else
            {
                yDeltas.Add(new byte[] { (byte)(dy >> 8), (byte)(dy & 0xFF) });
            }

            ttFlags.Add(ttFlag);
        }

        // Write flags
        for (int i = 0; i < ttFlags.Count; i++)
        {
            writer.Write(ttFlags[i]);
        }

        // Write X coordinates
        foreach (var delta in xDeltas)
        {
            writer.Write(delta);
        }

        // Write Y coordinates
        foreach (var delta in yDeltas)
        {
            writer.Write(delta);
        }
    }

    private static void WriteInt16BE(BinaryWriter writer, short value)
    {
        writer.Write((byte)((value >> 8) & 0xFF));
        writer.Write((byte)(value & 0xFF));
    }

    private static uint CalculateChecksum(byte[] data, int offset, int length)
    {
        uint sum = 0;
        int end = Math.Min(offset + length, data.Length);
        int i = offset;

        while (i + 4 <= end)
        {
            sum += (uint)((data[i] << 24) | (data[i + 1] << 16) | (data[i + 2] << 8) | data[i + 3]);
            i += 4;
        }

        if (i < end)
        {
            uint last = 0;
            int shift = 24;
            while (i < end)
            {
                last |= (uint)(data[i] << shift);
                shift -= 8;
                i++;
            }
            sum += last;
        }

        return sum;
    }

    private static uint ReadUInt32BE(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(4);
        return (uint)((bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3]);
    }

    private static ushort ReadUInt16BE(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(2);
        return (ushort)((bytes[0] << 8) | bytes[1]);
    }

    private static void WriteUInt32BE(BinaryWriter writer, uint value)
    {
        writer.Write((byte)((value >> 24) & 0xFF));
        writer.Write((byte)((value >> 16) & 0xFF));
        writer.Write((byte)((value >> 8) & 0xFF));
        writer.Write((byte)(value & 0xFF));
    }

    private static void WriteUInt16BE(BinaryWriter writer, ushort value)
    {
        writer.Write((byte)((value >> 8) & 0xFF));
        writer.Write((byte)(value & 0xFF));
    }

    private class Woff2TableEntry
    {
        public uint Tag { get; set; }
        public uint OrigLength { get; set; }
        public uint TransformLength { get; set; }
        public int TransformVersion { get; set; }
        public uint UncompressedOffset { get; set; }
    }

    #region Triple Encoding Table

    /// <summary>
    /// WOFF2 triplet encoding record.
    /// Defines how to decode a coordinate pair from the glyph stream.
    /// ByteCount includes the flag byte.
    /// </summary>
    private readonly struct TripleEncodingRecord
    {
        public readonly byte ByteCount;
        public readonly byte XBits;
        public readonly byte YBits;
        public readonly ushort DeltaX;
        public readonly ushort DeltaY;
        public readonly sbyte Xsign;
        public readonly sbyte Ysign;

        public TripleEncodingRecord(byte byteCount, byte xbits, byte ybits,
            ushort deltaX, ushort deltaY, sbyte xsign, sbyte ysign)
        {
            ByteCount = byteCount;
            XBits = xbits;
            YBits = ybits;
            DeltaX = deltaX;
            DeltaY = deltaY;
            Xsign = xsign;
            Ysign = ysign;
        }

        public int Tx(int orgX) => (orgX + DeltaX) * Xsign;
        public int Ty(int orgY) => (orgY + DeltaY) * Ysign;
    }

    /// <summary>
    /// Builds the 128-entry triple encoding table per the WOFF2 specification.
    /// </summary>
    private class TripleEncodingTable
    {
        private static readonly Lazy<TripleEncodingTable> _instance = new(() => new TripleEncodingTable());
        public static TripleEncodingTable Instance => _instance.Value;

        private readonly TripleEncodingRecord[] _records;

        public TripleEncodingRecord this[int index] => _records[index];

        private TripleEncodingTable()
        {
            var records = new List<TripleEncodingRecord>();

            // Set 1.1 (indices 0-9): ByteCount=2, X=0bits, Y=8bits
            BuildRecords(records, 2, 0, 8, null, new ushort[] { 0, 256, 512, 768, 1024 });

            // Set 1.2 (indices 10-19): ByteCount=2, X=8bits, Y=0bits
            BuildRecords(records, 2, 8, 0, new ushort[] { 0, 256, 512, 768, 1024 }, null);

            // Set 2.1 (indices 20-35): ByteCount=2, X=4bits, Y=4bits, DeltaX=1
            BuildRecords(records, 2, 4, 4, new ushort[] { 1 }, new ushort[] { 1, 17, 33, 49 });

            // Set 2.2 (indices 36-51): ByteCount=2, X=4bits, Y=4bits, DeltaX=17
            BuildRecords(records, 2, 4, 4, new ushort[] { 17 }, new ushort[] { 1, 17, 33, 49 });

            // Set 2.3 (indices 52-67): ByteCount=2, X=4bits, Y=4bits, DeltaX=33
            BuildRecords(records, 2, 4, 4, new ushort[] { 33 }, new ushort[] { 1, 17, 33, 49 });

            // Set 2.4 (indices 68-83): ByteCount=2, X=4bits, Y=4bits, DeltaX=49
            BuildRecords(records, 2, 4, 4, new ushort[] { 49 }, new ushort[] { 1, 17, 33, 49 });

            // Set 3.1 (indices 84-95): ByteCount=3, X=8bits, Y=8bits, DeltaX=1
            BuildRecords(records, 3, 8, 8, new ushort[] { 1 }, new ushort[] { 1, 257, 513 });

            // Set 3.2 (indices 96-107): ByteCount=3, X=8bits, Y=8bits, DeltaX=257
            BuildRecords(records, 3, 8, 8, new ushort[] { 257 }, new ushort[] { 1, 257, 513 });

            // Set 3.3 (indices 108-119): ByteCount=3, X=8bits, Y=8bits, DeltaX=513
            BuildRecords(records, 3, 8, 8, new ushort[] { 513 }, new ushort[] { 1, 257, 513 });

            // Set 4 (indices 120-123): ByteCount=4, X=12bits, Y=12bits
            BuildRecords(records, 4, 12, 12, new ushort[] { 0 }, new ushort[] { 0 });

            // Set 5 (indices 124-127): ByteCount=5, X=16bits, Y=16bits
            BuildRecords(records, 5, 16, 16, new ushort[] { 0 }, new ushort[] { 0 });

            _records = records.ToArray();
        }

        private static void BuildRecords(List<TripleEncodingRecord> records,
            byte byteCount, byte xbits, byte ybits, ushort[]? deltaXs, ushort[]? deltaYs)
        {
            if (deltaXs == null)
            {
                // Set 1.1: Y-only (X is 0)
                for (int y = 0; y < deltaYs!.Length; y++)
                {
                    records.Add(new TripleEncodingRecord(byteCount, xbits, ybits, 0, deltaYs[y], 0, -1));
                    records.Add(new TripleEncodingRecord(byteCount, xbits, ybits, 0, deltaYs[y], 0, 1));
                }
            }
            else if (deltaYs == null)
            {
                // Set 1.2: X-only (Y is 0)
                for (int x = 0; x < deltaXs.Length; x++)
                {
                    records.Add(new TripleEncodingRecord(byteCount, xbits, ybits, deltaXs[x], 0, -1, 0));
                    records.Add(new TripleEncodingRecord(byteCount, xbits, ybits, deltaXs[x], 0, 1, 0));
                }
            }
            else
            {
                // Sets 2.1 through 5: Both X and Y
                for (int x = 0; x < deltaXs.Length; x++)
                {
                    for (int y = 0; y < deltaYs.Length; y++)
                    {
                        records.Add(new TripleEncodingRecord(byteCount, xbits, ybits, deltaXs[x], deltaYs[y], -1, -1));
                        records.Add(new TripleEncodingRecord(byteCount, xbits, ybits, deltaXs[x], deltaYs[y], 1, -1));
                        records.Add(new TripleEncodingRecord(byteCount, xbits, ybits, deltaXs[x], deltaYs[y], -1, 1));
                        records.Add(new TripleEncodingRecord(byteCount, xbits, ybits, deltaXs[x], deltaYs[y], 1, 1));
                    }
                }
            }
        }
    }

    #endregion
}
