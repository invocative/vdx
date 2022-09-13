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
}