namespace InlineSqlSharp.TableClassGen;

internal sealed class information_schema_columns : AbstractTable
{
	public information_schema_columns(string alias)
		: base("information_schema.columns", alias)
	{
		table_schema = new CharacterColumn(alias, nameof(table_schema));
		table_name = new CharacterColumn(alias, nameof(table_name));
		column_name = new CharacterColumn(alias, nameof(column_name));
		ordinal_position = new NumericColumn(alias, nameof(ordinal_position));
		data_type = new CharacterColumn(alias, nameof(data_type));
	}

	public CharacterColumn table_schema { get; }

	public CharacterColumn table_name { get; }

	public CharacterColumn column_name { get; }

	public NumericColumn ordinal_position { get; }

	public CharacterColumn data_type { get; }
}
