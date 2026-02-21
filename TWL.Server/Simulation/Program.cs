using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Serilog;
using TWL.Server.Architecture.Pipeline;
using TWL.Server.Features.Combat;
using TWL.Server.Features.Interactions;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Database;
using TWL.Server.Persistence.Repositories;
using TWL.Server.Persistence.Services;
using TWL.Server.Security;
using TWL.Server.Services;
using TWL.Server.Services.World;
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
        cb.AddJsonFile("Persistence/ServerConfig.json", false, true)
            .AddJsonFile("Persistence/SerilogSettings.json", false, true);
    })
    // 2) Logging
    .UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration))
    // 3) DI
    .ConfigureServices((ctx, svcs) =>
    {
        var connString = ctx.Configuration.GetConnectionString("PostgresConn");

        // NpgsqlDataSource — shared connection pool for both EF Core and Dapper
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connString);
        var dataSource = dataSourceBuilder.Build();
        svcs.AddSingleton(dataSource);

        // EF Core — factory pattern (required because DbPlayerRepository is a singleton
        // and cannot inject a scoped/transient DbContext directly)
        svcs.AddDbContextFactory<GameDbContext>(opts =>
        {
            opts.UseNpgsql(dataSource);
        });

        // DbService (Singleton wrapper for legacy code + new migration trigger)
        svcs.AddSingleton<DbService>(sp =>
        {
            // Note: DbService takes IServiceProvider to create scopes for EF Core
            return new DbService(connString, sp);
        });

        // Base Services
        svcs.AddSingleton<ServerMetrics>();
        svcs.AddSingleton<IRandomService>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var seed = config.GetValue<int?>("Server:RandomSeed");
            return new SeedableRandomService(sp.GetRequiredService<ILogger<SeedableRandomService>>(), seed);
        });
        svcs.AddSingleton<ISkillCatalog>(_ => SkillRegistry.Instance);
        svcs.AddSingleton<IWorldScheduler, WorldScheduler>();
        svcs.AddSingleton<IStatusEngine, StatusEngine>();
        svcs.AddSingleton<IEconomyService>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var ledgerFile = config["Economy:LedgerFile"] ?? "economy_ledger.log";
            var secret = config["Economy:ProviderSecret"];
            return new EconomyManager(ledgerFile, secret);
        });
        svcs.AddSingleton<EconomyManager>(sp =>
            (EconomyManager)sp.GetRequiredService<IEconomyService>()); // Forward implementation if needed as concrete

        // World Services
        svcs.AddSingleton<MapLoader>();
        svcs.AddSingleton<IMapRegistry, MapRegistry>();
        svcs.AddSingleton<IWorldTriggerService, WorldTriggerService>();
        // Register Handlers? Maybe manually in WorldTriggerService constructor or here?
        // For now WorldTriggerService has RegisterHandler. I'll do it in ServerWorker or WorldTriggerService ctor.
        // Actually WorldTriggerService ctor could check for handlers in DI, but easier to do manual registration for now.

        // Domain Managers
        svcs.AddSingleton<InstanceService>();
        svcs.AddSingleton<PetManager>();
        svcs.AddSingleton<MonsterManager>();
        svcs.AddSingleton<SpawnManager>();
        svcs.AddSingleton<ServerQuestManager>();
        svcs.AddSingleton<InteractionManager>();
        svcs.AddSingleton<ICombatResolver, StandardCombatResolver>();
        svcs.AddSingleton<CombatManager>();

        svcs.AddSingleton<IPlayerRepository>(sp =>
            new DbPlayerRepository(
                sp.GetRequiredService<IDbContextFactory<GameDbContext>>(),
                sp.GetRequiredService<NpgsqlDataSource>(),
                sp.GetRequiredService<ILogger<DbPlayerRepository>>()));
        svcs.AddSingleton<PlayerService>();
        svcs.AddSingleton<PetService>();

        // Pipeline / Mediator
        svcs.AddSingleton<IMediator>(sp =>
        {
            var mediator = new Mediator();
            // Manual registration of handlers for now
            mediator.Register(new UseSkillHandler(sp.GetRequiredService<CombatManager>()));
            mediator.Register(new InteractHandler(sp.GetRequiredService<InteractionManager>()));
            return mediator;
        });

        // Security: Replay protection
        svcs.AddSingleton(new ReplayGuardOptions());
        svcs.AddSingleton<ReplayGuard>();

        // Security: Movement validation
        svcs.AddSingleton(new MovementValidationOptions());
        svcs.AddSingleton<MovementValidator>();

        svcs.AddSingleton<INetworkServer>(sp =>
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
                sp.GetRequiredService<IMediator>(),
                sp.GetRequiredService<IWorldTriggerService>(),
                sp.GetRequiredService<SpawnManager>(),
                sp.GetRequiredService<ReplayGuard>(),
                sp.GetRequiredService<MovementValidator>()
            );
        });
        svcs.AddSingleton<HealthCheckService>();
        svcs.AddHostedService<HealthCheckService>(sp => sp.GetRequiredService<HealthCheckService>());
        svcs.AddHostedService<ServerWorker>(); // Worker que arranca/para NetworkServer
    })
    .Build()
    .Run();
