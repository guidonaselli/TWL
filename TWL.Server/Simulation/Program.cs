using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using TWL.Server;
using TWL.Server.Persistence.Database;
using TWL.Server.Simulation;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;

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
        svcs.AddSingleton<ServerQuestManager>();
        svcs.AddSingleton<CombatManager>();
        svcs.AddSingleton<NetworkServer>(sp =>
        {
            var port = ctx.Configuration.GetValue<int>("Network:Port");
            return new NetworkServer(port, sp.GetRequiredService<DbService>(), sp.GetRequiredService<ServerQuestManager>(), sp.GetRequiredService<CombatManager>());
        });
        svcs.AddHostedService<ServerWorker>(); // Worker que arranca/para NetworkServer
    })
    .Build()
    .Run();