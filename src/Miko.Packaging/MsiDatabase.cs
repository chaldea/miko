using System.Text;
using OpenMcdf;

namespace Miko.Packaging;

// MSI stores tables column-wise in named compound-file streams. IDT type codes describe
// their on-disk widths, nullability (uppercase) and localization (l).
internal sealed class MsiDatabase(RootStorage storage)
{
    internal sealed record Column(string Name, char Type, int Width, bool Key)
    {
        public bool Numeric => char.ToLowerInvariant(Type) == 'i';
        public int Flags => Width | 0x100 | (Numeric ? Width == 4 ? 0 : 0x400 : 0xc00)
            | (char.ToLowerInvariant(Type) == 'l' ? 0x200 : 0) | (char.IsUpper(Type) ? 0x1000 : 0) | (Key ? 0x2000 : 0);
    }

    internal sealed class Table(string name, Column[] columns)
    {
        public string Name { get; } = name;
        public Column[] Columns { get; } = columns;
        public List<object?[]> Rows { get; } = [];
        public void Add(params object?[] cells) => Rows.Add(cells);
    }

    private readonly List<Table> tables = [];
    private readonly Dictionary<string, int> ids = new(StringComparer.Ordinal);
    private readonly List<ushort> references = [];

    public Table Define(string name, string schema)
    {
        var columns = schema.Split(' ').Select(def =>
        {
            var pair = def.TrimEnd('*').Split(':');
            return new Column(pair[0], pair[1][0], int.Parse(pair[1][1..]), def.EndsWith('*'));
        }).ToArray();
        var table = new Table(name, columns);
        tables.Add(table);
        return table;
    }

    public void Save()
    {
        var userTables = tables.ToArray();
        var names = Define("_Tables", "Name:s64*");
        var columns = Define("_Columns", "Table:s64* Number:i2* Name:s64 Type:i2");
        foreach (var table in userTables)
        {
            names.Add(table.Name);
            for (int i = 0; i < table.Columns.Length; i++)
                columns.Add(table.Name, i + 1, table.Columns[i].Name, table.Columns[i].Flags);
        }
        foreach (var table in tables)
        {
            if (table.Rows.Count == 0) continue; // A listed table with no data stream is empty.
            using var writer = new BinaryWriter(storage.CreateStream(StreamName(table.Name, true)));
            var rows = table.Rows.Select(row => row.Select((v, i) =>
                v is string s ? Intern(s) : (int?)v ?? (table.Columns[i].Numeric ? NullInteger : 0)).ToArray()).ToArray();
            var keys = Enumerable.Range(0, table.Columns.Length).Where(i => table.Columns[i].Key).ToArray();
            Array.Sort(rows, (a, b) =>
            {
                foreach (var key in keys)
                    if (a[key] != b[key]) return a[key].CompareTo(b[key]);
                return 0;
            });
            for (int i = 0; i < table.Columns.Length; i++)
            {
                var column = table.Columns[i];
                foreach (var row in rows)
                {
                    int v = row[i];
                    if (!column.Numeric) writer.Write(checked((ushort)v));
                    else if (column.Width == 4) writer.Write(v == NullInteger ? 0 : v ^ int.MinValue);
                    else writer.Write(v == NullInteger ? (ushort)0 : unchecked((ushort)(checked((short)v) ^ 0x8000)));
                }
            }
        }
        using var pool = new BinaryWriter(storage.CreateStream(StreamName("_StringPool", true)));
        using var data = storage.CreateStream(StreamName("_StringData", true));
        pool.Write(65001); // UTF-8, two-byte string references.
        foreach (var (value, id) in ids.OrderBy(p => p.Value))
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            pool.Write(checked((ushort)bytes.Length)); pool.Write(references[id - 1]);
            data.Write(bytes);
        }
    }

    internal const int NullInteger = int.MinValue;
    private int Intern(string value)
    {
        if (value == "") return 0;
        if (!ids.TryGetValue(value, out int id))
        {
            id = checked((ushort)(ids.Count + 1));
            ids.Add(value, id);
            references.Add(0);
        }
        references[id - 1] = checked((ushort)(references[id - 1] + 1));
        return id;
    }

    internal static string StreamName(string name, bool table)
    {
        const string alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz._";
        var result = new StringBuilder();
        if (table) result.Append('\u4840');
        for (int i = 0; i < name.Length; i++)
        {
            int first = alphabet.IndexOf(name[i]);
            int second = i + 1 < name.Length ? alphabet.IndexOf(name[i + 1]) : -1;
            if (first < 0) result.Append(name[i]);
            else if (second < 0) result.Append((char)(0x4800 + first));
            else { result.Append((char)(0x3800 + first + (second << 6))); i++; }
        }
        return result.ToString();
    }
}
