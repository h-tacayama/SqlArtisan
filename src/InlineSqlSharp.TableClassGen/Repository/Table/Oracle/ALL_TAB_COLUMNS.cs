namespace InlineSqlSharp.TableClassGen;

internal sealed class ALL_TAB_COLUMNS : AbstractTable
{
    public ALL_TAB_COLUMNS(string alias) : base(alias)
    {
        OWNER = new CharacterColumn(alias, nameof(OWNER));
        TABLE_NAME = new CharacterColumn(alias, nameof(TABLE_NAME));
        COLUMN_NAME = new CharacterColumn(alias, nameof(COLUMN_NAME));
        DATA_TYPE = new CharacterColumn(alias, nameof(DATA_TYPE));
        COLUMN_ID = new NumericColumn(alias, nameof(COLUMN_ID));
    }

    public CharacterColumn OWNER { get; }

    public CharacterColumn TABLE_NAME { get; }

    public CharacterColumn COLUMN_NAME { get; }

    public CharacterColumn DATA_TYPE { get; }

    public NumericColumn COLUMN_ID { get; }
}
