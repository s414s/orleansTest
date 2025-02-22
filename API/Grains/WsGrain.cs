using API.DTOs;
using API.Hubs;
using Microsoft.AspNetCore.SignalR;
using Orleans.Concurrency;
using System.Collections.Concurrent;

namespace API.Grains;

//https://github.com/OrleansContrib/SignalR.Orleans
[KeepAlive]
public sealed class WsGrain : Grain, IWsGrain
{
    private readonly ConcurrentDictionary<long, Pt> _points;
    private readonly IHubContext<ViewportHub, IViewportClient> _hub;
    private readonly HashSet<string> _connections = [];

    public WsGrain(IHubContext<ViewportHub, IViewportClient> hub)
    {
        _points = new ConcurrentDictionary<long, Pt>(2, 10_000);
        _hub = hub;
    }

    public async Task AddConnection(string connectionId)
    {
        _connections.Add(connectionId);

        // Send current state to new connection
        var pointsChunks = _points.Select(x => x.Value).Chunk(1000);

        foreach (var points in pointsChunks)
        {
            await _hub.Clients.Client(connectionId).InitializeState(points.ToList());
        }
    }

    public async ValueTask GetAtlasChangeEvent(AtlasChangeEvent evt)
    {
        //if (this.GetPrimaryKeyString() == "GeneralWS" && _connections.Any())
        //Console.WriteLine($"G_WS => Imei {evt.Imei:D15} \t {evt.Long} \t {evt.Lat} \t {evt.Color}");

        var newPoint = new Pt
        {
            Imei = evt.Imei.ToString("D15"),
            Lat = evt.Lat,
            Lng = evt.Long,
            C = evt.Color == 'R' ? 'R' : 'G',
        };

        if (_connections.Count > 0)
        {
            await _hub.Clients.All.SendStateChange(newPoint);
        }

        // Atomic
        _points.AddOrUpdate(evt.Imei,
            newPoint,
            (key, oldValue) =>
            {
                oldValue.Imei = evt.Imei.ToString("D15");
                oldValue.Lat = evt.Lat;
                oldValue.Lng = evt.Long;
                oldValue.C = evt.Color == 'R' ? 'R' : 'G';

                return oldValue;
            });

        //_points[evt.Imei] = new Pt
        //{
        //    Imei = evt.Imei,
        //    Lat = evt.Lat,
        //    Lng = evt.Long,
        //    C = evt.Color == "RED" ? 'R' : 'G',
        //};
    }

    public Task RemoveConnection(string connectionId)
    {
        _connections.Remove(connectionId);
        return Task.CompletedTask;
    }

    public Task<List<Pt>> GetAllPoints()
    {
        //return Task.FromResult(_points.Select(x => x.Value).ToList());
        return Task.FromResult(_points.Select(x => x.Value).Chunk(100).First().ToList());
    }

    //public Task<List<Pt>> GetAllPoints(int page = 1, int pageSize = 100)
    //{
    //    return Task.FromResult(
    //        _points.Select(x => x.Value)
    //               .Skip((page - 1) * pageSize)
    //               .Take(pageSize)
    //               .ToList()
    //    );
    //}
}

public interface IWsGrain : IGrainWithIntegerKey
{
    ValueTask GetAtlasChangeEvent(AtlasChangeEvent evt);

    [ReadOnly]
    Task<List<Pt>> GetAllPoints();
    Task AddConnection(string connectionId);
    Task RemoveConnection(string connectionId);
}
