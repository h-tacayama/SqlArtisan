namespace InlineSqlSharp;

internal sealed class UpdateSetClause : ISqlElement
{
	private readonly EqualityCondition[] _assignments;

	internal UpdateSetClause(params IEquality[] assignments)
	{
		_assignments = new EqualityCondition[assignments.Length];

		for (int i = 0; i < assignments.Length; i++)
		{
			if (assignments[i] is not EqualityCondition)
			{
				throw new ArgumentException("All assignments must be EqualityCondition.");
			}

			_assignments[i] = (EqualityCondition)assignments[i];
		}
	}

	public void FormatSql(SqlBuildingBuffer buffer) => buffer
		.AppendLine(Keywords.SET)
		.AppendCsvLines(_assignments);
}
