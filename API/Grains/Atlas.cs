﻿using API.DTOs;

namespace API.Grains;

public sealed class Atlas : Grain, IAtlas
{
    private readonly ILogger _logger;
    private int _batteryLevel;

    public Atlas(ILogger<Atlas> logger)
    {
        _logger = logger;
    }

    public Task<int> GetBatteryLevel()
    {
        _batteryLevel++;
        return Task.FromResult(_batteryLevel);
    }

    public Task ReadMsg(string msg)
    {
        _batteryLevel = new Random().Next(1, 100);
        return Task.CompletedTask;
    }

    public Task<string> SayHello(string greeting)
    {
        Console.WriteLine($"Hello from {IdentityString} with msg: {greeting}");

        //_logger.LogInformation("""
        //    SayHello message received on ID: greeting = "{Greeting}"
        //    """,
        //    greeting);

        return Task.FromResult($"""
            Client said: "{greeting}", so HelloGrain says: Hello!
            """);
    }

    public Task ProcessMsg(object msg)
    {
        if (msg is AtlasUpdate au)
        {
            Console.WriteLine($"Battery on {IdentityString}: {au.Battery}");
        }
        else
        {
            Console.WriteLine("Message type not recognized");
        }

        return Task.CompletedTask;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"Activating Grain {IdentityString}");
        await base.OnActivateAsync(cancellationToken);
    }
}


[GenerateSerializer, Alias(nameof(AtlasDetails))]
public sealed record class AtlasDetails
{
    [Id(0)]
    public string Imei { get; set; } = "";

    [Id(1)]
    public int Battery { get; set; }
}
