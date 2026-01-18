using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework.Content;
using TWL.Client.Presentation.Core;
using TWL.Client.Presentation.Managers;
using TWL.Client.Presentation.Networking;
using TWL.Shared.Net.Abstractions;
using TWL.Shared.Net.Network;

namespace TWL.Client
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            // 1) Configuro un Host para inyectar dependencias
            using var host = Host.CreateDefaultBuilder()
                .ConfigureServices((_, services) =>
                {
                    // Registra tus servicios
                    services.AddSingleton(sp => new ContentManager(sp, "Content"));
                    services.AddSingleton<IAssetLoader, AssetLoader>();
                    services.AddSingleton<ISceneManager, SceneManager>();
                    services.AddSingleton<IGameManager, GameManager>();
                    services.AddSingleton<GameClientManager>();
                    services.AddSingleton<INetworkChannel, LoopbackChannel>();
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
}