using Microsoft.EntityFrameworkCore;
using TWL.Server.Persistence;

namespace TWL.Server.Persistence.Database;

public class GameDbContext : DbContext
{
    public DbSet<PlayerEntity> Players { get; set; }

    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PlayerEntity>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();

            // Map the Data property to a JSONB column
            entity.OwnsOne(e => e.Data, builder =>
            {
                builder.ToJson();

                builder.OwnsMany(d => d.Inventory);
                builder.OwnsMany(d => d.Equipment);
                builder.OwnsMany(d => d.Bank);

                builder.OwnsMany(d => d.Pets, pb =>
                {
                    // UnlockedSkillIds is List<int>, typically works
                });

                builder.OwnsMany(d => d.Skills);

                // Dictionaries are marked as [NotMapped] in the Entity class
                // to avoid migration issues for now.
                // We will add proper dictionary support (flattening or value conversion)
                // in PERS-001b or subsequent steps if strictly required by EF Core.
                // Note: EF Core 9+ might support some dictionaries but it's flaky in preview.
            });
        });
    }
}
