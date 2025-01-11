namespace API.DTOs;

public record AtlasUpdate(string Imei, int Battery);

public record AtlasSnapshot(string Imei, int Battery);

[Serializable]
public class AtlasState
{
    public string Imei { get; set; }
    public int Battery { get; set; }
};
