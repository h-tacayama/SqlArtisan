namespace InlineSqlSharp.TableClassGen;

internal sealed class ALL_TABLES : AbstractTable
{
	public ALL_TABLES(string alias) : base(alias)
	{
		OWNER = new CharacterColumn(alias, nameof(OWNER));
		TABLE_NAME = new CharacterColumn(alias, nameof(TABLE_NAME));
	}

	public CharacterColumn OWNER { get; }

	public CharacterColumn TABLE_NAME { get; }
}
