namespace InlineSqlSharp;

public interface IExprOrAlias : IExprOrAliasOrSortOrder
{
	void FormatAsSelect(SqlBuildingBuffer buffer);
}
