using System.Text;

namespace Miko.Packaging;

internal sealed class BillOfMaterials
{
    private readonly List<byte[]> blocks = [[]]; // block 0 is the null pointer
    private readonly Dictionary<string, int> variables = [];
    private int Add(byte[] data) { blocks.Add(data); return blocks.Count - 1; }
    private int Add(Action<Stream> write) => Add(NetworkOrder.Bytes(write));

    public static void Write(string path, PackageEntry[] files)
    {
        var bom = new BillOfMaterials();
        bom.variables["BomInfo"] = bom.Add(s =>
        {
            s.U32(1); s.U32(files.Length + 1); s.U32(1); s.Write(new byte[16]);
        });
        var ids = new Dictionary<string, int>(StringComparer.Ordinal);
        var records = new List<(int Parent, string Name, int Value, int Key)>();
        foreach (var file in files)
        {
            var slash = file.Name.LastIndexOf('/');
            int parent = slash < 0 ? 0 : ids[file.Name[..slash]];
            string name = file.Name[(slash + 1)..];
            int info = bom.Add(s =>
            {
                s.WriteByte((byte)(file.IsDirectory ? 2 : file.IsLink ? 3 : 1));
                s.WriteByte(1);
                s.U16(15); s.U16(file.Mode);
                s.U32(0); s.U32(0); s.U32(0); s.U32(file.Size);
                s.WriteByte(1);
                s.U32(file.IsDirectory ? 0 : Checksum.Bom(file));
                s.U32(file.IsLink ? file.Size + 1 : 0);
                if (file.IsLink) s.CString(file.LinkTarget);
            });
            int id = ids.Count + 1;
            ids.Add(file.Name, id);
            int value = bom.Add(s => { s.U32(id); s.U32(info); });
            int key = bom.Add(s => { s.U32(parent); s.CString(name); });
            records.Add((parent, name, value, key));
        }
        var sorted = records.OrderBy(r => r.Parent).ThenBy(r => r.Name, StringComparer.Ordinal).Select(r => (r.Value, r.Key)).ToList();
        bom.variables["Paths"] = bom.Tree(sorted);
        bom.variables["HLIndex"] = bom.Tree([]);
        bom.variables["Size64"] = bom.Tree([]);
        var vtree = bom.Tree([]);
        bom.variables["VIndex"] = bom.Add(s => { s.U32(1); s.U32(vtree); s.U32(0); s.WriteByte(0); });
        bom.Save(path);
    }

    private int Tree(List<(int Value, int Key)> records)
    {
        const int blockSize = 4096;
        int count = records.Count;
        bool leaf = true;
        while (true)
        {
            var parents = new List<(int Value, int Key)>();
            var pages = records.Chunk((blockSize - 12) / 8).ToList();
            if (pages.Count == 0) pages.Add([]);
            for (int i = 0; i < pages.Count; i++)
            {
                var page = pages[i];
                int index = blocks.Count;
                int next = i + 1 < pages.Count ? index + 1 : 0;
                int prev = i == 0 ? 0 : index - 1;
                Add(s =>
                {
                    s.U16(leaf ? 1 : 0); s.U16(page.Length);
                    s.U32(next); s.U32(prev);
                    foreach (var (value, key) in page) { s.U32(value); s.U32(key); }
                    s.Write(new byte[blockSize - (int)s.Length]);
                });
                parents.Add((index, page.Length == 0 ? 0 : page[^1].Key));
            }
            if (parents.Count == 1)
                return Add(s =>
                {
                    s.Write("tree"u8); s.U32(1); s.U32(parents[0].Value);
                    s.U32(blockSize); s.U32(count); s.WriteByte(0);
                });
            records = parents;
            leaf = false;
        }
    }

    private void Save(string path)
    {
        using var output = File.Create(path);
        output.Position = 512;
        output.U32(variables.Count);
        foreach (var (name, id) in variables)
        {
            output.U32(id);
            output.WriteByte((byte)name.Length);
            output.Write(Encoding.ASCII.GetBytes(name));
        }
        long variableSize = output.Position - 512;
        var pointers = new List<(long Offset, int Size)> { (0, 0) };
        foreach (var block in blocks.Skip(1))
        {
            pointers.Add((output.Position, block.Length));
            output.Write(block);
        }
        long index = output.Position;
        output.U32(blocks.Count);
        foreach (var (offset, size) in pointers) { output.U32(offset); output.U32(size); }
        output.U32(0);
        long indexSize = output.Position - index;
        output.Position = 0;
        output.Write("BOMStore"u8); output.U32(1); output.U32(blocks.Count - 1);
        output.U32(index); output.U32(indexSize); output.U32(512); output.U32(variableSize);
    }
}
