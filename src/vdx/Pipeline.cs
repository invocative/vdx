namespace Invocative.VDX;

using System;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Rendering;

public static class _
{
    public static DirectoryInfo Assets = new("C:\\git\\ElementarySandbox.V2\\Assets");
    public static void Main(string[] args)
    {
        var ctn = new DirectoryInfo("./content");

        ctn.Delete(true);
        var p = new VdxAssembler(Assets, ctn, "ElementarySandbox");
        var textures = new List<FileInfo>();

        foreach (var png in Assets.GetFiles("*.png", SearchOption.AllDirectories)) textures.Add(png);
        foreach (var png in Assets.GetFiles("*.jpg", SearchOption.AllDirectories)) textures.Add(png);

        var size = textures.Max(x => x.Length);


        foreach (var info in textures) p.AddTexture(info);


        p.Seal().FlushToDisk();

    }
}


public static class VDXConstants
{
    public const string VDX_TEXTURE_GROUP = "textures";
    public const string VDX_AUDIO_GROUP = "audio";
    public const string VDX_MODEL_GROUP = "models";
    public const string VDX_CONFIG_GROUP = "config";
    public const string VDX_NONE_GROUP = "<root>";


    public const int VDX_CHUNK_SIZE = 200 * 1024 * 1024;
    
    
    public static List<string> TEXTURE_EXT = new()
    {
        ".png",
        ".bmp",
        ".dds",
        ".jpg"
    };

    public static List<string> MODEL_EXT = new()
    {
        ".fbx", // autodesk fbx
        ".obj",
        ".gltf" // opengl scene 
    };

    public static List<string> AUDIO_EXT = new()
    {
        ".wav",
    };
}

public class VdxArchive
{
    
}


public class VirtualFile
{
    public Guid ID { get; }
    public VdxEntity Entity { get; }
    public Uri Path { get; }

    private VdxRepository _vdx;
    public VirtualFile(VdxRepository self, Uri path, VdxEntity entity) 
        => (_vdx, Path, ID, Entity) = (self, path, entity.ID, entity);

    
}

public class VdxRepository
{
    private readonly DirectoryInfo _contentFolder;
    private readonly Dictionary<string, VdxDirectoryInfo> _tables = new ();
    private readonly Dictionary<string, string> _tables_pathes = new ();

    private readonly Dictionary<Guid, VirtualFile> _entities = new ();

    private bool isOpened;

    public VdxRepository(DirectoryInfo contentFolder)
    {
        _contentFolder = contentFolder;

        if (!_contentFolder.Exists)
            throw new DirectoryNotFoundException($"{_contentFolder.FullName} is not exist.");
    }

    public VirtualFile Get(Uri path)
    {
        var @namespace = path.Segments.Length == 1 ? VDXConstants.VDX_NONE_GROUP : path.Host;
        if (!_tables.ContainsKey(@namespace))
            throw new ArgumentException($"No '{@namespace}' namespace found.");
        var table = _tables[@namespace];
        var folder = _tables_pathes[@namespace];

        if (!_entities.ContainsKey(folder))
            throw new ArgumentException($"No loaded entities.");
    }


    public async Task OpenAsync()
    {
        if (isOpened) return;
        var tables = _contentFolder.GetFiles("*.vft", SearchOption.AllDirectories);

        if (tables.Length == 0)
            throw new Exception("No any vft files found!");

        foreach (var info in tables)
        {
            var content = await File.ReadAllTextAsync(info.FullName);
            var obj = JsonConvert.DeserializeObject<VdxDirectoryInfo>(content);
            if (obj is null)
                throw new Exception("");
            _tables.Add(obj.Group, obj);
            _tables_pathes.Add(obj.Group, info.Directory.FullName);
        }

        isOpened = true;
    }
}