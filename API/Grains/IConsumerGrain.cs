namespace API.Grains;

public interface ISampleStreaming_ConsumerGrain : IGrainWithGuidKey
{
    Task BecomeConsumer(Guid streamId, string streamNamespace, string providerToUse);

    Task StopConsuming();

    Task<int> GetNumberConsumed();
}
