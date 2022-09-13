namespace Invocative.VDX;

public class VdxDirectoryInfo
{
    public string Group { get; set; }
    public List<VdxEntity> Entities { get; set; }
    public Dictionary<int, List<Guid>> ChunkInfo { get; set; }
}