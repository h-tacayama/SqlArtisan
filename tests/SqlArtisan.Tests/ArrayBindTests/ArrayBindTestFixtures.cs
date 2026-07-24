namespace SqlArtisan.Tests;

internal sealed class ArrayBindTestTable : DbTableBase
{
    public ArrayBindTestTable(string alias = "") : base("bulk_test", alias)
    {
        Id = new DbColumn(this, "id");
        Code = new DbColumn(this, "code");
        Qty = new DbColumn(this, "qty");
        Price = new DbColumn(this, "price");
        Name = new DbColumn(this, "name");
        CreatedAt = new DbColumn(this, "created_at");
    }

    public DbColumn Id { get; }

    public DbColumn Code { get; }

    public DbColumn Qty { get; }

    public DbColumn Price { get; }

    public DbColumn Name { get; }

    public DbColumn CreatedAt { get; }
}
