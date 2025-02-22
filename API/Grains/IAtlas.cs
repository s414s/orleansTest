using API.DTOs;
using Orleans.Concurrency;

namespace API.Grains;

public interface IAtlas : IGrainWithIntegerKey
{
    [ReadOnly]
    Task<int> GetBatteryLevel();
    ValueTask UpdateFromRabbit(RabbitMQMessage msg);
    [ReadOnly]
    Task<AtlasState> GetState();
}
