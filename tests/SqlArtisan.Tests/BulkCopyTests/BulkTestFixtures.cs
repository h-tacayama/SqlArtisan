namespace SqlArtisan.Tests;

internal sealed class BulkTestTable : DbTableBase
{
    public BulkTestTable(string alias = "") : base("bulk_test", alias)
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

internal sealed class EmptyBulkTestTable : DbTableBase
{
    public EmptyBulkTestTable() : base("bulk_test", string.Empty)
    {
    }
}

internal sealed class BulkTestRow
{
    public long Id { get; init; }

    public int? Code { get; init; }

    public short Qty { get; init; }

    public decimal Price { get; init; }

    public string? Name { get; init; }

    public DateTime CreatedAt { get; init; }
}

internal sealed class BulkTestRowMissingProperty
{
    public long Id { get; init; }

    public int? Code { get; init; }

    public short Qty { get; init; }

    public decimal Price { get; init; }

    public string? Name { get; init; }
}

internal sealed class BulkTestRowUnsupportedType
{
    public long Id { get; init; }

    public int? Code { get; init; }

    public bool Qty { get; init; }

    public decimal Price { get; init; }

    public string? Name { get; init; }

    public DateTime CreatedAt { get; init; }
}
