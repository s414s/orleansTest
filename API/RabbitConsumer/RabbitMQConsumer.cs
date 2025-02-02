using API.DTOs;
using API.Grains;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace API.RabbitConsumer;

public class RabbitMqConsumerService : IHostedService, IDisposable
{
    private readonly string _queueName = "atlas";
    private IGrainFactory _grains;
    private IConnection? _connection;
    private IChannel? _channel;
    private AsyncEventingBasicConsumer? _consumer;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public RabbitMqConsumerService(IGrainFactory grains)
    {
        _grains = grains;
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(2000, cancellationToken);
        Console.WriteLine("===========Starting WORKER=============");

        // Initialize RabbitMQ connection and channel
        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            Port = 5672,
            UserName = "guest",
            Password = "guest",
            VirtualHost = "/",
            ConsumerDispatchConcurrency = 1,
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        // Declare the queue
        await _channel.QueueDeclareAsync(
            queue: _queueName,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        // Create a consumer to listen for messages
        _consumer = new AsyncEventingBasicConsumer(_channel);
        _consumer.ReceivedAsync += HandleMessageAsync;

        // Start consuming messages
        var consumerTag = await _channel.BasicConsumeAsync(_queueName, autoAck: false, consumer: _consumer, cancellationToken: cancellationToken);

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
            var body = eventArgs.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            //Console.WriteLine($" [x] Received {message}");

            var msg = JsonSerializer.Deserialize<RabbitMQMessage>(message)
                ?? throw new Exception("msg is NULL");

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
}

