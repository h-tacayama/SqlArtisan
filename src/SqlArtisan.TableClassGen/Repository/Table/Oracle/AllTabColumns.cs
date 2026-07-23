namespace SqlArtisan.TableClassGen;

internal sealed class AllTabColumns : DbTableBase
{
    public AllTabColumns(string alias = "") : base("all_tab_columns", alias)
    {
        Owner = new DbColumn(this, "owner");
        TableName = new DbColumn(this, "table_name");
        ColumnName = new DbColumn(this, "column_name");
        DataType = new DbColumn(this, "data_type");
        ColumnId = new DbColumn(this, "column_id");
    }

    public DbColumn Owner { get; }

    public new DbColumn TableName { get; }

    public DbColumn ColumnName { get; }

    public DbColumn DataType { get; }

    public DbColumn ColumnId { get; }
}
