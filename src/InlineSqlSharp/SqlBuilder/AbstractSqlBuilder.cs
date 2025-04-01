namespace InlineSqlSharp;

public abstract class AbstractSqlBuilder
{
	private readonly List<ISqlElement> _elements;

	public AbstractSqlBuilder(ISqlElement element)
	{
		_elements = [element];
	}

	protected void AddElement(ISqlElement element)
	{
		_elements.Add(element);
	}

	public void FormatAsSubquery(ref SqlBuildingBuffer buffer) =>
		buffer.AppendSpaceSeparated(_elements.ToArray());

	protected SqlStatement BuildCore() => new SqlBuildingBuffer()
		.AppendSpaceSeparated(_elements.ToArray())
		.ToSqlStatement();
}
