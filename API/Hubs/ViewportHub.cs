using API.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace API.Hubs;

public class ViewportHub : Hub
{
    public async Task SendGameState(AtlasChangeEvent atlasChange)
    {
        if (Clients != null)
        {
            await Clients.All.SendAsync("ReceiveGameState", atlasChange);
            //await Clients.Group(atlasChange.Imei).SendAsync("ReceiveGameState", gameState);
        }
    }

    public async Task JoinGame(string gameCode)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, gameCode);
    }
}
