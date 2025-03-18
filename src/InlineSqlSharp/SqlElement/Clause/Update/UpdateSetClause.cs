namespace InlineSqlSharp;

internal sealed class UpdateSetClause : ISqlElement
{
	private readonly EqualityCondition[] _assignments;

	internal UpdateSetClause(params IEquality[] assignments)
	{
		_assignments = ArgumentValidator.ThrowIfNotEqualityCondition(assignments);
	}

	public void FormatSql(SqlBuildingBuffer buffer) => buffer
		.AppendLine(Keywords.SET)
		.AppendCsvLines(_assignments);
}
