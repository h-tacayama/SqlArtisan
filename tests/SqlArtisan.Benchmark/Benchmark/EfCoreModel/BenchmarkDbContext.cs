using Microsoft.EntityFrameworkCore;

namespace SqlArtisan.Benchmark.EfCoreModel;

public sealed class BenchmarkDbContext : DbContext
{
    // A dummy connection string is enough: ToQueryString() / CreateDbCommand() build
    // SQL through the Npgsql provider without ever opening a connection.
    private const string ConnectionString =
        "Host=localhost;Database=benchmark;Username=benchmark;Password=benchmark";

    public DbSet<User> Users => Set<User>();

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.UseNpgsql(ConnectionString);

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<Order>()
            .HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId);
}
