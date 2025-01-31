using API.DTOs;
using Orleans.Streams;

namespace API.Grains;

public sealed class Atlas : Grain, IAtlas
{
    private readonly ILogger _logger;
    private int _batteryLevel;
    private readonly IPersistentState<AtlasState> _atState;
    private IAsyncStream<AtlasChangeEvent>? _stream;
    private string _streamProvider = "StreamProvider";

    //The profile state will not be loaded at the time it is injected into the constructor, so accessing it is invalid at that time.The state will be loaded before OnActivateAsync is called.
    public Atlas(
        ILogger<Atlas> logger,
        [PersistentState("atlasState", "AtlasStateStorageProvider")] IPersistentState<AtlasState> atState
        )
    {
        _logger = logger;
        _atState = atState;
        //_batteryLevel = 0;
    }

    public Task<int> GetBatteryLevel()
    {
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
        _atState.State.Long = Math.Round(msg.Long, 10);
        _atState.State.Lat = Math.Round(msg.Lat, 10);

        await _atState.WriteStateAsync();

        var changeEvent = new AtlasChangeEvent
        {
            Imei = this.GetPrimaryKeyString(),
            Long = _atState.State.Long,
            Lat = _atState.State.Lat,
            Color = _atState.State.Battery > 50 ? "GREEN" : "RED",
        };

        if (_stream is not null)
        {
            await _stream.OnNextAsync(changeEvent);
        }
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
            .GetStream<AtlasChangeEvent>(StreamId.Create("AtlasChange", "ALL"));
    }

    public Task<AtlasState> GetState()
    {
        return Task.FromResult(_atState.State);
    }
}
