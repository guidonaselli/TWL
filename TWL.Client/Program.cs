using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Xna.Framework.Content;
using TWL.Client.Presentation.Core;
using TWL.Client.Presentation.Managers;
using TWL.Client.Presentation.Networking;
using TWL.Shared.Net.Abstractions;
using TWL.Shared.Net.Network;

namespace TWL.Client;

public static class Program
{
    [STAThread]
    private static void Main()
    {
        // 1) Configuro un Host para inyectar dependencias
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                // Registra tus servicios
                services.AddSingleton(sp => new ContentManager(sp, "Content"));
                services.AddSingleton<AssetLoader>();
                services.AddSingleton<IAssetLoader>(sp => sp.GetRequiredService<AssetLoader>());
                services.AddSingleton<SceneManager>();
                services.AddSingleton<ISceneManager>(sp => sp.GetRequiredService<SceneManager>());
                services.AddSingleton<GameManager>();
                services.AddSingleton<IGameManager>(sp => sp.GetRequiredService<GameManager>());
                services.AddSingleton<GameSessionManager>();
                services.AddSingleton<GameClientManager>();
                services.AddSingleton<LoopbackChannel>();
                services.AddSingleton<INetworkChannel>(sp => sp.GetRequiredService<LoopbackChannel>());
                services.AddSingleton<SettingsManager>();
                services.AddSingleton<PersistenceManager>();
                services.AddLogging();
                // Registra el Game1
                services.AddSingleton<Game1>();
            })
            .Build();

        // 2) Resuelvo el Game1 y lo arranco
        var game = host.Services.GetRequiredService<Game1>();
        game.Run();
    }
}