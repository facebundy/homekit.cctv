using Microsoft.Extensions.Options;

namespace homekit.cctv;

public class MetadataBackgroundService : BackgroundService
{
    private readonly IConfiguration configuration;
    private readonly ILogger<MetadataBackgroundService> _logger;
    private readonly DriveInfoService _cctvService;
    private readonly AppConfig appConfig;

    public MetadataBackgroundService(
        IConfiguration configuration,
        ILogger<MetadataBackgroundService> logger,
        DriveInfoService cctvService,
        IOptionsMonitor<AppConfig> options)
    {
        this.configuration = configuration;
        _logger = logger;
        _cctvService = cctvService;
        appConfig = options.CurrentValue;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await base.StartAsync(cancellationToken);
        await _cctvService.PurgeQueue();
    }

    private EventId GetEventId() => new(Environment.ProcessId, "Homekit CCTV");
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _cctvService.SendMetadata();
                await Task.Delay(TimeSpan.FromSeconds(configuration.GetValue<int>("UploadInterval")), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // When the stopping token is canceled, for example, a call made from services.msc,
            // we shouldn't exit with a non-zero exit code. In other words, this is expected...
        }
        catch (Exception ex)
        {
            _logger.LogError(GetEventId(), ex, "{Message}", ex.Message);

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
}
