using Microsoft.EntityFrameworkCore;
using TWL.Server.Persistence.Database.Entities;
using TWL.Server.Persistence.Database.Configurations;

namespace TWL.Server.Persistence.Database;

public class GameDbContext : DbContext
{
    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options)
    {
    }

    public DbSet<AccountEntity> Accounts { get; set; }
    public DbSet<PlayerEntity> Players { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new AccountConfiguration());
        modelBuilder.ApplyConfiguration(new PlayerConfiguration());
    }
}
