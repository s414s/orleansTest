using API.DTOs;
using API.Grains;
using RabbitMQ.Stream.Client;
using RabbitMQ.Stream.Client.Reliable;
using System.Buffers;
using System.Net;
using System.Text;
using System.Text.Json;

namespace API.RabbitConsumer;

public class RabbitMQStreamConsumer : IHostedService, IDisposable
{
    private readonly string _streamName = "hello-stream";
    private IGrainFactory _grains;
    private Consumer _consumer;
    private StreamSystem _streamSystem;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public RabbitMQStreamConsumer(IGrainFactory grains)
    {
        _grains = grains;
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        //await Task.Delay(1000, cancellationToken);
        Console.WriteLine("===========Starting WORKER=============");
        var config = new StreamSystemConfig()
        {
            UserName = "guest",
            Password = "guest",
            VirtualHost = "/",
            Endpoints =
            [
                new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5552), // Atention!! different port for streams
            ],
            ConnectionPoolConfig = new ConnectionPoolConfig()
            {
                ConsumersPerConnection = 1, // low value
                //ProducersPerConnection = 1,  // low value
            },
        };

        _streamSystem = await StreamSystem.Create(config);

        await _streamSystem.CreateStream(new StreamSpec("hello-stream")
        {
            MaxLengthBytes = 5_000_000_000
        });

        _consumer = await Consumer.Create(new ConsumerConfig(_streamSystem, _streamName)
        {
            OffsetSpec = new OffsetTypeFirst(),
            MessageHandler = HandleMessageAsync,
            //ClientProvidedName = $"restart-from-beginning-consumer-{Guid.NewGuid()}"
            ClientProvidedName = "high-throughput-consumer",

            // Increase credits (flow control - how many messages to prefetch)
            InitialCredits = 50,

            // Set socket options for higher throughput
            //SocketConfigurator = (socket) =>
            //{
            //    socket.ReceiveBufferSize = 8 * 1024 * 1024; // 8MB receive buffer
            //    socket.SendBufferSize = 8 * 1024 * 1024;    // 8MB send buffer
            //    socket.NoDelay = true;                      // Disable Nagle's algorithm
            //},
            // Use a short subscriber timeout
            //SubscriptionId = $"high-speed-consumer-{Guid.NewGuid()}",
        });

        // Set QoS before declaring queue or starting consumer (optional)

        // Create a consumer to listen for messages

        // Start consuming messages
        Console.WriteLine("RabbitMQ Consumer started.");
    }

    private async Task HandleMessageAsync(string streamName, RawConsumer rc, MessageContext mc, Message message)
    {
        try
        {
            // Get message position/offset in the stream
            var offset = mc.Offset;
            var chunkId = mc.ChunkId;
            var chunkMessageCount = mc.ChunkMessagesCount;
            var chunkTimestamp = mc.Timestamp;

            Console.WriteLine($"Message order: chunkMessageCount={chunkMessageCount}, Offset={offset}, ChunkId={chunkId}");

            // Option A
            var payload = Encoding.UTF8.GetString(message.Data.Contents);

            var msg = JsonSerializer.Deserialize<RabbitMQMessage>(payload)
                ?? throw new Exception("msg is NULL");

            var atlasGrain = _grains.GetGrain<IAtlas>(long.Parse(msg.Imei));
            await atlasGrain.UpdateFromRabbit(msg);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing message: {ex}");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _cancellationTokenSource.Cancel();

            if (_consumer != null)
            {
                await _consumer.Close();
            }

            if (_streamSystem != null && !_streamSystem.IsClosed)
            {
                await _streamSystem.Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping RabbitMQ consumer: {ex}");
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource.Dispose();
        //_streamSystem?.Dispose();
        //_connection?.Dispose();
    }

    private RabbitMQMessage ZeroAllocDeserializer(ReadOnlySequence<byte> content)
    {
        var message = Encoding.UTF8.GetString(content).AsSpan();

        return JsonSerializer.Deserialize<RabbitMQMessage>(message)
            ?? throw new Exception("msg is NULL");
    }
}

