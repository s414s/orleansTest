using API.DTOs;
using API.Grains;
using Microsoft.AspNetCore.SignalR;

namespace API.Hubs;

//https://www.rafaagahbichelab.dev/articles/signalr-dotnet-postman
public class ViewportHub : Hub<IViewportClient>
{
    private readonly IGrainFactory _grainFactory;

    public ViewportHub(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    public async Task SendMessage(string message)
    {
        //await Clients.All.SendAsync("ReceiveMessage", $"{Context.ConnectionId}: {message}");
        await Clients.All.ReceiveMessage($"{Context.ConnectionId}: {message}");
    }

    public async Task SendStateChange(AtlasChangeEvent atlasChange)
    {
        if (Clients != null)
        {
            await Clients.All.SendStateChange(atlasChange);
            //await Clients.Group(atlasChange.Imei).SendAsync("ReceiveGameState", gameState);
        }
    }

    public async Task Flush(List<AtlasChangeEvent> changes)
    {
        if (Clients != null)
        {
            await Clients.All.Flush(changes);
        }
    }

    public override async Task OnConnectedAsync()
    {
        var grain = _grainFactory.GetGrain<IWsGrain>("GeneralWS");
        await grain.AddConnection(Context.ConnectionId);

        //var userGrain = _grainFactory.GetGrain<IUserGrain>(Context.ConnectionId);
        //await userGrain.Start();

        await base.OnConnectedAsync();

        await Clients.All.ReceiveMessage($"{Context.ConnectionId} has joined"); // Name of the method invoked on the client
        //_grain ??= _grainFactory.GetGrain<IWsGrain>("GeneralWS");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var grain = _grainFactory.GetGrain<IWsGrain>("GeneralWS");
        await grain.RemoveConnection(Context.ConnectionId);

        await base.OnDisconnectedAsync(exception);
    }
}
