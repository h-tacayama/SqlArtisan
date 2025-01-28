namespace InlineSqlSharp;

public sealed class SelectClause(
	IExprOrAlias primaryItem,
	IExprOrAlias[] secondaryItems) : ISqlElement
{
	private readonly IExprOrAlias _primaryItem = primaryItem;
	private readonly IExprOrAlias[] _secondaryItems = secondaryItems;

	public void FormatSql(ref SqlBuildingBuffer buffer)
	{
		buffer.AppendLine(Keywords.SELECT);

		_primaryItem.FormatAsSelect(ref buffer);

		for (int i = 0; i < _secondaryItems.Length; i++)
		{
			buffer.AppendLine();
			buffer.Append(", ");
			_secondaryItems[i].FormatAsSelect(ref buffer);
		}
	}
}
