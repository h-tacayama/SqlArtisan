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

	protected SqlCommand BuildCore(int parameterIndex)
	{
		SqlBuildingBuffer buffer = new(parameterIndex);

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
