using API.DTOs;
using API.Hubs;
using Microsoft.AspNetCore.SignalR;
using Orleans.Streams;

namespace API.Grains;

//public class UserGrain : Grain, IUserGrain, IAsyncObserver<AtlasChangeEvent>
//{
//    private IAsyncStream<AtlasChangeEvent>? _stream;
//    private string _streamProvider = "StreamProvider";
//    private readonly IHubContext<ViewportHub, IViewportClient> _hub;
//    private readonly HashSet<string> _connections = [];
//    private IGrainTimer? _timer;
//    private StreamSubscriptionHandle<AtlasChangeEvent>? _subscription;

//    private readonly List<AtlasChangeEvent> _newStates = new List<AtlasChangeEvent>(20);

//    public UserGrain(IHubContext<ViewportHub, IViewportClient> hub)
//    {
//        _hub = hub;
//    }

//    public override async Task OnActivateAsync(CancellationToken cancellationToken)
//    {
//        await base.OnActivateAsync(cancellationToken);

//        Console.WriteLine($"GetPrimaryKeyString() {this.GetPrimaryKeyString()}");

//        _connections.Add(this.GetPrimaryKeyString());

//        _stream = this.GetStreamProvider(_streamProvider)
//                .GetStream<AtlasChangeEvent>(StreamId.Create("AtlasChange", "ALL"));

//        _subscription = await _stream.SubscribeAsync(OnNextAsync);

//        _timer = this.RegisterGrainTimer(async () =>
//          {
//              await Flush();
//              //Console.WriteLine("Starting Timer");
//              //await Task.Delay(TimeSpan.FromSeconds(1));
//              //Console.WriteLine("Ending Timer");
//          }, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
//    }

//    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
//    {
//        _connections.Clear();

//        await base.OnDeactivateAsync(reason, cancellationToken);
//    }

//    public Task RemoveConnection(string connectionId)
//    {
//        _connections.Remove(connectionId);
//        return Task.CompletedTask;
//    }

//    public Task OnNextAsync(AtlasChangeEvent item, StreamSequenceToken? token = null)
//    {
//        Console.WriteLine($"CLIENT_WS => Imei {item.Imei} \t {item.Long} \t {item.Lat} \t {item.Color}");
//        //await _hub.Clients.All.SendStateChange(item);

//        _newStates.Add(item);

//        return Task.CompletedTask;
//        //_points[item.Imei] = new Pt
//        //{
//        //    Name = item.Imei,
//        //    Lat = item.Lat,
//        //    Lng = item.Long,
//        //    C = item.Color == "RED" ? 'R' : 'G',
//        //};
//    }

//    public Task OnCompletedAsync()
//    {
//        Console.WriteLine($"Stream COMPLETED");
//        return Task.CompletedTask;
//    }

//    public Task OnErrorAsync(Exception ex)
//    {
//        Console.WriteLine($"Stream error: {ex.Message}");
//        return Task.CompletedTask;
//    }

//    public Task Start()
//    {
//        return Task.CompletedTask;
//    }

//    public async Task Flush()
//    {
//        await _hub.Clients.All.Flush(_newStates);

//        _newStates.Clear();
//    }
//}

//public interface IUserGrain : IGrainWithStringKey
//{
//    public Task Start();
//    public Task Flush();
//}

