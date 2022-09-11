namespace Invocative.VDX;

using System.Collections.Concurrent;
using System.IO.Hashing;
using Newtonsoft.Json;
using Spectre.Console;

public static class _
{
    public static void Main(string[] args)
    {
        var p = new Pipeline();
        p.CollectFiles(new DirectoryInfo("C:\\git\\ElementarySandbox.V2\\Assets"));
    }
}


public class Pipeline
{
    public static int ALIGN = 512;

    public List<string> TEXTURE_EXT = new ()
    {
        ".png",
        ".bmp",
        ".dds",
        ".jpg"
    };

    public List<string> MODEL_EXT = new ()
    {
        ".fbx", // autodesk fbx
        ".obj",
        ".gltf" // opengl scene 
    };

    public List<string> AUDIO_EXT = new ()
    {
        ".wav",
    };
    
    public List<string> NO_PACK_EXT = new ()
    {
        ".json", ".ini", ".xml",
        ".js", ".mjs",
        ".yaml", ".yml",
        ".html",
        ".css",
        ".svg",
        ".jsm",
        ".md",
    };

    public List<string> IGNORED_EXT = new ()
    {
        // ignore binaries and portable debug symbols
        ".pdb",
        ".mdb",
        ".bin",

        // ignore unity specifics
        ".meta",
        ".physicsMaterial2D",
        ".sceneWithBuildSettings",
        ".buildconfiguration",
        ".asset",
        ".mat",
        ".prefab",
        ".unity",
        ".asmdef",


        // ignore code and binaries
        ".cs",
        ".dll",
        ".dylib",
        ".so",
        
        ".txt",
        ".pak",
        ".pdf",


        // ignore mac os specifics
        ".icns",
        ".plist",

        // shared cyrrently is not support for packing
        ".cginc",
        ".compute",
        ".shader",

        // fonts currently is not supported for packing
        ".ttf",
        ".woff2",

        // psd too
        ".psd",
    };
    
    public ConcurrentBag<VdxEntity> VdxEntities = new ConcurrentBag<VdxEntity>();

    public void CollectFiles(DirectoryInfo contentFolder)
    {
        var groups = contentFolder.GetDirectories();


        foreach (var group in groups)
        {
            CollectAudio(group);
            CollectModels(group);
            CollectTextures(group);
        }

        CollectNoPackEntities(contentFolder);
        BumpNoPackedEntities(contentFolder);
        //new Crc32().
    }


    public void BumpNoPackedEntities(DirectoryInfo contentFolder)
    {
        var ignored = contentFolder.GetFiles("*.*", SearchOption.AllDirectories)
            .Where(x => !TEXTURE_EXT.Contains(x.Extension, StringComparer.InvariantCultureIgnoreCase))
            .Where(x => !MODEL_EXT.Contains(x.Extension, StringComparer.InvariantCultureIgnoreCase))
            .Where(x => !AUDIO_EXT.Contains(x.Extension, StringComparer.InvariantCultureIgnoreCase))
            .Where(x => !NO_PACK_EXT.Contains(x.Extension, StringComparer.InvariantCultureIgnoreCase))
            .Where(x => !IGNORED_EXT.Contains(x.Extension, StringComparer.InvariantCultureIgnoreCase))
            .Where(x => !x.Extension.Contains("~"))
            .Where(x => !x.Attributes.HasFlag(FileAttributes.Hidden));


        foreach (var info in ignored)
        {
            AnsiConsole.Markup($"File '");
            AnsiConsole.Write(new TextPath(info.FullName));
            AnsiConsole.MarkupLine("' has been [red]ignored[/]!");
        }
            
    }

    public void CollectAudio(DirectoryInfo dir) =>
        CollectFor(dir, AUDIO_EXT, "audio");
    public void CollectModels(DirectoryInfo dir) =>
        CollectFor(dir, MODEL_EXT, "models");
    public void CollectTextures(DirectoryInfo dir) =>
        CollectFor(dir, TEXTURE_EXT, "textures");
    public void CollectNoPackEntities(DirectoryInfo dir) =>
        CollectFiles(dir.GetFiles("*.*", SearchOption.AllDirectories)
            .Where(x => NO_PACK_EXT.Contains(x.Extension, StringComparer.InvariantCultureIgnoreCase)).ToList(), null);

    public void CollectFor(DirectoryInfo dir, List<string> exts, string? group = null) =>
        CollectFiles(dir.GetFiles("*.*", SearchOption.AllDirectories)
            .Where(x => exts.Contains(x.Extension, StringComparer.InvariantCultureIgnoreCase)).ToList(), group ?? dir.Name);


    public void CollectFiles(List<FileInfo> files, string? group)
    {
        foreach (var file in files)
        {
            var meta = GetMetaFor(file);

            VdxEntities.Add(new VdxEntity()
            {
                Crc32 = -1,
                Group = group?.ToLowerInvariant() ?? "NO+PACK",
                ID = Guid.NewGuid(),
                Size = (ulong)file.Length,
                Tags = meta?.Tags ?? new (),
                UtfName = file.Name,
                OriginalEntityPath = file.FullName
            });
        }
    }

    public EntityTextMeta? GetMetaFor(FileInfo file)
    {
        var metafile = new FileInfo($"{file.FullName}.vxmeta");

        if (!metafile.Exists)
            return null;
        return JsonConvert.DeserializeObject<EntityTextMeta>(File.ReadAllText(metafile.FullName));
    }
}

public class EntityTextMeta
{
    public string TargetFileName;
    public List<string> Tags = new List<string>();
}

public record struct VdxEntity
{
    public Guid ID;
    public string UtfName;
    public ulong Size;
    public string Group;
    public List<string> Tags;
    public long Crc32;
    public string OriginalEntityPath;
}