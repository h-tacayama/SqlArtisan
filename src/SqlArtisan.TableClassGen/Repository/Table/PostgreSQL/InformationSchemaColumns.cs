namespace InlineSqlSharp.TableClassGen;

internal sealed class InformationSchemaColumns : AbstractTable
{
    public InformationSchemaColumns(string alias)
        : base("information_schema.columns", alias)
    {
        TableSchema = new Column(alias, "table_schema");
        TableName = new Column(alias, "table_name");
        ColumnName = new Column(alias, "column_name");
        OrdinalPosition = new Column(alias, "ordinal_position");
        DataType = new Column(alias, "data_type");
    }

    public Column TableSchema { get; }

    public Column TableName { get; }

    public Column ColumnName { get; }

    public Column OrdinalPosition { get; }

    public Column DataType { get; }
}
