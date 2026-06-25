namespace SqlArtisan.Tests;

// A typed derived-table schema: columns exposed as members, for an APPLY / LATERAL
// source whose columns are referenced repeatedly.
internal sealed class TestDerivedTable : DerivedTableBase
{
    public TestDerivedTable(string name) : base(name)
    {
        Code = new DbColumn(name, "code");
        Total = new DbColumn(name, "total");
    }

    public DbColumn Code { get; }

    public DbColumn Total { get; }
}
