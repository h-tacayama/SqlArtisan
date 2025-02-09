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
		buffer.Core.AppendLineSeparated(_elements.ToArray());

	protected SqlCommand BuildCore()
	{
		SqlBuildingBuffer buffer = new();

		try
		{
			buffer.Core.AppendLineSeparated(_elements.ToArray());
			return buffer.Core.ToSqlCommand();
		}
		finally
		{
			buffer.Dispose();
		}
	}
}
