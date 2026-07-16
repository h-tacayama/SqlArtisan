namespace SqlArtisan.Tests;

// An OUTPUT ... INTO archive target for the SQL Server OUTPUT tests.
internal sealed class ArchiveTable : DbTableBase
{
    public ArchiveTable(string alias = "") : base("archive_table", alias)
    {
        Code = new DbColumn(this, "code");
        Name = new DbColumn(this, "name");
    }

    public DbColumn Code { get; }

    public DbColumn Name { get; }
}
