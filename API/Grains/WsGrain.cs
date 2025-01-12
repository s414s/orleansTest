using API.DTOs;
using Orleans.Streams;

namespace API.Grains;

//https://github.com/OrleansContrib/SignalR.Orleans
public sealed class WsGrain : Grain, IWsGrain, IAsyncObserver<AtlasChangeEvent>
{
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // Always call the base activation first
        await base.OnActivateAsync(cancellationToken);

        //Console.WriteLine($"========================================");
        //Console.WriteLine($"Activating WS Grain {IdentityString} with IMEI: {this.GetPrimaryKeyString()}");
        //Console.WriteLine($"========================================");

        // Get the IMEI from the grain's key
        // If you created the grain with GrainFactory.GetGrain<IWsGrain>("device123"),
        // then "device123" is the primary key

        // Ya se suscriben con el metodo
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
        //throw new NotImplementedException();
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
            Console.WriteLine($"G_WS => msg {item.Imei} \t {item.Long}-{item.Lat} \t {item.Color}");
        }

        return Task.CompletedTask;
    }
}

public interface IWsGrain : IGrainWithStringKey
{
    Task SubscribeToAtlasStream(string imei);
}
