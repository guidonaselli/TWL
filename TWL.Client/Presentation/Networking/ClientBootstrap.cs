using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TWL.Client.Presentation.Managers;

namespace TWL.Client.Presentation.Networking;

/// <summary>Inicializa DI y logging. Llama en el ctor de Game1.</summary>
public static class ClientBootstrap
{
    public static ServiceProvider Services { get; private set; } = null!;

    public static void Init()
    {
        // Config + Serilog
        var cfg = new ConfigurationBuilder()
            .AddJsonFile("SerilogSettings.json", false, true)
            .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(cfg)
            .CreateLogger();

        // container
        Services = new ServiceCollection()
            .AddLogging(b => b.AddSerilog())
            .AddSingleton<GameClientManager>()
            .AddSingleton<InputManager>()
            .BuildServiceProvider();
    }
}