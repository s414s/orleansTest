using API.DTOs;

namespace API.Grains;

[KeepAlive]
public sealed class Atlas : Grain, IAtlas
{
    //private readonly ILogger _logger;
    private int _batteryLevel;
    private readonly IPersistentState<AtlasState> _atState;

    //The profile state will not be loaded at the time it is injected into the constructor, so accessing it is invalid at that time.The state will be loaded before OnActivateAsync is called.
    public Atlas(
        //ILogger<Atlas> logger,
        [PersistentState("atlasState", "AtlasStateStorageProvider")] IPersistentState<AtlasState> atState
        )
    {
        //_logger = logger;
        _atState = atState;
    }

    public Task<int> GetBatteryLevel()
    {
        return Task.FromResult(_atState.State.Battery);
    }

    public async ValueTask UpdateFromRabbit(RabbitMQMessage msg)
    {
        //Console.WriteLine($"RabbitMQ msg received on {IdentityString} battery: {msg.Battery}");
        _atState.State.Battery = msg.Battery;
        _atState.State.Long = Math.Round(msg.Long, 10);
        _atState.State.Lat = Math.Round(msg.Lat, 10);

        await _atState.WriteStateAsync();

        var changeEvent = new AtlasChangeEvent
        {
            Imei = this.GetPrimaryKeyLong(),
            Long = _atState.State.Long,
            Lat = _atState.State.Lat,
            Color = _atState.State.Battery > 50 ? 'G' : 'R',
        };

        var grain = GrainFactory.GetGrain<IWsGrain>(0);
        await grain.GetAtlasChangeEvent(changeEvent);
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

    public Task<AtlasState> GetState()
    {
        return Task.FromResult(_atState.State);
    }
}
