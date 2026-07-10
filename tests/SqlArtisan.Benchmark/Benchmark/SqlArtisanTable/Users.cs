namespace SqlArtisan.Benchmark.SqlArtisanTable;

internal sealed class Users : DbTableBase
{
    public Users(string alias) : base("users", alias)
    {
        Id = new DbColumn(this, "id");
        Name = new DbColumn(this, "name");
        CreatedAt = new DbColumn(this, "created_at");
    }

    public DbColumn Id { get; }

    public DbColumn Name { get; }

    public DbColumn CreatedAt { get; }
}
