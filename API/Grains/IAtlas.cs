using API.DTOs;

namespace API.Grains;

public interface IAtlas : IGrainWithStringKey
{
    Task ReadMsg(string msg);
    Task<int> GetBatteryLevel();
    Task<string> SayHello(string greeting);
    Task UpdateFromRabbit(RabbitMQMessage msg);
    Task<AtlasState> GetState();

}
