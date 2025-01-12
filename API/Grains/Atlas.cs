using API.DTOs;
using Orleans.Streams;

namespace API.Grains;

public sealed class Atlas : Grain, IAtlas
{
    private readonly ILogger _logger;
    private int _batteryLevel;
    private readonly IPersistentState<AtlasState> _atState;
    private IAsyncStream<AtlasChangeEvent> _stream;
    private IAsyncStream<AtlasChangeEvent> _generalStream;
    private string _streamProvider = "StreamProvider";

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
        //Console.WriteLine($"RabbitMQ msg received on {IdentityString} battery: {msg.Battery}");

        _atState.State.Battery = msg.Battery;
        _atState.State.Long = msg.Long;
        _atState.State.Lat = msg.Lat;

        await _atState.WriteStateAsync();

        var changeEvent = new AtlasChangeEvent
        {
            Color = _atState.State.Battery > 50 ? "GREEN" : "RED",
            Long = _atState.State.Battery,
            Lat = _atState.State.Battery,
            Imei = this.GetPrimaryKeyString(),
        };

        // Console.WriteLine($"Sending to stream: {changeEvent.Color}");
        await _stream.OnNextAsync(changeEvent);
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
        await base.OnActivateAsync(cancellationToken);

        // Initialize the stream
        _stream = this.GetStreamProvider(_streamProvider)
            .GetStream<AtlasChangeEvent>(StreamId.Create("AtlasChange", this.GetPrimaryKeyString()));

        // Stream Individual
        var wsGrain = GrainFactory.GetGrain<IWsGrain>(this.GetPrimaryKeyString());
        await wsGrain.SubscribeToAtlasStream(this.GetPrimaryKeyString());

        var generalWSGrain = GrainFactory.GetGrain<IWsGrain>("GeneralWS");
        await generalWSGrain.SubscribeToAtlasStream(this.GetPrimaryKeyString());

        //Console.WriteLine($"Activating Grain {IdentityString}");
    }

    public Task<AtlasState> GetState()
    {
        return Task.FromResult(_atState.State);
    }
}
