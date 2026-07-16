namespace SqlArtisan.IntegrationTests.Schema;

/// <summary>
/// The <c>output_archive</c> table (SQL Server only) — the <c>OUTPUT ... INTO</c>
/// redirect target for the OUTPUT sweep and the archive-then-delete test.
/// </summary>
internal sealed class OutputArchiveTable : DbTableBase
{
    public OutputArchiveTable(string alias = "") : base("output_archive", alias)
    {
        Id = new DbColumn(this, "id");
        Name = new DbColumn(this, "name");
    }

    public DbColumn Id { get; }

    public DbColumn Name { get; }
}
