namespace homekit.cctv;
using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

public class RabbitMQMessagePublisher : IMessagePublisher
{
    private readonly RabbitMQConfig _config;

    public RabbitMQMessagePublisher(IOptions<RabbitMQConfig> rabbitMqConfig)
    {
        _config = rabbitMqConfig.Value;
    }

    public async Task PurgeQueue()
    {
        var factory = new ConnectionFactory { Uri = new Uri(_config.Uri) };
        var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();
        await channel.QueuePurgeAsync(_config.Queue);

        await channel.DisposeAsync();
        await connection.DisposeAsync();
    }

    public async Task SendMessage<T>(string queue, T obj)
    {
        var factory = new ConnectionFactory { Uri = new Uri(_config.Uri) };
        var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(_config.Exchange, ExchangeType.Direct);
        await channel.QueueDeclareAsync(queue, durable: false, exclusive: false, autoDelete: false, null);
        await channel.QueueBindAsync(queue, _config.Exchange, queue);

        var message = System.Text.Json.JsonSerializer.Serialize(obj, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            MaxDepth = 5
        });
        byte[] body = Encoding.UTF8.GetBytes(message);
        await channel.BasicPublishAsync(_config.Exchange, routingKey: queue, body: body);

        await channel.DisposeAsync();
        await connection.DisposeAsync();
    }
}