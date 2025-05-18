namespace SqlArtisan.TableClassGen;

internal sealed class InformationSchemaTables : DbTableBase
{
    public InformationSchemaTables(string alias = "")
        : base("information_schema.tables", alias)
    {
        TableSchema = new DbColumn(alias, "table_schema");
        TableName = new DbColumn(alias, "table_name");
        TableType = new DbColumn(alias, "table_type");
    }

    public DbColumn TableSchema { get; }

    public DbColumn TableName { get; }

    public DbColumn TableType { get; }
}
