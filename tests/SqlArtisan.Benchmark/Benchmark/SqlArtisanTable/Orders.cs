namespace SqlArtisan.Benchmark.SqlArtisanTable;

internal sealed class Orders : DbTableBase
{
    public Orders(string alias) : base("orders", alias)
    {
        Id = new DbColumn(this, "id");
        UserId = new DbColumn(this, "user_id");
        OrderDate = new DbColumn(this, "order_date");
    }

    public DbColumn Id { get; }

    public DbColumn UserId { get; }

    public DbColumn OrderDate { get; }
}
