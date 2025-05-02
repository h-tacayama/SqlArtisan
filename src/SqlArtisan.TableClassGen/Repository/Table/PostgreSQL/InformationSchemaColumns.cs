namespace SqlArtisan.TableClassGen;

internal sealed class InformationSchemaColumns : DbTableBase
{
    public InformationSchemaColumns(string alias)
        : base("information_schema.columns", alias)
    {
        TableSchema = new DbColumn(alias, "table_schema");
        TableName = new DbColumn(alias, "table_name");
        ColumnName = new DbColumn(alias, "column_name");
        OrdinalPosition = new DbColumn(alias, "ordinal_position");
        DataType = new DbColumn(alias, "data_type");
    }

    public DbColumn TableSchema { get; }

    public DbColumn TableName { get; }

    public DbColumn ColumnName { get; }

    public DbColumn OrdinalPosition { get; }

    public DbColumn DataType { get; }
}
