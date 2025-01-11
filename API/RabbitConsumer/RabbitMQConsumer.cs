using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace API.RabbitConsumer;

public class RabbitMqConsumerService : IHostedService
{
    private readonly string _queueName = "atlas";
    private IConnection? _connection;
    private IModel? _channel;

    public Task StartAsync(CancellationToken cancellationToken)
    {
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
        consumer.Received += (model, eventArgs) =>
        {
            var body = eventArgs.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            Console.WriteLine($"Received message: {message}");

            _channel.BasicAck(deliveryTag: eventArgs.DeliveryTag, multiple: false);
        };

        // Start consuming messages
        _channel.BasicConsume(queue: _queueName,
                              autoAck: false,
                              consumer: consumer);

        Console.WriteLine("RabbitMQ Consumer started.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Stopping RabbitMQ Consumer...");

        _channel?.Close();
        _connection?.Close();

        return Task.CompletedTask;
    }
}

