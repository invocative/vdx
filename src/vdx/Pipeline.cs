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

    public const int VDX_VERSION = 1;

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


public class VirtualFile : IAsyncDisposable
{
    public Guid ID { get; }
    public VdxEntityWithPos Entity { get; }
    public Uri Path { get; }

    internal VirtualFile(VdxRepository self, Uri path, VdxEntityWithPos entity, Func<Task> OnUnload, FileInfo bundle)
    {
        _onUnload = OnUnload;
        _bundle = bundle;
        (_vdx, Path, ID, Entity) = (self, path, entity.ID, entity);
    }

    private readonly VdxRepository _vdx;
    private MemoryStream _stream;
    private readonly Func<Task> _onUnload;
    private readonly FileInfo _bundle;
    private bool isLoaded;


    public async Task LoadAsync()
    {
        if (isLoaded)
            throw new Exception("Virtual File state exception, cannot load object because object is already loaded.");
        using var file = _bundle.OpenRead();
        if (Entity.Size < 200 * 1024 * 1024)
        {
            var buffer = new byte[Entity.Size];
            file.Position = Entity.pos; // TODO
            await file.ReadAsync(buffer, 0, buffer.Length);
            _stream = new MemoryStream(buffer);
        }
        else
            throw new NotImplementedException($"Entity.Size > 200 * 1024 * 1024");
        
        isLoaded = true;
    }

    public async ValueTask UnloadAsync()
    {
        if (!isLoaded)
            throw new Exception("Virtual File state exception, cannot unload object because object is not loaded.");
        await _stream.DisposeAsync();
        await _onUnload();
        isLoaded = false;
    }

    public ValueTask DisposeAsync() => UnloadAsync();
}

public class VdxRepository
{
    private readonly DirectoryInfo _contentFolder;
    private readonly Dictionary<string, VdxDirectoryInfo> _tables = new ();
    private readonly Dictionary<string, DirectoryInfo> _tables_pathes = new ();

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

        var entity = table.Entities.Find(x => x.EntityPath.Equals(path.LocalPath, StringComparison.InvariantCultureIgnoreCase));

        if (entity is null)
            throw new Exception($"File '{path.LocalPath}' is not found in '{@namespace}' bundles.");

        if (!_entities.ContainsKey(entity.ID))
            throw new ArgumentException($"No loaded entities.");

        return _entities[entity.ID];
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
            _tables_pathes.Add(obj.Group, info.Directory);
        }

        isOpened = true;
    }

    public async Task LoadAsync(IProgress<VdxLoadState>? progress)
    {
        if (!isOpened) return;
        progress ??= new DefaultReporter();

        progress.Report(new VdxLoadState()
        {
            TotalEntities = _tables.Sum(x => x.Value.Entities.Count),
            CurrentIndex = 0
        });

        var allBundles = _tables.SelectMany(x => _tables_pathes[x.Key].GetFiles("*.vdx"));


        foreach (var bundle in allBundles.AsParallel())
        {
            using var file = bundle.OpenRead();
            using var reader = new BinaryReader(file);
            var entities = VdxFormat.ReadHeader(reader);

            new VirtualFile(this, new Uri(), )
        }
    }


    private async Task ReadBundle(FileInfo bundleFile)
    {

    }

    public struct VdxLoadState
    {
        public long TotalEntities;
        public long CurrentIndex;
        public string? Text;
    }

    private class DefaultReporter : IProgress<VdxLoadState>
    {
        public void Report(VdxLoadState value) {}
    }
}

/*
 * private void WriteHeader(BinaryWriter writer, IReadOnlyCollection<VdxEntity> entities)
    {
        writer.Write(Encoding.ASCII.GetBytes("VDX"));
        writer.Write(VDX_VERSION);
        writer.Write(Encoding.ASCII.GetBytes(_gameName));
        writer.Write(new byte[2] { 0xFF, 0xFF });


        writer.Write(entities.Count);
        writer.Write(new byte[] { 0xF1 });
        foreach (var entity in entities)
        {
            // 16 bytes
            writer.Write(entity.ID.ToByteArray());

            writer.WriteVdxString(entity.UtfName);
            writer.WriteVdxString(entity.EntityPath);
            writer.WriteVdxString(entity.Group);
            writer.Write(entity.Crc64);
            writer.Write(entity.Size);
        }
        writer.Write(new byte[2] { 0xFF, 0xFF });
    }
 */

public record VdxEntityWithPos(long pos) : VdxEntity;

public static class VdxFormat
{
    public static List<VdxEntityWithPos> ReadHeader(BinaryReader _reader)
    {
        if (_reader.ReadASCIIString(4) != "VDX0")
            throw new Exception("Bad format");
        var list = new List<VdxEntityWithPos>();
        var ver = _reader.ReadInt32();
        var game = _reader.ReadASCIIString(_reader.ReadInt32());

        if (VDXConstants.VDX_VERSION != ver)
            throw new Exception("version is not matched");
        _reader.Skip(64);
        for (int i = 0; i != _reader.ReadInt32(); i++)
        {
            var id = _reader.ReadGuid();
            var utfName = _reader.ReadVdxString();
            var path = _reader.ReadVdxString();
            var group = _reader.ReadVdxString();
            var crc64 = _reader.ReadInt64();
            var size = _reader.ReadUInt64();
            var pos = _reader.ReadInt64();
            
            list.Add(new VdxEntityWithPos(pos)
            {
                UtfName = utfName,
                Size = size,
                ID = id,
                Group = group,
                EntityPath = path,
                Crc64 = crc64
            });
        }

        return list;
    }
}