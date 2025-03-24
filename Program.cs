using homekit.cctv;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Homekit CCTV";
});
builder.Services.Configure<AppConfig>(builder.Configuration.GetSection("Directory"));
builder.Services.Configure<RabbitMQConfig>(builder.Configuration.GetSection("RabbitMQ"));
// builder.Services.AddHttpClient("homekit.cctv", options =>
// {
//     options.BaseAddress = new Uri(builder.Configuration["UploadUrl"]);
// });

#pragma warning disable CA1416 // Validate platform compatibility
LoggerProviderOptions.RegisterProviderOptions<
    EventLogSettings, EventLogLoggerProvider>(builder.Services);
#pragma warning restore CA1416 // Validate platform compatibility

builder.Services.AddSingleton<DriveInfoService>();
builder.Services.AddSingleton<FileCleanupService>();
builder.Services.AddHostedService<MetadataBackgroundService>();
builder.Services.AddHostedService<FileCleanupBackgroundService>();
builder.Services.AddTransient<IMessagePublisher, RabbitMQMessagePublisher>();

var host = builder.Build();
host.Run();
