namespace InlineSqlSharp.TableClassGen;

internal sealed class ALL_TABLES : AbstractTable
{
    public ALL_TABLES(string alias) : base(alias)
    {
        OWNER = new Column(alias, nameof(OWNER));
        TABLE_NAME = new Column(alias, nameof(TABLE_NAME));
    }

    public Column OWNER { get; }

    public Column TABLE_NAME { get; }
}
