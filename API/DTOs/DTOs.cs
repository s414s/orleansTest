namespace API.DTOs;

public record AtlasUpdate(string Imei, int Battery);

public record AtlasSnapshot(string Imei, int Battery);

[GenerateSerializer]
public class AtlasState
{
    //public string? Imei { get; set; }
    public int Battery { get; set; }
};

[GenerateSerializer]
public record RabbitMQMessage(string Imei, int Battery);
