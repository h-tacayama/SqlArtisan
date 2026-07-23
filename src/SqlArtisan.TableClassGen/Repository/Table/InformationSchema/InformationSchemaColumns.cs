namespace SqlArtisan.TableClassGen;

internal sealed class InformationSchemaColumns : DbTableBase
{
    public InformationSchemaColumns(string alias = "")
        : base("information_schema.columns", alias)
    {
        TableSchema = new DbColumn(this, "table_schema");
        TableName = new DbColumn(this, "table_name");
        ColumnName = new DbColumn(this, "column_name");
        OrdinalPosition = new DbColumn(this, "ordinal_position");
        DataType = new DbColumn(this, "data_type");
    }

    public DbColumn TableSchema { get; }

    public new DbColumn TableName { get; }

    public DbColumn ColumnName { get; }

    public DbColumn OrdinalPosition { get; }

    public DbColumn DataType { get; }
}
