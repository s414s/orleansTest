using API.DTOs;

namespace API.Grains;

public sealed class Atlas : Grain, IAtlas
{
    private readonly ILogger _logger;
    private int _batteryLevel;
    private readonly IPersistentState<AtlasState> _atState;
    //The profile state will not be loaded at the time it is injected into the constructor, so accessing it is invalid at that time.The state will be loaded before OnActivateAsync is called.
    public Atlas(
        ILogger<Atlas> logger,
        [PersistentState("atlasState", "AtlasStateStorageProvider")] IPersistentState<AtlasState> atState
        )
    {
        _logger = logger;
        _atState = atState;
    }

    public Task<int> GetBatteryLevel()
    {
        //_batteryLevel++;
        //return Task.FromResult(_batteryLevel);
        return Task.FromResult(_atState.State.Battery);
    }

    public Task ReadMsg(string msg)
    {
        _batteryLevel = new Random().Next(1, 100);
        return Task.CompletedTask;
    }

    public async Task UpdateFromRabbit(RabbitMQMessage msg)
    {
        Console.WriteLine($"RabbitMQ msg received on {IdentityString} with bat: {msg.Battery}");

        _atState.State.Battery = msg.Battery;
        await _atState.WriteStateAsync();
    }


    public async Task<string> SayHello(string greeting)
    {
        Console.WriteLine($"Hello from {IdentityString} with msg: {greeting}");

        Console.WriteLine($"Battery BEFORE {_atState.State.Battery}");

        var level = new Random().Next(1, 100);
        _atState.State.Battery = level;
        await _atState.WriteStateAsync();

        Console.WriteLine($"Battery AFTER {_atState.State.Battery}");

        //_logger.LogInformation("""
        //    SayHello message received on ID: greeting = "{Greeting}"
        //    """,
        //    greeting);

        return $"""
            Client said: "{greeting}", so HelloGrain says: Hello!
            """;
    }

    public async Task ProcessMsg(object msg)
    {
        if (msg is AtlasUpdate au)
        {
            Console.WriteLine($"Battery on {IdentityString}: {au.Battery}");

            _atState.State.Battery = au.Battery;
            await _atState.WriteStateAsync();
        }
        else
        {
            Console.WriteLine("Message type not recognized");
        }
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"Activating Grain {IdentityString}");
        await base.OnActivateAsync(cancellationToken);
    }

    public Task<AtlasState> GetState()
    {
        return Task.FromResult(_atState.State);
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
