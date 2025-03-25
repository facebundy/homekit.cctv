using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace homekit.cctv;

public class RabbitMQMessageReceiver : IMessageReceiver
{
    private RabbitMQConfig _config;
    private readonly ILogger<RabbitMQMessageReceiver> _logger;
    private IConnection connection;
    private IChannel channel;

    public RabbitMQMessageReceiver(
        ILoggerFactory loggerFactory,
        IOptions<RabbitMQConfig> options)
    {
        _config = options.Value;
        _logger = loggerFactory.CreateLogger<RabbitMQMessageReceiver>();
    }

    public async Task DisposeAsync()
    {
        if (channel != null)
        {
            if (channel.IsOpen)
                await channel.CloseAsync();
            await channel.DisposeAsync();
        }

        if (connection != null)
        {
            if (connection.IsOpen)
                await connection.CloseAsync();
            await connection.DisposeAsync();
        }

        await Task.FromResult(0);
    }

    public async Task ReceiveAsync(string queue)
    {
        var factory = new ConnectionFactory() { Uri = new Uri(_config.Uri) };
        connection = await factory.CreateConnectionAsync();
        channel = await connection.CreateChannelAsync();
        await channel.ExchangeDeclareAsync(_config.Exchange, ExchangeType.Direct);
        await channel.QueueDeclareAsync(queue, durable: false, exclusive: false, autoDelete: false, null);
        await channel.QueueBindAsync(queue, _config.Exchange, queue);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (ch, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var routingKey = ea.RoutingKey;

            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
        };

        string consumerTag = await channel.BasicConsumeAsync(queue, false, consumer);
    }
}
