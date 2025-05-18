namespace SqlArtisan.TableClassGen;

internal sealed class AllTabColumns : DbTableBase
{
    public AllTabColumns(string alias = "") : base("all_tab_columns", alias)
    {
        Owner = new DbColumn(alias, "owner");
        TableName = new DbColumn(alias, "table_name");
        ColumnName = new DbColumn(alias, "column_name");
        DataType = new DbColumn(alias, "data_type");
        ColumnId = new DbColumn(alias, "column_id");
    }

    public DbColumn Owner { get; }

    public DbColumn TableName { get; }

    public DbColumn ColumnName { get; }

    public DbColumn DataType { get; }

    public DbColumn ColumnId { get; }
}
