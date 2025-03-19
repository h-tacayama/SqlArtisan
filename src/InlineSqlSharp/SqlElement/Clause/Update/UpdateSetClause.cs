namespace InlineSqlSharp;

internal sealed class UpdateSetClause : ISqlElement
{
	private readonly EqualityCondition[] _assignments;

	internal UpdateSetClause(params IEquality[] assignments)
	{
		_assignments = ArgumentValidator.ThrowIfNotEqualityCondition(assignments);
	}

	public void FormatSql(SqlBuildingBuffer buffer) => buffer
		.AppendSpace(Keywords.SET)
		.AppendCsv(_assignments);
}
