using API.DTOs;
using Orleans.Streams;

namespace API.Grains;

//https://github.com/OrleansContrib/SignalR.Orleans
public sealed class WsGrain : Grain, IWsGrain, IAsyncObserver<AtlasChangeEvent>
{
    private readonly Dictionary<string, Pt> _points;

    public WsGrain()
    {
        _points = [];
    }

    // When the grain is activated (created/loaded), this method is automatically called
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // Always call the base activation first
        await base.OnActivateAsync(cancellationToken);

        // Get the IMEI from the grain's key
        // If you created the grain with GrainFactory.GetGrain<IWsGrain>("device123"),
        // then "device123" is the primary key

        //await SubscribeToAtlasStream(this.GetPrimaryKeyString());
    }

    public async Task SubscribeToAtlasStream(string imei)
    {
        var streamProvider = this.GetStreamProvider("StreamProvider");
        var streamId = StreamId.Create("AtlasChange", imei);
        var stream = streamProvider.GetStream<AtlasChangeEvent>(streamId);
        await stream.SubscribeAsync(this);
    }

    public Task OnCompletedAsync()
    {
        Console.WriteLine($"Stream COMPLETED");
        return Task.CompletedTask;
    }

    public Task OnErrorAsync(Exception ex)
    {
        Console.WriteLine($"Stream error: {ex.Message}");
        return Task.CompletedTask;
    }

    public Task OnNextAsync(AtlasChangeEvent item, StreamSequenceToken? token = null)
    {
        if (this.GetPrimaryKeyString() == "GeneralWS")
        {
            Console.WriteLine($"G_WS => Imei {item.Imei} \t {item.Long} \t {item.Lat} \t {item.Color}");
        }

        _points[item.Imei] = new Pt
        {
            Name = item.Imei,
            Lat = item.Lat,
            Lng = item.Long,
            C = item.Color == "RED" ? 'R' : 'G',
        };

        return Task.CompletedTask;
    }

    public Task<List<Pt>> GetAllPoints()
    {
        return Task.FromResult(_points.Select(x => x.Value).ToList());
    }
}

public interface IWsGrain : IGrainWithStringKey
{
    Task SubscribeToAtlasStream(string imei);
    Task<List<Pt>> GetAllPoints();
}
