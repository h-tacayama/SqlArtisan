namespace InlineSqlSharp;

public abstract class AbstractSqlBuilder<TDerived>
	where TDerived : AbstractSqlBuilder<TDerived>
{
	private readonly List<ISqlElement> _elements;

	public AbstractSqlBuilder(ISqlElement element)
	{
		_elements = [element];
	}

	protected TDerived AddElement(ISqlElement element)
	{
		_elements.Add(element);
		return (TDerived)this;
	}

	public void FormatAsSubquery(ref SqlBuildingBuffer buffer) =>
		buffer.AppendLineSeparated(_elements.ToArray());

	protected SqlCommand BuildCore() => new SqlBuildingBuffer()
		.AppendLineSeparated(_elements.ToArray())
		.ToSqlCommand();
}
