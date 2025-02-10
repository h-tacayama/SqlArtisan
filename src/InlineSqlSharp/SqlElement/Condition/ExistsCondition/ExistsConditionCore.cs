namespace InlineSqlSharp;

public sealed class ExistsConditionCore(bool isNot, ISubquery subquery)
{
	private readonly bool _isNot = isNot;
	private readonly ISubquery _subquery = subquery;

	public void FormatSql(ref SqlBuildingBuffer buffer) =>
		buffer.Core
			.AppendSpaceIf(_isNot, Keywords.NOT)
			.AppendLine(Keywords.EXISTS)
			.EncloseInLines(_subquery);
}
