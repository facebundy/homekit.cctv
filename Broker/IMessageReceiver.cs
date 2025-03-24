namespace homekit.cctv;

public interface IMessageReceiver
{
    Task ReceiveAsync(string queue);
    Task DisposeAsync();
}