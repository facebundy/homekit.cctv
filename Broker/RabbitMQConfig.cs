namespace homekit.cctv;

public class RabbitMQConfig
{
    public required string Uri { get; set; }
    public required string Exchange { get; set; }
    public required string Queue { get; set; }
    public required string LogQueue { get; set; }
}
