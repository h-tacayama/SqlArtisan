namespace SqlArtisan.Benchmark.SqlArtisanTable;

internal sealed class Orders : DbTableBase
{
    public Orders(string alias) : base("orders", alias)
    {
        Id = new DbColumn(alias, "id");
        UserId = new DbColumn(alias, "user_id");
        OrderDate = new DbColumn(alias, "order_date");
    }

    public DbColumn Id { get; }

    public DbColumn UserId { get; }

    public DbColumn OrderDate { get; }
}
