namespace InlineSqlSharp;

public interface IExprOrAlias : IExprOrAliasOrSortOrder
{
	void FormatAsSelect(ref SqlBuildingBuffer buffer);
}
