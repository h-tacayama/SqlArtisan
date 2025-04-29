namespace InlineSqlSharp.TableClassGen;

internal sealed class AllTables : AbstractTable
{
    public AllTables(string alias) : base("all_tables", alias)
    {
        Owner = new Column(alias, "owner");
        TableName = new Column(alias, "table_name");
    }

    public Column Owner { get; }

    public Column TableName { get; }
}
