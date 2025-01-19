using API.DTOs;
using API.Hubs;
using Microsoft.AspNetCore.SignalR;
using Orleans.Streams;

namespace API.Grains;

//https://github.com/OrleansContrib/SignalR.Orleans
public sealed class WsGrain : Grain, IWsGrain, IAsyncObserver<AtlasChangeEvent>
{
    private readonly Dictionary<string, Pt> _points;
    private readonly IHubContext<ViewportHub, IViewportClient> _hub;
    private readonly HashSet<string> _connections = [];
    private IAsyncStream<AtlasChangeEvent>? _stream;
    private string _streamProvider = "StreamProvider";
    private StreamSubscriptionHandle<AtlasChangeEvent>? _subscription;

    public WsGrain(IHubContext<ViewportHub, IViewportClient> hub)
    {
        _points = [];
        _hub = hub;
    }

    // When the grain is activated (created/loaded), this method is automatically called
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // Always call the base activation first
        await base.OnActivateAsync(cancellationToken);

        // Get the IMEI from the grain's key
        // If you created the grain with GrainFactory.GetGrain<IWsGrain>("device123") then "device123" is the primary key

        // Testing this
        //if (this.GetPrimaryKeyString() == "GeneralWS")
        //{
        //    var streamProvider = this.GetStreamProvider("StreamProvider");
        //    var streamId = StreamId.Create("AtlasChange", "ALL");
        //    _stream = streamProvider.GetStream<AtlasChangeEvent>(streamId);
        //    await _stream.SubscribeAsync(this);
        //}

        _stream = this.GetStreamProvider(_streamProvider)
                .GetStream<AtlasChangeEvent>(StreamId.Create("AtlasChange", "ALL"));

        //await SubscribeToAtlasStream(this.GetPrimaryKeyString());
    }

    public async Task AddConnection(string connectionId)
    {
        _connections.Add(connectionId);

        // Send current state to new connection
        var points = await GetAllPoints();
        await _hub.Clients.Client(connectionId)
            .InitializeState(points);
    }

    private async Task OnNextNumber(AtlasChangeEvent evt, StreamSequenceToken? token)
    {
        //if (this.GetPrimaryKeyString() == "GeneralWS" && _connections.Any())
        Console.WriteLine($"G_WS => Imei {evt.Imei} \t {evt.Long} \t {evt.Lat} \t {evt.Color}");
        await _hub.Clients.All.SendStateChange(evt);

        _points[evt.Imei] = new Pt
        {
            Name = evt.Imei,
            Lat = evt.Lat,
            Lng = evt.Long,
            C = evt.Color == "RED" ? 'R' : 'G',
        };
    }

    public Task RemoveConnection(string connectionId)
    {
        _connections.Remove(connectionId);
        return Task.CompletedTask;
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

    public async Task OnNextAsync(AtlasChangeEvent item, StreamSequenceToken? token = null)
    {
        if (this.GetPrimaryKeyString() == "GeneralWS" && _connections.Any())
        {
            Console.WriteLine($"G_WS => Imei {item.Imei} \t {item.Long} \t {item.Lat} \t {item.Color}");
            await _hub.Clients.All.SendStateChange(item); // TODO - make async?
        }

        _points[item.Imei] = new Pt
        {
            Name = item.Imei,
            Lat = item.Lat,
            Lng = item.Long,
            C = item.Color == "RED" ? 'R' : 'G',
        };
    }

    public Task<List<Pt>> GetAllPoints()
    {
        return Task.FromResult(_points.Select(x => x.Value).ToList());
    }

    public async Task StartConsuming()
    {
        if (_stream == null)
            return;

        _subscription = await _stream.SubscribeAsync(OnNextNumber);
        Console.WriteLine($"Consumer {this.GetPrimaryKeyString()}: Started consuming");
    }

    public async Task StopConsuming()
    {
        if (_subscription != null)
        {
            await _subscription.UnsubscribeAsync();
            _subscription = null;
            Console.WriteLine($"Consumer {this.GetPrimaryKeyString()}: Stopped consuming");
        }
    }
}

public interface IWsGrain : IGrainWithStringKey
{
    Task<List<Pt>> GetAllPoints();
    Task AddConnection(string connectionId);
    Task RemoveConnection(string connectionId);
    Task StartConsuming();
    Task StopConsuming();
}
