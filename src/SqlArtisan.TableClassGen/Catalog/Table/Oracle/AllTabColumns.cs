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
        Nullable = new DbColumn(this, "nullable");
        DefaultLength = new DbColumn(this, "default_length");
        IdentityColumn = new DbColumn(this, "identity_column");
    }

    public DbColumn Owner { get; }

    public DbColumn TableName { get; }

    public DbColumn ColumnName { get; }

    public DbColumn DataType { get; }

    public DbColumn ColumnId { get; }

    public DbColumn Nullable { get; }

    // The length of DATA_DEFAULT rather than the value: the value is a LONG, and
    // its presence is the only thing this needs.
    public DbColumn DefaultLength { get; }

    public DbColumn IdentityColumn { get; }
}
