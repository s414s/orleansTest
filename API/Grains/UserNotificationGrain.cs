using API.DTOs;
using API.Hubs;
using Orleans.Streams;
using SignalR.Orleans.Core;
using System.Threading;

namespace API.Grains;

public class UserNotificationGrain : Grain, IUserNotificationGrain, IAsyncObserver<AtlasChangeEvent>

{
    private const string BroadcastMessage = "BroadcastMessage";
    private readonly ILogger<UserNotificationGrain> _logger;
    private readonly IGrainFactory _grainFactory;
    private HubContext<ChatHub> _hubContext = default!;

    public UserNotificationGrain(
        ILogger<UserNotificationGrain> logger,
        IGrainContext context,
        IGrainFactory grainFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _grainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
        GrainContext = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IGrainContext GrainContext { get; }

    public override async Task OnActivateAsync(CancellationToken ct)
    {
        // Always call the base activation first
        await base.OnActivateAsync(ct);

        _logger.LogInformation($"{nameof(OnActivateAsync)} called");
        _hubContext = this._grainFactory.GetHub<ChatHub>();

        Console.WriteLine($"========================================");
        Console.WriteLine($"========================================");
        Console.WriteLine($"Activating WS CLIENT GRAIN {IdentityString} with IMEI: {this.GetPrimaryKeyString()}");
        Console.WriteLine($"========================================");
        Console.WriteLine($"========================================");

        await SubscribeToAtlasStream("GeneralWS");
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
        Console.WriteLine($"Stream Error {ex}");
        return Task.CompletedTask;
    }

    public Task OnNextAsync(AtlasChangeEvent item, StreamSequenceToken? token = null)
    {
        throw new NotImplementedException();
    }

    public async Task SendMessageAsync(string name, string message)
    {
        var groupId = this.GetPrimaryKeyString();
        _logger.LogInformation($"{nameof(SendMessageAsync)} called. Name:{name}, Message:{message}, Key:{groupId}");
        _logger.LogInformation($"Sending message to group: {groupId}. MethodName:{BroadcastMessage} Name:{name}, Message:{message}");

        await _hubContext.Group(groupId).Send(BroadcastMessage, name, message);
    }
}

public interface IUserNotificationGrain : IGrainWithStringKey
{
    Task SendMessageAsync(string name, string message);
}
