namespace homekit.cctv;

public interface IMessagePublisher
{
    Task SendMessage<T>(string queue, T message);
    Task PurgeQueue();
}
