namespace InlineSqlSharp.TableClassGen;

internal sealed class AllTabColumns : AbstractTable
{
    public AllTabColumns(string alias) : base("all_tab_columns", alias)
    {
        Owner = new Column(alias, "owner");
        TableName = new Column(alias, "table_name");
        ColumnName = new Column(alias, "column_name");
        DataType = new Column(alias, "data_type");
        ColumnId = new Column(alias, "column_id");
    }

    public Column Owner { get; }

    public Column TableName { get; }

    public Column ColumnName { get; }

    public Column DataType { get; }

    public Column ColumnId { get; }
}
