namespace InlineSqlSharp.TableClassGen;

internal sealed class information_schema_tables : Table
{
	public information_schema_tables(string alias)
		: base("information_schema.tables", alias)
	{
		table_schema = new CharacterColumn(alias, nameof(table_schema));
		table_name = new CharacterColumn(alias, nameof(table_name));
		table_type = new CharacterColumn(alias, nameof(table_type));
	}

	public CharacterColumn table_schema { get; }

	public CharacterColumn table_name { get; }

	public CharacterColumn table_type { get; }
}
