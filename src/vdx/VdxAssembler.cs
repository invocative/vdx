namespace Invocative.VDX;

using System.Collections.Concurrent;
using System.IO.Hashing;
using System.Text;
using Newtonsoft.Json;

public class VdxAssembler
{
    public const int VDX_VERSION = 1;

    private readonly DirectoryInfo _rootFolder;
    private readonly DirectoryInfo _contentFolder;
    private readonly string _gameName;
    private readonly ConcurrentBag<VdxEntity> _vdxEntities = new();


    // TODO replace to event
    private Action<string> _log = (_) => {};
    private Action<string> _warn = (_) => {};
    private Action<string> _err = (_) => {};
    public void SetLogActions(Action<string> log, Action<string> warn, Action<string> error) 
        => (_log, _warn, _err) = (log, warn, error);
    
    private bool isSealed;

    public VdxAssembler(DirectoryInfo rootFolder, DirectoryInfo contentFolder, string gameName)
    {
        _rootFolder = rootFolder;
        _contentFolder = contentFolder;
        _gameName = gameName;
    }


    public VdxAssembler AddTexture(FileInfo file)
        => AddEntity(file, VDXConstants.VDX_TEXTURE_GROUP);
    public VdxAssembler AddAudio(FileInfo file)
        => AddEntity(file, VDXConstants.VDX_AUDIO_GROUP);
    public VdxAssembler AddModel(FileInfo file)
        => AddEntity(file, VDXConstants.VDX_MODEL_GROUP);
    public VdxAssembler AddConfig(FileInfo file)
        => AddEntity(file, VDXConstants.VDX_CONFIG_GROUP);
    public VdxAssembler AddRaw(FileInfo file)
        => AddEntity(file, VDXConstants.VDX_NONE_GROUP);
    public VdxAssembler AddGeneric(FileInfo file, string groupName)
        => AddEntity(file, groupName);



    public VdxAssembler Seal()
    {
        isSealed = true;
        _contentFolder.CreateIfNotExist();

        _log("[[[blue]VDX[/]]] [green]Success sealed pipeline[/]");

        foreach (var entity in _vdxEntities.AsParallel())
        {
            var hash = Crc64.Hash(ReadMeatForHash(new(entity.OriginalEntityPath)));
            entity.Crc64 = BitConverter.ToInt64(hash);
            _log($"[[[orange3]CRC64[/]]] Generated hash for '{entity.UtfName}': [yellow]{hash.Select(x => $"{x:X}").Join(" ")}[/].");
        }

        return this;
    }


    public void FlushToDisk()
    {
        if (!isSealed) throw new Exception();

        _log($"[[[blue]VDX[/]]] Flushing to disk...");

        var groups = _vdxEntities.GroupBy(x => x.Group);
        _log($"[[[blue]VDX[/]]] Total groups: {groups.Count()}");
        var chunks = new List<(string group, Dictionary<int, List<VdxEntity>>)>();
        foreach (var group in groups)
            chunks.Add(PrepareChunks(group));
        _log($"[[[blue]VDX[/]]] Total chunks: {chunks.Sum(x => x.Item2.Count)}");
        foreach (var (group, entities) in chunks)
            WriteVirtualFileTables(group,
                group == VDXConstants.VDX_NONE_GROUP ?
                    _contentFolder : _contentFolder.Directory(group),
                entities);
        foreach (var (group, entities) in chunks)
            WriteFileChunks(
                group == VDXConstants.VDX_NONE_GROUP ?
                    "resources" : group,
                group == VDXConstants.VDX_NONE_GROUP ?
                    _contentFolder : _contentFolder.Directory(group),
                entities);
    }


    private void WriteVirtualFileTables(string group, DirectoryInfo targetFolder, Dictionary<int, List<VdxEntity>> chunks)
    {
        targetFolder.CreateIfNotExist();
        var file = targetFolder.File(".vft");
        var meta = new VdxDirectoryInfo()
        {
            ChunkInfo = chunks.ToDictionary(x => x.Key, x => x.Value.Select(z => z.ID).ToList()),
            Entities = chunks.SelectMany(x => x.Value).ToList(),
            Group = group
        };
        var content = JsonConvert.SerializeObject(meta, Formatting.Indented);
        _log($"[[[blue]VDX[/]]] Virtual file table has been [green]success[/] writed to '{file.FullName}'");

        File.WriteAllText(file.FullName, content);
    }

