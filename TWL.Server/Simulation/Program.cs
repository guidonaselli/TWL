using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using TWL.Server;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Database;
using TWL.Server.Persistence.Services;
using TWL.Server.Services;
using TWL.Server.Services.World;
using TWL.Server.Simulation;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Services;
using TWL.Server.Architecture.Pipeline;
using TWL.Server.Features.Combat;
using TWL.Server.Features.Interactions;
using TWL.Shared.Domain.Requests;

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

        // World Services
        svcs.AddSingleton<MapLoader>();
        svcs.AddSingleton<IWorldTriggerService, WorldTriggerService>();
        // Register Handlers? Maybe manually in WorldTriggerService constructor or here?
        // For now WorldTriggerService has RegisterHandler. I'll do it in ServerWorker or WorldTriggerService ctor.
        // Actually WorldTriggerService ctor could check for handlers in DI, but easier to do manual registration for now.

        // Domain Managers
        svcs.AddSingleton<PetManager>();
        svcs.AddSingleton<ServerQuestManager>();
        svcs.AddSingleton<InteractionManager>();
        svcs.AddSingleton<ICombatResolver, StandardCombatResolver>();
        svcs.AddSingleton<CombatManager>();

        svcs.AddSingleton<IPlayerRepository, FilePlayerRepository>();
        svcs.AddSingleton<PlayerService>();
        svcs.AddSingleton<PetService>();

        // Pipeline / Mediator
        svcs.AddSingleton<IMediator>(sp => {
            var mediator = new Mediator();
            // Manual registration of handlers for now
            mediator.Register<UseSkillCommand, CombatResult>(new UseSkillHandler(sp.GetRequiredService<CombatManager>()));
            mediator.Register<InteractCommand, InteractResult>(new InteractHandler(sp.GetRequiredService<InteractionManager>()));
            return mediator;
        });

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
                sp.GetRequiredService<ServerMetrics>(),
                sp.GetRequiredService<PetService>(),
                sp.GetRequiredService<IWorldTriggerService>()
            );
        });
        svcs.AddHostedService<ServerWorker>(); // Worker que arranca/para NetworkServer
    })
    .Build()
    .Run();