namespace InlineSqlSharp.TableClassGen;

internal sealed class information_schema_tables : AbstractTable
{
    public information_schema_tables(string alias)
        : base("information_schema.tables", alias)
    {
        table_schema = new Column(alias, nameof(table_schema));
        table_name = new Column(alias, nameof(table_name));
        table_type = new Column(alias, nameof(table_type));
    }

    public Column table_schema { get; }

    public Column table_name { get; }

    public Column table_type { get; }
}
