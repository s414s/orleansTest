using Orleans.Concurrency;

namespace API.Grains;

[StatelessWorker(1)]
public class StatelessWorker : Grain, IStatelessWorker
{
    public Task<int> GetCurrentState()
    {
        throw new NotImplementedException();
    }
}

public interface IStatelessWorker : IGrainWithIntegerKey
{
    Task<int> GetCurrentState();
}
