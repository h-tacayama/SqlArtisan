namespace SqlArtisan.Tests;

internal sealed class TestCte : CteSchemaBase
{
    public TestCte(string name) : base(name)
    {
        CteCode = new DbColumn(name, "cte_code");
        CteName = new DbColumn(name, "cte_name");
        CteCreatedAt = new DbColumn(name, "cte_created_at");
    }

    public DbColumn CteCode { get; }

    public DbColumn CteName { get; }

    public DbColumn CteCreatedAt { get; }
}
