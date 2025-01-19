using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Streams;
using System.Runtime.CompilerServices;
using System.Xml;
//using UnitTests.GrainInterfaces;

namespace API.Grains;

public class SampleConsumerObserver<T> : IAsyncObserver<T>
{
    private readonly SampleStreaming_ConsumerGrain hostingGrain;

    internal SampleConsumerObserver(SampleStreaming_ConsumerGrain hostingGrain)
    {
        this.hostingGrain = hostingGrain;
    }

    public Task OnNextAsync(T item, StreamSequenceToken? token = null)
    {
        hostingGrain.logger.LogInformation("OnNextAsync(item={Item}, token={Token})", item, token != null ? token.ToString() : "null");
        hostingGrain.numConsumedItems++;
        return Task.CompletedTask;
    }

    public Task OnCompletedAsync()
    {
        hostingGrain.logger.LogInformation("OnCompletedAsync()");
        return Task.CompletedTask;
    }

    public Task OnErrorAsync(Exception ex)
    {
        hostingGrain.logger.LogInformation(ex, "OnErrorAsync()");
        return Task.CompletedTask;
    }
}

public class SampleStreaming_ConsumerGrain : Grain, ISampleStreaming_ConsumerGrain
{
    private IAsyncObservable<int>? consumer;
    internal int numConsumedItems;
    internal ILogger logger;
    private IAsyncObserver<int>? consumerObserver;
    private StreamSubscriptionHandle<int>? consumerHandle;

    public SampleStreaming_ConsumerGrain(ILoggerFactory loggerFactory)
    {
        this.logger = loggerFactory.CreateLogger($"{this.GetType().Name}-{this.IdentityString}");
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("OnActivateAsync");
        logger.LogInformation("OnActivateAsync");
        numConsumedItems = 0;
        consumerHandle = null;
        return Task.CompletedTask;
    }

    public async Task BecomeConsumer(Guid streamId, string streamNamespace, string providerToUse)
    {
        Console.WriteLine("BecomeConsumer");
        logger.LogInformation("OnActivateAsync");
        logger.LogInformation("BecomeConsumer");

        consumerObserver = new SampleConsumerObserver<int>(this);
        //IStreamProvider streamProvider = this.GetStreamProvider(providerToUse);
        var streamProvider = this.GetStreamProvider(providerToUse);
        consumer = streamProvider.GetStream<int>(streamNamespace, streamId);
        consumerHandle = await consumer.SubscribeAsync(consumerObserver);
    }

    public async Task StopConsuming()
    {
        Console.WriteLine("StopConsuming");
        logger.LogInformation("StopConsuming");
        if (consumerHandle != null)
        {
            await consumerHandle.UnsubscribeAsync();
            consumerHandle = null;
        }
    }

    public Task<int> GetNumberConsumed()
    {
        return Task.FromResult(numConsumedItems);
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        Console.WriteLine("OnDeactivateAsync");
        logger.LogInformation("OnDeactivateAsync");
        return Task.CompletedTask;
    }
}
