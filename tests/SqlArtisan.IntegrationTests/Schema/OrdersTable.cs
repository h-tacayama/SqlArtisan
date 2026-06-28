namespace SqlArtisan.IntegrationTests.Schema;

/// <summary>
/// The <c>orders</c> table used across the integration matrix, joined to
/// <see cref="UsersTable"/> by <c>user_id</c>.
/// </summary>
internal sealed class OrdersTable : DbTableBase
{
    public OrdersTable(string alias = "") : base("orders", alias)
    {
        Id = new DbColumn(alias, "id");
        UserId = new DbColumn(alias, "user_id");
        Amount = new DbColumn(alias, "amount");
    }

    public DbColumn Id { get; }

    public DbColumn UserId { get; }

    public DbColumn Amount { get; }
}
