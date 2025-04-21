namespace InlineSqlSharp.TableClassGen;

internal sealed class information_schema_columns : AbstractTable
{
    public information_schema_columns(string alias)
        : base("information_schema.columns", alias)
    {
        table_schema = new Column(alias, nameof(table_schema));
        table_name = new Column(alias, nameof(table_name));
        column_name = new Column(alias, nameof(column_name));
        ordinal_position = new Column(alias, nameof(ordinal_position));
        data_type = new Column(alias, nameof(data_type));
    }

    public Column table_schema { get; }

    public Column table_name { get; }

    public Column column_name { get; }

    public Column ordinal_position { get; }

    public Column data_type { get; }
}
