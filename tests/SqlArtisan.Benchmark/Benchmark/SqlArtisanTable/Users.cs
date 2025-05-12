namespace SqlArtisan.Benchmark.SqlArtisanTable;

internal sealed class Users : DbTableBase
{
    public Users(string alias) : base("users", alias)
    {
        Id = new DbColumn(alias, "id");
        Name = new DbColumn(alias, "name");
        CreatedAt = new DbColumn(alias, "created_at");
    }

    public DbColumn Id { get; }

    public DbColumn Name { get; }

    public DbColumn CreatedAt { get; }
}
