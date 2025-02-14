namespace InlineSqlSharp;

public sealed class ExceptOperator(bool all) : ISqlElement
{
	private readonly bool _all = all;

	public void FormatSql(ref SqlBuildingBuffer buffer) => buffer
		.Append(Keywords.EXCEPT)
		.PrependSpaceIf(_all, Keywords.ALL);
}
