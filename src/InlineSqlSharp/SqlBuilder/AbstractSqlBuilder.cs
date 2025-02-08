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
		buffer.AppendLineSeparated(_elements.ToArray());

	protected SqlCommand BuildCore()
	{
		SqlBuildingBuffer buffer = new();

		try
		{
			buffer.AppendLineSeparated(_elements.ToArray());
			return new(buffer.ToString(), buffer.Parameters);
		}
		finally
		{
			buffer.Dispose();
		}
	}
}
