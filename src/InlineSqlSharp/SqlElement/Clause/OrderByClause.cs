namespace InlineSqlSharp;

public sealed class OrderByClause(
	IExprOrAliasOrSortOrder[] sortExpressions) : ISqlElement
{
	private readonly IExprOrAliasOrSortOrder[] _sortExpressions = sortExpressions;

	public void FormatSql(ref SqlBuildingBuffer buffer) =>
		buffer.Core.AppendSpace(Keywords.ORDER)
			.AppendLine(Keywords.BY)
			.AppendCommaSeparated(_sortExpressions);
}
