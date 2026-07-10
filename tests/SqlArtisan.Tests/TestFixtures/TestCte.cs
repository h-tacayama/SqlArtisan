namespace SqlArtisan.Tests;

internal sealed class TestCte : CteBase
{
    public TestCte(string name) : base(name)
    {
        CteCode = new DbColumn(this, "cte_code");
        CteName = new DbColumn(this, "cte_name");
        CteCreatedAt = new DbColumn(this, "cte_created_at");
    }

    public DbColumn CteCode { get; }

    public DbColumn CteName { get; }

    public DbColumn CteCreatedAt { get; }
}
