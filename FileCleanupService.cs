using System.Threading.Tasks;
using homekit.cctv.Models;
using Microsoft.Extensions.Options;

namespace homekit.cctv;

public class FileCleanupService
{
    private readonly ILogger<FileCleanupService> logger;
    private readonly RabbitMQConfig rabbitConfig;
    private readonly IMessagePublisher messagePublisher;
    private bool isRunning;

    public FileCleanupService(
        ILogger<FileCleanupService> logger,
        IOptionsMonitor<RabbitMQConfig> rabbitOptions,
        IMessagePublisher messagePublisher)
    {
        this.logger = logger;
        this.rabbitConfig = rabbitOptions.CurrentValue;
        this.messagePublisher = messagePublisher;
    }

    public void FreeUpSpace(string directoryPath, long totalSize, long maxSize)
    {
        var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories)
                            .Select(f => new FileInfo(f))
                            .OrderBy(f => f.CreationTime)
                            .ToList();

        foreach (var file in files)
        {
            if (totalSize <= maxSize) break;
            try
            {
                if (!file.Exists)
                {
                    Task.Run(async () => await LogMessageAsync($"File no longer exists: {file.FullName}", LogLevel.Warning));
                    continue;
                }

                long fileLength = file.Length; // Get length before deletion
                file.Attributes = FileAttributes.Normal;
                file.Delete();
                totalSize -= fileLength;
                Task.Run(async () => await LogMessageAsync($"Deleted: {file.FullName}", LogLevel.Warning));
            }
            catch (Exception ex)
            {
                Task.Run(async () => await LogMessageAsync($"Failed to delete {file.FullName}: {ex.Message}", LogLevel.Error, ex));
            }
        }
    }

    public static double BytesToGigabytes(long bytes)
    {
        return bytes / (1024.0 * 1024.0 * 1024.0);
    }

    public static double MegabytesToGigabytes(double megabytes)
    {
        return megabytes / 1024.0;
    }

    public static long MegabytesToBytes(double megabytes)
    {
        return (long)(megabytes * 1024 * 1024);
    }

    public void CheckAndCleanDirectory(string directoryPath, long maxSizeByMegabytes)
    {
        try
        {
            if (isRunning) return;

            isRunning = true;
            long totalSize = GetDirectorySize(directoryPath);
            var maxSizeBytes = MegabytesToBytes(maxSizeByMegabytes);
            if (totalSize > maxSizeBytes)
            {
                Task.Run(async () => await LogMessageAsync($"Total directory size: {FileExtensions.FormatSize(totalSize)} exceeded the {FileExtensions.FormatSize(maxSizeBytes)} limit.", LogLevel.Warning));
                FreeUpSpace(directoryPath, totalSize, maxSizeBytes);
            }
            isRunning = false;
        }
        catch (Exception ex)
        {
            isRunning = false;
            Task.Run(async () => await LogMessageAsync($"Error: {ex.Message}", LogLevel.Error, ex));
        }
    }

    private long GetDirectorySize(string directoryPath)
    {
        long size = 0;
        try
        {
            foreach (var file in Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories))
            {
                size += new FileInfo(file).Length;
            }
        }
        catch (Exception ex)
        {
            Task.Run(async () => await LogMessageAsync($"Error accessing {directoryPath}: {ex.Message}", LogLevel.Error, ex));
        }
        return size;
    }

    private EventId GetEventId() => new(Environment.ProcessId, "Homekit CCTV");

    private async Task LogMessageAsync(string message, LogLevel level = LogLevel.Information, Exception? ex = null)
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