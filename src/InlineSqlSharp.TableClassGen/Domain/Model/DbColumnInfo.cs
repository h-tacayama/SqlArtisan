namespace InlineSqlSharp.TableClassGen;

internal sealed class DbColumnInfo(string name, string dataType)
{
	public string Name => name;

	public string DataType => dataType;

	public string GetCSharpType()
	{
		return DataType.ToLowerInvariant() switch
		{
			"number"
			or "numeric"
			or "decimal"
			or "int"
			or "integer"
			or "smallint"
			or "bigint" => nameof(NumericColumn),
			"char"
			or "character"
			or "varchar"
			or "varchar2"
			or "text"
			or "nvarchar"
			or "character varying" => nameof(CharacterColumn),
			"date"
			or "timestamp"
			or "datetime"
			or "timestamp with time zone"
			or "timestamp without time zone" => nameof(DateTimeColumn),
			_ => ""
		};
	}
}
