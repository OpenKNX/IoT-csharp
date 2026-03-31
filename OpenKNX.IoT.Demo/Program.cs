using OpenKNX.IoT;
using OpenKNX.IoT.Demo.Classes;
using OpenKNX.IoT.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<KnxIotDevice>(provider =>
{
    var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
    return new KnxIotDevice(loggerFactory);
});

builder.Services.AddSingleton<WebsocketHandler>();

builder.Services.AddSingleton<LogicHandler>(provider =>
{
    var device = provider.GetRequiredService<KnxIotDevice>();
    var websocket = provider.GetRequiredService<WebsocketHandler>();
    var logic = new LogicHandler(websocket, device);
    websocket.SetLogicHandler(logic);
    return logic;
});

var app = builder.Build();
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.UseWebSockets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

List<string> configPaths = new()
{
    "/config/device.json",
    "device.json"
};
string configPath = string.Empty;

foreach(string path in configPaths)
{
    if(File.Exists(path))
    {
        configPath = path;
        break;
    }
}

if(string.IsNullOrEmpty(configPath))
{
    app.Logger.LogError("No configuration file found. Please ensure that 'device.json' is present in the application directory or in the '/config' folder.");
    Environment.Exit(1);
}

app.Logger.LogInformation("Loading configuration from {ConfigPath}", configPath);
var device = app.Services.GetRequiredService<KnxIotDevice>();
InitialDeviceConfig? config = System.Text.Json.JsonSerializer.Deserialize<InitialDeviceConfig>(File.ReadAllText(configPath));
if (config == null)
{
    app.Logger.LogError("Failed to deserialize device configuration from {Path}", configPath);
    Environment.Exit(1);
}
device.Start(config);
var logicHandler = app.Services.GetRequiredService<LogicHandler>();

app.Run();