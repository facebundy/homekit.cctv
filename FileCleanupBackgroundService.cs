using homekit.cctv;
using homekit.cctv.Models;
using Microsoft.Extensions.Options;

public class FileCleanupBackgroundService : BackgroundService
{
    private readonly FileCleanupService service;
    private readonly IMessagePublisher messagePublisher;
    private readonly ILogger<MetadataBackgroundService> logger;
    private readonly IConfiguration configuration;
    private readonly AppConfig appConfig;
    private readonly RabbitMQConfig rabbitConfig;

    public FileCleanupBackgroundService(
        FileCleanupService service,
        IOptionsMonitor<AppConfig> options,
        IOptionsMonitor<RabbitMQConfig> rabbitOptions,
        IMessagePublisher messagePublisher,
        ILogger<MetadataBackgroundService> logger,
        IConfiguration configuration)
    {
        this.service = service;
        this.messagePublisher = messagePublisher;
        this.logger = logger;
        this.configuration = configuration;
        this.appConfig = options.CurrentValue;
        this.rabbitConfig = rabbitOptions.CurrentValue;
    }

    private EventId GetEventId() => new(Environment.ProcessId, "Homekit CCTV");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                service.CheckAndCleanDirectory(appConfig.RootUrl, appConfig.MaxSizeLimitMb);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                // await Task.Delay(TimeSpan.FromMinutes(configuration.GetValue<int>("AutoDeleteInterval")), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // When the stopping token is canceled, for example, a call made from services.msc,
            // we shouldn't exit with a non-zero exit code. In other words, this is expected...
        }
        catch (Exception ex)
        {
            await LogMessageAsync(ex.Message, LogLevel.Error, ex);

            // Terminates this process and returns an exit code to the operating system.
            // This is required to avoid the 'BackgroundServiceExceptionBehavior', which
            // performs one of two scenarios:
            // 1. When set to "Ignore": will do nothing at all, errors cause zombie services.
            // 2. When set to "StopHost": will cleanly stop the host, and log errors.
            //
            // In order for the Windows Service Management system to leverage configured
            // recovery options, we need to terminate the process with a non-zero exit code.
            Environment.Exit(1);
        }
    }

    private async Task LogMessageAsync(string message, LogLevel level = LogLevel.Information, Exception? ex = null)
    {
        if (!string.IsNullOrEmpty(message))
        {
            logger.Log(level, GetEventId(), ex, message);
            await messagePublisher.SendMessage(rabbitConfig.LogQueue, new LogMessage
            {
                Message = message,
                Level = level,
                Exception = ex?.ToString(),
                Timestamp = DateTime.UtcNow
            });
        }
    }
}