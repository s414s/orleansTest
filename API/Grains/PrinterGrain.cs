using API.DTOs;
using Orleans.BroadcastChannel;
using System.Collections.Concurrent;

namespace API.Grains;

//https://learn.microsoft.com/en-us/dotnet/orleans/streaming/broadcast-channel
//[ImplicitChannelSubscription]
//public sealed class PrinterGrain : Grain, IPrinterGrain, IOnBroadcastChannelSubscribed
//{
//    private readonly IDictionary<string, AtlasSnapshot> _ssCache =
//        new ConcurrentDictionary<string, AtlasSnapshot>();

//    public ValueTask<AtlasSnapshot> GetBattery(string imei) =>
//        _ssCache.TryGetValue(imei, out AtlasSnapshot? ss) is false
//            ? new ValueTask<AtlasSnapshot>(Task.FromException<AtlasSnapshot>(new KeyNotFoundException()))
//            : new ValueTask<AtlasSnapshot>(ss);

//    public Task OnSubscribed(IBroadcastChannelSubscription subscription) =>
//        subscription.Attach<AtlasSnapshot>(OnSSUpdate, OnError);

//    private Task OnSSUpdate(AtlasSnapshot ss)
//    {
//        //if (stock is { GlobalQuote: { } })
//        //{
//        //    _stockCache[stock.GlobalQuote.Symbol] = stock;
//        //}

//        _ssCache[ss.Imei] = ss;

//        return Task.CompletedTask;
//    }

//    private static Task OnError(Exception ex)
//    {
//        Console.Error.WriteLine($"An error occurred: {ex}");

//        return Task.CompletedTask;
//    }
//}

//public interface IPrinterGrain
//{
//    public ValueTask<AtlasSnapshot> GetBattery(string imei);
//}
