namespace Invocative.VDX;

using System.Text;

public static class Ex
{
    public static ulong Sum(this IEnumerable<ulong> l)
    {
        var o = 0ul;

        foreach (var @ulong in l)
        {
            o += @ulong;
        }

        return o;
    }


    public static DirectoryInfo Directory(this DirectoryInfo b, string target)
        => new(Path.Combine(b.FullName, target));

    public static void CreateIfNotExist(this DirectoryInfo b)
    {
        if (!b.Exists) b.Create();
    }
    public static FileInfo File(this DirectoryInfo b, string target)
        => new(Path.Combine(b.FullName, target));

    public static string Join(this IEnumerable<string> s, string c) => string.Join(c, s);

    public static void WriteVdxString(this BinaryWriter writer, string str)
    {
        writer.Write(new byte[] { 0xAB });
        writer.Write(Encoding.UTF8.GetByteCount(str));
        writer.Write(Encoding.UTF8.GetBytes(str));
        writer.Write(new byte[] { 0xCD });
    }

    public static string ReadVdxString(this BinaryReader reader)
    {
        reader.Assert(0xAB);
        var len = reader.ReadInt32();
        var bts = new byte[len];
        reader.Read(bts);
        var str = Encoding.UTF8.GetString(bts);
        reader.Assert(0xCD);

        return str;
    }

    public static string ReadASCIIString(this BinaryReader reader, int size)
    {
        var result = new byte[size];

        reader.Read(result);

        return Encoding.ASCII.GetString(result);
    }

    public unsafe static Guid ReadGuid(this BinaryReader reader)
    {
        var result = new byte[sizeof(Guid)];

        reader.Read(result);

        return new Guid(result);
    }

    public static void Skip(this BinaryReader reader, int size)
    {
        var result = new byte[size];
        reader.Read(result);
    }

    public static void Assert(this BinaryReader reader, byte to)
    {
        if (to != reader.ReadByte())
            throw new Exception("Format bad");
    }
}