    private void WriteFileChunks(string name, DirectoryInfo targetFolder, Dictionary<int, List<VdxEntity>> chunks)
    {
        foreach (var chunk in chunks.AsParallel())
            WriteFileChunk(name, targetFolder, chunk.Value, chunk.Key);
    }

    private void WriteFileChunk(string name, DirectoryInfo targetFolder, List<VdxEntity> @in, int index)
    {
        var file = targetFolder.File($"{name}_{index:00}.vdx");

        using var stream = file.OpenWrite();
        using var writer = new BinaryWriter(stream);

        var entities = @in.AsReadOnly();

        WriteHeader(writer, entities);

        foreach (var entity in entities)
            WriteContent(writer, entity);

        _log($"[[[blue]VDX[/]]] Data bank has been [green]success[/] writed to '{file.FullName}'");
        writer.Flush();
        stream.Flush(true);

        writer.Close();
        stream.Close();
    }


    private void WriteHeader(BinaryWriter writer, IReadOnlyCollection<VdxEntity> entities)
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


    private void WriteContent(BinaryWriter writer, VdxEntity entity)
    {
        using var fsRead = File.OpenRead(entity.OriginalEntityPath);
        var bufferSize = 2048;
        var buffer = new byte[bufferSize];
        var leftLength = fsRead.Length;
        var targetSize = 0;
        var offset = 0L;

        while (leftLength > 0)
        {
            fsRead.Position = offset;
            if (leftLength < bufferSize)
                targetSize = fsRead.Read(buffer, 0, (int)leftLength);
            else
                targetSize = fsRead.Read(buffer, 0, bufferSize);
            if (targetSize == 0) break;

            offset += targetSize;
            leftLength -= targetSize;

            writer.Write(buffer);
        }
    }

    private (string group, Dictionary<int, List<VdxEntity>>) PrepareChunks(IGrouping<string, VdxEntity> group)
    {
        var chunks = new Dictionary<int, List<VdxEntity>>();


        chunks.Add(0, new List<VdxEntity>());
        var currentChunkIndex = 0;
        var currentTotalSize = 0ul;

        foreach (var entity in group)
        {
            if (entity.Size > VDXConstants.VDX_CHUNK_SIZE)
                throw new NotSupportedException($"File size great 200mb not supported, file: {entity.OriginalEntityPath}");

            currentTotalSize += entity.Size;
            chunks[currentChunkIndex].Add(entity);

            if (currentTotalSize >= VDXConstants.VDX_CHUNK_SIZE)
            {
                currentChunkIndex++;
                chunks[currentChunkIndex] = new();
                currentTotalSize = 0;
            }
        }

        return (group.Key, chunks);
    }


    private byte[] ReadMeatForHash(FileInfo info)
    {
        if (info.Length < 16)
            return File.ReadAllBytes(info.FullName);

        using var reader = info.OpenRead();
        var fb = new byte[2];
        var mb = new byte[4];
        var eb = new byte[2];

        reader.Read(fb, 0, fb.Length);
        reader.Position = (info.Length / 2);
        reader.Read(mb, 0, mb.Length);
        reader.Position = (info.Length - eb.Length);
        reader.Read(eb, 0, eb.Length);

        return fb.Union(mb).Union(eb).ToArray();
    }

    private VdxAssembler AddEntity(FileInfo file, string group)
    {
        if (isSealed) throw new Exception("VDX Repository has been sealed");
        _vdxEntities.Add(new VdxEntity()
        {
            UtfName = file.Name,
            Group = group,
            ID = Guid.NewGuid(),
            OriginalEntityPath = file.FullName,
            EntityPath = file.FullName.Replace(_rootFolder.FullName, ""),
            Crc64 = -1,
            Size = (ulong)file.Length,
            Tags = new List<string>()
        });
        return this;
    }
}