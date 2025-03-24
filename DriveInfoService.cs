using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace homekit.cctv;

public class DriveInfoService
{
    private readonly ILogger<MetadataBackgroundService> logger;
    private readonly IConfiguration configuration;
    private readonly IMessagePublisher publisher;
    private readonly AppConfig appConfig;
    private readonly RabbitMQConfig rabbitConfig;

    public DriveInfoService(
        ILogger<MetadataBackgroundService> logger,
        IConfiguration configuration,
        IOptionsMonitor<AppConfig> options,
        IOptionsMonitor<RabbitMQConfig> rabbitOptions,
        IMessagePublisher publisher)
    {
        this.logger = logger;
        this.configuration = configuration;
        this.publisher = publisher;
        appConfig = options.CurrentValue;
        rabbitConfig = rabbitOptions.CurrentValue;
    }

    public DriveInfo GetDriveInfo(string driveName)
    {
        foreach (DriveInfo drive in DriveInfo.GetDrives())
        {
            if (drive.IsReady && drive.Name == driveName)
            {
                return drive;
            }
        }
        return null;
    }

    public MemoryInfo GetMemoryInfo()
    {
        Process proc = Process.GetCurrentProcess();
        return new MemoryInfo
        {
            NonpagedSystemMemorySize64 = proc.NonpagedSystemMemorySize64,
            PagedMemorySize64 = proc.PagedMemorySize64,
            VirtualMemorySize64 = proc.VirtualMemorySize64,
            PagedSystemMemorySize64 = proc.PagedSystemMemorySize64,
            PeakVirtualMemorySize64 = proc.PeakVirtualMemorySize64,
            PrivateMemorySize64 = proc.PrivateMemorySize64
        };
    }

    private EventId GetEventId() => new(Environment.ProcessId, "Homekit CCTV");

    public async Task PurgeQueue()
    {
        await publisher.PurgeQueue();
    }

    public async Task SendMetadata()
    {
        var dir = appConfig.RootUrl;

        if (!Directory.Exists(dir))
        {
            logger.LogError(GetEventId(), "{Message}", "Directory does not exist.");
        }
        else
        {
            var videoUrl = Path.Combine(dir, appConfig.VideoUrl);
            if (!Directory.Exists(videoUrl))
            {
                logger.LogError(GetEventId(), "{Message}", "Video directory does not exist.");
            }

            var audioUrl = Path.Combine(dir, appConfig.AudioUrl);
            if (!Directory.Exists(audioUrl))
            {
                logger.LogError(GetEventId(), "{Message}", "Audio directory does not exist.");
            }

            var driveLetter = Path.GetPathRoot(dir);
            if (!string.IsNullOrEmpty(driveLetter))
            {
                var drive = GetDriveInfo(driveLetter);
                if (drive != null)
                {
                    await publisher.SendMessage(rabbitConfig.Queue, new Metadata(new DiskDriveInfo
                    {
                        Name = drive.Name,
                        DriveType = drive.DriveType,
                        VolumeLabel = drive.VolumeLabel,
                        DriveFormat = drive.DriveFormat,
                        TotalSize = drive.TotalSize,
                        AvailableFreeSpace = drive.AvailableFreeSpace,
                        TotalFreeSpace = drive.TotalFreeSpace
                    }, GetMemoryInfo(), new CctvConfig
                    {
                        RootUrl = dir,
                        VideoUrl = videoUrl,
                        AudioUrl = audioUrl,
                        AutoDeleteInterval = appConfig.AutoDeleteInterval,
                        MaxSizeLimitMb = appConfig.MaxSizeLimitMb,
                        Size = FileExtensions.GetDirectorySize(dir)
                    }));
                }
            }
        }
    }

    // public async Task PushMetadata(Action<HttpStatusCode, string> callback)
    // {
    //     var postResult = await http.PostAsJsonAsync(configuration["MetadataUrl"], new
    //     {
    //         Memory = GetMemoryInfo(),
    //         Drive = GetDriveInfo(configuration["Root"])
    //     });

    //     if (postResult.IsSuccessStatusCode)
    //     {
    //         var error = await postResult.Content.ReadAsStringAsync();
    //         callback(postResult.StatusCode, error);
    //     }
    //     else
    //     {
    //         callback(postResult.StatusCode, string.Empty);
    //     }
    // }
}

// public record PushCallback(HttpStatusCode StatusCode, string Error);
public record Metadata(DiskDriveInfo Drive, MemoryInfo Memory, CctvConfig Config);

public class CctvConfig
{
    public string RootUrl { get; set; }
    public string VideoUrl { get; set; }
    public string AudioUrl { get; set; }
    public long Size { get; set; }
    public long AutoDeleteInterval { get; set; }
    public long MaxSizeLimitMb { get; set; }
}

public class DiskDriveInfo
{
    public string Name { get; set; }
    public string VolumeLabel { get; set; }
    public string DriveFormat { get; set; }
    public long TotalSize { get; set; }
    public long AvailableFreeSpace { get; set; }
    public long TotalFreeSpace { get; set; }
    public DriveType DriveType { get; internal set; }
}

public class MemoryInfo
{
    public long NonpagedSystemMemorySize64 { get; set; }
    public long PagedMemorySize64 { get; set; }
    public long VirtualMemorySize64 { get; set; }
    public long PagedSystemMemorySize64 { get; set; }
    public long PeakVirtualMemorySize64 { get; set; }
    public long PrivateMemorySize64 { get; set; }
    public long TotalMemory => GC.GetTotalMemory(false);
}