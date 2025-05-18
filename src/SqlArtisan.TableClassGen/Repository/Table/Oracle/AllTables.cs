namespace SqlArtisan.TableClassGen;

internal sealed class AllTables : DbTableBase
{
    public AllTables(string alias = "") : base("all_tables", alias)
    {
        Owner = new DbColumn(alias, "owner");
        TableName = new DbColumn(alias, "table_name");
    }

    public DbColumn Owner { get; }

    public DbColumn TableName { get; }
}
