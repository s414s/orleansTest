namespace API.DTOs;

public record AtlasUpdate(string Imei, int Battery);

public record AtlasSnapshot(string Imei, int Battery);

[GenerateSerializer]
public class AtlasState
{
    //public string? Imei { get; set; }
    [Id(0)]
    public int Battery { get; set; }

    [Id(1)]
    public double Long { get; set; }

    [Id(3)]
    public double Lat { get; set; }
};

[GenerateSerializer]
public record RabbitMQMessage
{
    [Id(0)]
    public required string Imei { get; init; }
    [Id(1)]
    public required double Long { get; init; }
    [Id(2)]
    public required double Lat { get; init; }
    [Id(3)]
    public required int Battery { get; init; }
}

[GenerateSerializer]
public record AtlasChangeEvent
{
    [Id(0)]
    public required string Imei { get; init; }

    [Id(1)]
    public required double Long { get; init; }

    [Id(2)]
    public required double Lat { get; init; }

    [Id(3)]
    public required string Color { get; init; }
}

