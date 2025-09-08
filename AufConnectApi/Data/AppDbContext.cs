using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AufConnectApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Project> Projects { get; set; }
    public DbSet<Resource> Resources { get; set; }
    public DbSet<ResourceSection> ResourceSections { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<EventSection> EventSections { get; set; }
    public DbSet<Member> Members { get; set; }
    public DbSet<Partner> Partners { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Project>(entity =>
        {
            entity.Property(e => e.RoleOfAufInAction)
                .HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                );

            entity.Property(e => e.OperationalPartners)
                .HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                );
        });

        modelBuilder.Entity<Resource>(entity =>
        {
            entity.HasMany(r => r.Sections)
                .WithOne(s => s.Resource)
                .HasForeignKey(s => s.ResourceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasMany(e => e.Sections)
                .WithOne(s => s.Event)
                .HasForeignKey(s => s.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasOne(u => u.NotificationPreferences)
                .WithOne(n => n.User)
                .HasForeignKey<NotificationPreferences>(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}