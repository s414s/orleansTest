using API.DTOs;
using API.Grains;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace API.RabbitConsumer;

// https://www.rabbitmq.com/docs/confirms#basics
public class RabbitMqConsumerService : IHostedService, IDisposable
{
    private readonly string _queueName = "atlas";
    private readonly string _queueName2 = "atlas2";
    private AsyncEventingBasicConsumer? _consumer;
    private AsyncEventingBasicConsumer? _consumer2;

    private IGrainFactory _grains;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public RabbitMqConsumerService(IGrainFactory grains)
    {
        _grains = grains;
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        //await Task.Delay(1000, cancellationToken);
        Console.WriteLine("===========Starting WORKER=============");

        // Initialize RabbitMQ connection and channel
        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            Port = 5672,
            UserName = "guest",
            Password = "guest",
            VirtualHost = "/",
            ConsumerDispatchConcurrency = 10,
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        // Set QoS before declaring queue or starting consumer (optional)
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 400, global: false, cancellationToken: cancellationToken);

        // Declare the queue
        await _channel.QueueDeclareAsync(
            queue: _queueName,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: _queueName2,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        // Create a consumer to listen for messages
        _consumer = new AsyncEventingBasicConsumer(_channel);
        _consumer.ReceivedAsync += HandleMessageAsync;

        _consumer2 = new AsyncEventingBasicConsumer(_channel);
        _consumer2.ReceivedAsync += HandleMessageAsync;

        // Start consuming messages
        var consumerTag = await _channel.BasicConsumeAsync(_queueName, autoAck: false, consumer: _consumer, cancellationToken: cancellationToken);
        var consumerTag2 = await _channel.BasicConsumeAsync(_queueName2, autoAck: false, consumer: _consumer, cancellationToken: cancellationToken);

        Console.WriteLine("RabbitMQ Consumer started.");

        // Add connection shutdown event handler
        //_connection.ConnectionShutdownAsync += (sender, args) =>
        //{
        //    Console.WriteLine($"RabbitMQ connection shut down: {args.ReplyText}");
        //};
    }

    private async Task HandleMessageAsync(object? sender, BasicDeliverEventArgs eventArgs)
    {
        try
        {
            //Console.WriteLine($" [x] Received {message}");

            // Option A
            //var body = eventArgs.Body.ToArray();
            //var message = Encoding.UTF8.GetString(body);
            //var msg = JsonSerializer.Deserialize<RabbitMQMessage>(message) ?? throw new Exception("msg is NULL");

            var msg = ZeroAllocDeserializer(eventArgs);

            //var hasMessageBeenRedelivered = eventArgs.Redelivered;
            //var exchange = eventArgs.Exchange;

            var atlasGrain = _grains.GetGrain<IAtlas>(long.Parse(msg.Imei));
            await atlasGrain.UpdateFromRabbit(msg);

            if (_channel != null && !_channel.IsClosed)
            {
                await _channel.BasicAckAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    cancellationToken: _cancellationTokenSource.Token);
            }
        }
        catch (Exception ex)
        {
            if (_channel != null && !_channel.IsClosed)
            {
                await _channel.BasicNackAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: true,
                    cancellationToken: _cancellationTokenSource.Token);
            }

            Console.WriteLine($"Error processing message: {ex}");
            // Optionally implement retry logic or dead letter queue here
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _cancellationTokenSource.Cancel();

            if (_consumer != null)
            {
                _consumer.ReceivedAsync -= HandleMessageAsync;
            }

            if (_channel != null && !_channel.IsClosed)
            {
                await _channel.CloseAsync(cancellationToken);
            }

            if (_connection != null && _connection.IsOpen)
            {
                await _connection.CloseAsync(cancellationToken);
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
        _channel?.Dispose();
        _connection?.Dispose();
    }

    private RabbitMQMessage ZeroAllocDeserializer(BasicDeliverEventArgs ea)
    {
        var message = Encoding.UTF8.GetString(ea.Body.Span).AsSpan();

        return JsonSerializer.Deserialize<RabbitMQMessage>(message)
            ?? throw new Exception("msg is NULL");
    }
}

