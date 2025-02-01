using API.DTOs;

namespace API.Grains;

public interface IAtlas : IGrainWithIntegerKey
{
    Task ReadMsg(string msg);
    Task<int> GetBatteryLevel();
    Task UpdateFromRabbit(RabbitMQMessage msg);
    Task<AtlasState> GetState();
}
