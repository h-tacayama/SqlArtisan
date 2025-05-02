namespace SqlArtisan.Benchmark.SqlArtisanTable;

internal sealed class Authors : DbTableBase
{
    public Authors(string alias) : base(alias)
    {
        Id = new DbColumn(alias, "Id");
        Name = new DbColumn(alias, "Name");
    }

    public DbColumn Id { get; }

    public DbColumn Name { get; }
}
