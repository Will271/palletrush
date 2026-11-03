using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace League.Api.Data;

public class LeagueDbContext(DbContextOptions<LeagueDbContext> options) : DbContext(options)
{
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Heat> Heats => Set<Heat>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<Run> Runs => Set<Run>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Run>().Property(r => r.Id).ValueGeneratedNever();

        modelBuilder.Entity<Registration>()
            .HasIndex(r => new { r.HeatId, r.DriverId })
            .IsUnique()
            .HasDatabaseName("ux_registrations_heat_driver");

        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.SetTableName(ToSnakeCase(entity.GetTableName()!));
            foreach (var property in entity.GetProperties())
                property.SetColumnName(ToSnakeCase(property.Name));
        }
    }

    private static string ToSnakeCase(string name) =>
        Regex.Replace(name, "(?<=[a-z0-9])([A-Z])", "_$1").ToLowerInvariant();
}
