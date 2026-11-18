using System.IO.Compression;
using System.Text;
using Miko.Common;
using Miko.Components.Reader;
using Miko.Core;
using Miko.Hosting;
using Shouldly;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace Miko.Components.Tests.Reader;

public sealed class FileReaderSourceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "miko-reader-tests-" + Guid.NewGuid().ToString("N"));
    public FileReaderSourceTests() => Directory.CreateDirectory(_directory);
    public void Dispose() => Directory.Delete(_directory, true);
    private string PathFor(string extension) => Path.Combine(_directory, "book" + extension);

    [Fact]
    public async Task Txt_Detects_Bom_And_Chunks_Without_Losing_Unicode()
    {
        const string text = "你好👩🏽‍💻a\r\nCafe\u0301 and more text\nLast.";
        string path = PathFor(".txt");
        await File.WriteAllTextAsync(path, text, Encoding.Unicode);
        await using var source = new FileReaderSource(path, new() { TextChunkLength = 10 });
        var document = await source.GetDocumentAsync();
        var chunks = new List<string>();
        foreach (var entry in document.Entries) chunks.Add(((ReaderTextContent)await source.ReadAsync(entry)).Text);
        string.Concat(chunks).ShouldBe(text.Replace("\r\n", "\n"));
        chunks.ShouldContain(c => c.Contains("👩🏽‍💻"));
        chunks.ShouldAllBe(c => !char.IsLowSurrogate(c[0]) && !char.IsHighSurrogate(c[c.Length - 1]));
    }

    [Fact]
    public async Task Epub_Uses_Container_Spine_And_Navigation_Without_Loading_All_Chapters()
    {
        string path = PathFor(".epub");
        Zip(path, new()
        {
            ["META-INF/container.xml"] = "<container><rootfiles><rootfile full-path='OPS/package.opf'/></rootfiles></container>",
            ["OPS/package.opf"] = "<package><metadata><title>测试书籍</title></metadata><manifest><item id='a' href='Text/a.xhtml' media-type='application/xhtml+xml'/><item id='b' href='Text/b.xhtml' media-type='application/xhtml+xml'/><item id='nav' href='nav.xhtml' properties='nav' media-type='application/xhtml+xml'/></manifest><spine><itemref idref='b'/><itemref idref='a'/></spine></package>",
            ["OPS/nav.xhtml"] = "<html xmlns:epub='http://www.idpf.org/2007/ops'><body><nav epub:type='toc'><a href='Text/b.xhtml#start'>第二章</a><a href='Text/a.xhtml'>第一章</a></nav></body></html>",
            ["OPS/Text/b.xhtml"] = "<html><head><title>Ignore</title><style>bad</style></head><body><h1>标题</h1><p>Hello <b>世界</b> &amp; friends.</p><script>bad()</script><p>结束</p></body></html>",
            ["OPS/Text/a.xhtml"] = "deliberately malformed; the unopened chapter must not be parsed",
        });
        await using var source = new FileReaderSource(path);
        var document = await source.GetDocumentAsync();
        document.Title.ShouldBe("测试书籍");
        document.Entries.Select(e => e.Title).ShouldBe(["第二章", "第一章"]);
        var content = (ReaderTextContent)await source.ReadAsync(document.Entries[0]);
        content.Text.ShouldBe("标题\nHello 世界 & friends.\n结束");
    }

    [Fact]
    public void Epub_Text_Accepts_Html_Entities_Without_Fetching_External_Dtd()
    {
        var content = Encoding.UTF8.GetBytes("<!DOCTYPE html SYSTEM 'https://invalid.example/no-network.dtd'><html><body><p>Hello&nbsp;world &copy; &mdash; 你好</p><script>hidden</script></body></html>");
        EpubReader.ReadText(content).ShouldBe("Hello world © — 你好");
    }

    [Fact]
    public async Task Epub2_Ncx_Resolves_Parent_Paths_And_Percent_Encoded_Names()
    {
        string path = PathFor(".epub");
        Zip(path, new()
        {
            ["META-INF/container.xml"] = "<container><rootfile full-path='OPS/book.opf'/></container>",
            ["OPS/book.opf"] = "<package><manifest><item id='c' href='../chap%20one.xhtml' media-type='application/xhtml+xml'/><item id='toc' href='toc.ncx' media-type='application/x-dtbncx+xml'/></manifest><spine><itemref idref='c'/></spine></package>",
            ["OPS/toc.ncx"] = "<ncx><navMap><navPoint><navLabel><text>Chapter one</text></navLabel><content src='../chap%20one.xhtml#x'/></navPoint></navMap></ncx>",
            ["chap one.xhtml"] = "<html><body><p>One</p></body></html>",
        });
        await using var source = new FileReaderSource(path);
        var document = await source.GetDocumentAsync();
        document.Entries.Single().ShouldBe(new ReaderEntry("chap one.xhtml", "Chapter one"));
        Should.Throw<InvalidDataException>(() => EpubReader.Resolve("OPS/book.opf", "../../outside"));
    }

    [Theory]
    [InlineData(".cbz")]
    [InlineData(".cbr")]
    public async Task Comics_Read_Images_In_Natural_Order_And_Ignore_Archive_Metadata(string extension)
    {
        string path = PathFor(extension);
        var files = new Dictionary<string, byte[]> { ["page10.png"] = [10], ["page2.png"] = [2], ["page1.png"] = [1], ["__MACOSX/page0.png"] = [0], ["ComicInfo.xml"] = [0] };
        if (extension == ".cbr") File.WriteAllBytes(path, StoredRar(files));
        else using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            foreach (var (name, bytes) in files) { using var entry = archive.CreateEntry(name).Open(); entry.Write(bytes); }
        await using var source = new FileReaderSource(path);
        var document = await source.GetDocumentAsync();
        document.Entries.Select(e => e.Id).ShouldBe(["page1.png", "page2.png", "page10.png"]);
        ((ReaderImageContent)await source.ReadAsync(document.Entries[1])).Data.ToArray().ShouldBe(new byte[] { 2 });
    }

    [Fact]
    public async Task Pdf_Renders_A_Text_Page_And_A_Scanned_Page_And_Can_Reflow_Text()
    {
        string path = PathFor(".pdf");
        var engine = new MikoEngineBuilder().Build();
        using var image = engine.Graphics.CreateSurface(20, 30);
        image.Context.Clear(Color.FromHex("#ff0000"));
        using var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        builder.AddPage(200, 300).AddText("Hello reader", 20, new PdfPoint(20, 200), font);
        builder.AddPage(200, 300).AddPng(image.EncodePng(), new PdfRectangle(0, 0, 200, 300));
        File.WriteAllBytes(path, builder.Build());
        await using (var source = new FileReaderSource(path, new() { PdfScale = 1 }))
        {
            var document = await source.GetDocumentAsync();
            document.Entries.Count.ShouldBe(2);
            foreach (var entry in document.Entries)
            {
                var page = (ReaderImageContent)await source.ReadAsync(entry);
                using var decoded = engine.Graphics.DecodeImage(page.Data.Span);
                decoded.ShouldNotBeNull();
                decoded.Width.ShouldBe(200);
                decoded.Height.ShouldBe(300);
            }
        }
        await using (var source = new FileReaderSource(path, new() { ReflowPdf = true }))
        {
            var document = await source.GetDocumentAsync();
            ((ReaderTextContent)await source.ReadAsync(document.Entries[0])).Text.ShouldContain("Hello reader");
        }
    }

    [Fact]
    public async Task Entry_Limits_And_Disposal_Are_Enforced()
    {
        string path = PathFor(".cbz");
        Zip(path, new() { ["1.png"] = new string('x', 1000) });
        var source = new FileReaderSource(path, new() { MaxEntryBytes = 32 });
        var document = await source.GetDocumentAsync();
        await Should.ThrowAsync<InvalidDataException>(() => source.ReadAsync(document.Entries[0]));
        await source.DisposeAsync();
        await Should.ThrowAsync<ObjectDisposedException>(() => source.GetDocumentAsync());
        // All archive handles are closed, including after failures.
        using var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    }

    private static void Zip(string path, Dictionary<string, string> files)
    {
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var (name, content) in files)
        {
            using var writer = new StreamWriter(archive.CreateEntry(name).Open(), new UTF8Encoding(false));
            writer.Write(content);
        }
    }

    // Small RAR4 store-only fixture generated in the test: no external rar executable or licensed sample.
    private static byte[] StoredRar(Dictionary<string, byte[]> files)
    {
        using var output = new MemoryStream();
        output.Write([0x52, 0x61, 0x72, 0x21, 0x1a, 0x07, 0x00]);
        Header(0x73, 0, new byte[6]);
        foreach (var (name, bytes) in files)
        {
            using var body = new MemoryStream();
            using var writer = new BinaryWriter(body, Encoding.UTF8, true);
            byte[] filename = Encoding.UTF8.GetBytes(name);
            writer.Write(bytes.Length); writer.Write(bytes.Length); writer.Write((byte)3);
            writer.Write(Crc(bytes)); writer.Write(0u); writer.Write((byte)20); writer.Write((byte)0x30);
            writer.Write((ushort)filename.Length); writer.Write(0x20u); writer.Write(filename);
            Header(0x74, 0x8000, body.ToArray()); output.Write(bytes);
        }
        Header(0x7b, 0, []);
        return output.ToArray();

        void Header(byte type, ushort flags, byte[] body)
        {
            using var header = new MemoryStream();
            using var writer = new BinaryWriter(header, Encoding.UTF8, true);
            writer.Write(type); writer.Write(flags); writer.Write((ushort)(7 + body.Length)); writer.Write(body);
            byte[] bytes = header.ToArray();
            using var target = new BinaryWriter(output, Encoding.UTF8, true);
            target.Write((ushort)Crc(bytes)); target.Write(bytes);
        }
        static uint Crc(byte[] bytes)
        {
            uint crc = 0xffffffff;
            foreach (byte b in bytes)
            {
                crc ^= b;
                for (int i = 0; i < 8; i++) crc = (crc >> 1) ^ ((crc & 1) != 0 ? 0xedb88320 : 0);
            }
            return ~crc;
        }
    }
}
