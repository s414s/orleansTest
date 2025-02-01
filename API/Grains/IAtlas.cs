using API.DTOs;

namespace API.Grains;

public interface IAtlas : IGrainWithIntegerKey
{
    Task<int> GetBatteryLevel();
    Task UpdateFromRabbit(RabbitMQMessage msg);
    Task<AtlasState> GetState();
}
