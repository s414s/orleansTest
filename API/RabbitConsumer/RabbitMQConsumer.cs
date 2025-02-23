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
    private readonly int _messageTtl = 3000;
    private readonly int _maxNumberRetries = 3;
    private readonly string _exchangeName = "actors_exchange";
    private readonly string[] _queueNames = ["info", "atlas"];

    private readonly string _queueName = "atlas";
    private readonly string _queueName2 = "atlas2";
    private AsyncEventingBasicConsumer? _consumer;

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
            ConsumerDispatchConcurrency = 1,
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        // Set QoS before declaring queue or starting consumer (optional)
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 200, global: false, cancellationToken: cancellationToken);

        // Declare the queue; not necessary in consumer???
        //await _channel.QueueDeclareAsync(queue: _queueName, durable: false, exclusive: false, autoDelete: false, arguments: null, cancellationToken: cancellationToken);
        //await _channel.QueueDeclareAsync(queue: _queueName2, durable: false, exclusive: false, autoDelete: false, arguments: null, cancellationToken: cancellationToken);

        await CreateExchanges(cancellationToken);
        await CreateQueues(cancellationToken);

        // Create a consumer to listen for messages
        _consumer = new AsyncEventingBasicConsumer(_channel);
        _consumer.ReceivedAsync += HandleMessageAsync;

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
            var exchange = eventArgs.Exchange;
            var messageId = eventArgs.BasicProperties.MessageId;
            Console.WriteLine($"[ERROR] -> {exchange} - {messageId}");

            var currentRetryAttempt = 1;

            if (eventArgs.BasicProperties.Headers?.TryGetValue("x-retry", out object? attempt) ?? false)
            {
                if (attempt is int a && a > 1)
                {
                    currentRetryAttempt = a;
                }
            }

            if (currentRetryAttempt >= _maxNumberRetries)
            {

            }

            // Using a Dead Letter Exchange(DLX)
            // creo que los hace el productor
            var retryHeaders = new Dictionary<string, object?>
            {
                //{ "x-TTL-3000", "deadLetterExchange" },
                { "x-ttl-3000", _messageTtl },
                { "x-dead-letter-exchange", _exchangeName },
                { "x-dead-letter-routing-key", eventArgs.RoutingKey }, // optional: if you want to override the routing key
                { "x-retry", eventArgs.RoutingKey } // optional: if you want to override the routing key
            };

            if (_channel != null && !_channel.IsClosed)
            {
                await _channel.BasicNackAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: false, // for it to be dead-lettered
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

    private static RabbitMQMessage ZeroAllocDeserializer(BasicDeliverEventArgs ea)
    {
        var message = Encoding.UTF8.GetString(ea.Body.Span).AsSpan();

        return JsonSerializer.Deserialize<RabbitMQMessage>(message)
            ?? throw new Exception("msg is NULL");
    }

    private async Task CreateQueues(CancellationToken ct)
    {
        if (_channel != null)
        {
            foreach (var queueName in _queueNames)
            {
                await _channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: ct);
                await _channel.QueueBindAsync(queue: queueName, exchange: _exchangeName, routingKey: queueName, cancellationToken: ct);

                await _channel.QueueDeclareAsync(queue: $"{queueName}.retry", durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: ct);
                await _channel.QueueBindAsync(queue: $"{queueName}.retry", exchange: $"{_exchangeName}.retry", routingKey: queueName, cancellationToken: ct);

                await _channel.QueueDeclareAsync(queue: $"{queueName}.deadletter", durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: ct);
                await _channel.QueueBindAsync(queue: $"{queueName}.deadletter", exchange: $"{_exchangeName}.deadletter", routingKey: queueName, cancellationToken: ct);
            }
        }
    }

    private async Task CreateExchanges(CancellationToken ct)
    {
        if (_channel != null)
        {
            await _channel.ExchangeDeclareAsync(exchange: _exchangeName, type: ExchangeType.Direct, autoDelete: false, durable: true);
            await _channel.ExchangeDeclareAsync(exchange: $"{_exchangeName}.retry", type: ExchangeType.Direct, autoDelete: false, durable: true);
            await _channel.ExchangeDeclareAsync(exchange: $"{_exchangeName}.deadletter", type: ExchangeType.Direct, autoDelete: false, durable: true);
        }
    }
}

