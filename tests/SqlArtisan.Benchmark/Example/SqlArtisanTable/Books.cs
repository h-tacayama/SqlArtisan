namespace SqlArtisan.Benchmark.SqlArtisanTable;

internal sealed class Books : DbTableBase
{
    public Books(string alias) : base(alias)
    {
        Id = new DbColumn(alias, "Id");
        Name = new DbColumn(alias, "Name");
        AuthorId = new DbColumn(alias, "AuthorId");
        Rating = new DbColumn(alias, "Rating");
    }

    public DbColumn Id { get; }

    public DbColumn Name { get; }

    public DbColumn AuthorId { get; }

    public DbColumn Rating { get; }
}
