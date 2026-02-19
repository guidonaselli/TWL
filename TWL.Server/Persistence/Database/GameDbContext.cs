using Microsoft.EntityFrameworkCore;
using TWL.Server.Persistence.Database.Entities;

namespace TWL.Server.Persistence.Database;

public class GameDbContext : DbContext
{
    public DbSet<PlayerEntity> Players { get; set; } = null!;
    public DbSet<AccountEntity> Accounts { get; set; } = null!;

    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GameDbContext).Assembly);
    }
}
