namespace InlineSqlSharp.TableClassGen;

internal sealed class ALL_TAB_COLUMNS : AbstractTable
{
    public ALL_TAB_COLUMNS(string alias) : base(alias)
    {
        OWNER = new Column(alias, nameof(OWNER));
        TABLE_NAME = new Column(alias, nameof(TABLE_NAME));
        COLUMN_NAME = new Column(alias, nameof(COLUMN_NAME));
        DATA_TYPE = new Column(alias, nameof(DATA_TYPE));
        COLUMN_ID = new Column(alias, nameof(COLUMN_ID));
    }

    public Column OWNER { get; }

    public Column TABLE_NAME { get; }

    public Column COLUMN_NAME { get; }

    public Column DATA_TYPE { get; }

    public Column COLUMN_ID { get; }
}
