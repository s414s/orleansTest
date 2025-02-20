namespace API.DTOs;

[GenerateSerializer]
public record AtlasUpdate
{
    [Id(0)]
    public string Imei { get; set; } = "";

    [Id(1)]
    public int Battery { get; set; }

};

[GenerateSerializer]
public class AtlasState
{
    //public string? Imei { get; set; }
    [Id(0)]
    public int Battery { get; set; }

    [Id(1)]
    public double Long { get; set; }

    [Id(2)]
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
    public required long Imei { get; init; }

    [Id(1)]
    public required double Long { get; init; }

    [Id(2)]
    public required double Lat { get; init; }

    [Id(3)]
    public required char Color { get; init; }
}

// =======================================
// =======================================

[GenerateSerializer]
public class Pt
{
    [Id(0)]
    public required double Lat { get; set; }

    [Id(1)]
    public required double Lng { get; set; }

    [Id(2)]
    public required string Imei { get; set; }

    [Id(3)]
    public required char C { get; set; }
};

