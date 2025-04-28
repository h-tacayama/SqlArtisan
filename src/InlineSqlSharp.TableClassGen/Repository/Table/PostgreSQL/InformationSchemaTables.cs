namespace InlineSqlSharp.TableClassGen;

internal sealed class InformationSchemaTables : AbstractTable
{
    public InformationSchemaTables(string alias)
        : base("information_schema.tables", alias)
    {
        TableSchema = new Column(alias, "table_schema");
        TableName = new Column(alias, "table_name");
        TableType = new Column(alias, "table_type");
    }

    public Column TableSchema { get; }

    public Column TableName { get; }

    public Column TableType { get; }
}
