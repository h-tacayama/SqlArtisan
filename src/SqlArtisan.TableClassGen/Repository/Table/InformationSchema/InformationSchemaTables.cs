namespace SqlArtisan.TableClassGen;

internal sealed class InformationSchemaTables : DbTableBase
{
    public InformationSchemaTables(string alias = "")
        : base("information_schema.tables", alias)
    {
        TableSchema = new DbColumn(this, "table_schema");
        TableName = new DbColumn(this, "table_name");
        TableType = new DbColumn(this, "table_type");
    }

    public DbColumn TableSchema { get; }

    public new DbColumn TableName { get; }

    public DbColumn TableType { get; }
}
