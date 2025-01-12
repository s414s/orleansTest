using API.DTOs;
using API.Grains;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace API.RabbitConsumer;

public class RabbitMqConsumerService : IHostedService
{
    private readonly string _queueName = "atlas";
    private IConnection? _connection;
    private IModel? _channel;
    private IGrainFactory _grains;

    public RabbitMqConsumerService(IGrainFactory grains)
    {
        _grains = grains;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(5000, cancellationToken);
        Console.WriteLine("===========Starting WORKER=============");

        // Initialize RabbitMQ connection and channel
        var factory = new ConnectionFactory()
        {
            HostName = "localhost",
            Port = 5672,
            UserName = "guest",
            Password = "guest",
            VirtualHost = "/",
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Declare the queue
        _channel.QueueDeclare(queue: _queueName,
                              durable: false,
                              exclusive: false,
                              autoDelete: false,
                              arguments: null);

        // Create a consumer to listen for messages
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, eventArgs) =>
        {
            try
            {
                var body = eventArgs.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                var msg = JsonSerializer.Deserialize<RabbitMQMessage>(message)
                    ?? throw new Exception("msg is NULL");

                // Console.WriteLine($"Received message on Rabbit Consumer: {message}");

                var atlasGrain = _grains.GetGrain<IAtlas>(msg.Imei);
                await atlasGrain.UpdateFromRabbit(msg);

                _channel.BasicAck(deliveryTag: eventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        };

        // Start consuming messages
        _channel.BasicConsume(queue: _queueName,
                              autoAck: false,
                              consumer: consumer);

        Console.WriteLine("RabbitMQ Consumer started.");
        //return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Stopping RabbitMQ Consumer...");

        _channel?.Close();
        _connection?.Close();

        return Task.CompletedTask;
    }
}

