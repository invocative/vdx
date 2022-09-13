namespace Invocative.VDX;

using Newtonsoft.Json;

public record class VdxEntity
{
    public Guid ID;
    public string UtfName;
    public ulong Size;
    public string Group;
    public List<string> Tags;
    public long Crc64;
    [JsonIgnore]
    public string OriginalEntityPath;
    public string EntityPath;
}