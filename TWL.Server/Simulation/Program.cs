using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using TWL.Server;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Database;
using TWL.Server.Persistence.Services;
using TWL.Server.Services;
using TWL.Server.Simulation;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Services;

Host.CreateDefaultBuilder(args)
    // 1) Config: appsettings + ServerConfig.json + SerilogSettings.json
    .ConfigureAppConfiguration(cb =>
    {
        cb.AddJsonFile("ServerConfig.json", false, true)
            .AddJsonFile("SerilogSettings.json", false, true);
    })
    // 2) Logging
    .UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration))
    // 3) DI
    .ConfigureServices((ctx, svcs) =>
    {
        svcs.AddSingleton<DbService>(sp =>
        {
            var cs = ctx.Configuration.GetConnectionString("PostgresConn");
            return new DbService(cs);
        });

        // Base Services
        svcs.AddSingleton<ServerMetrics>();
        svcs.AddSingleton<IRandomService, SystemRandomService>();
        svcs.AddSingleton<ISkillCatalog>(_ => SkillRegistry.Instance);
        svcs.AddSingleton<IWorldScheduler, WorldScheduler>();
        svcs.AddSingleton<IStatusEngine, StatusEngine>();
        svcs.AddSingleton<IEconomyService, EconomyManager>(); // Use Interface
        svcs.AddSingleton<EconomyManager>(sp => (EconomyManager)sp.GetRequiredService<IEconomyService>()); // Forward implementation if needed as concrete

        // Domain Managers
        svcs.AddSingleton<PetManager>();
        svcs.AddSingleton<ServerQuestManager>();
        svcs.AddSingleton<InteractionManager>();
        svcs.AddSingleton<ICombatResolver, StandardCombatResolver>();
        svcs.AddSingleton<CombatManager>();

        svcs.AddSingleton<IPlayerRepository, FilePlayerRepository>();
        svcs.AddSingleton<PlayerService>();
        svcs.AddSingleton<NetworkServer>(sp =>
        {
            var port = ctx.Configuration.GetValue<int>("Network:Port");
            return new NetworkServer(
                port,
                sp.GetRequiredService<DbService>(),
                sp.GetRequiredService<PetManager>(),
                sp.GetRequiredService<ServerQuestManager>(),
                sp.GetRequiredService<CombatManager>(),
                sp.GetRequiredService<InteractionManager>(),
                sp.GetRequiredService<PlayerService>(),
                sp.GetRequiredService<EconomyManager>(),
                sp.GetRequiredService<ServerMetrics>()
            );
        });
        svcs.AddHostedService<ServerWorker>(); // Worker que arranca/para NetworkServer
    })
    .Build()
    .Run();