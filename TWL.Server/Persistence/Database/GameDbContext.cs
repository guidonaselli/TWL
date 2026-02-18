using System.Text.Json;
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

            // Map QuestData and InstanceLockouts via JSON serialization
            entity.Property(e => e.Quests)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<QuestData>(v, (JsonSerializerOptions)null) ?? new QuestData());

            entity.Property(e => e.InstanceLockouts)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, DateTime>>(v, (JsonSerializerOptions)null) ?? new Dictionary<string, DateTime>());

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
            });
        });
    }
}
