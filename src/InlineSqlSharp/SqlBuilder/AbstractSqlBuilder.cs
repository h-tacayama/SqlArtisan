namespace InlineSqlSharp;

public abstract class AbstractSqlBuilder(ISqlElement primaryElement)
{
	private readonly ISqlElement _primaryElement = primaryElement;
	private readonly List<ISqlElement> _secondaryElements = new();

	protected void AddElement(ISqlElement element)
	{
		_secondaryElements.Add(element);
	}

	protected SqlCommand BuildCore()
	{
		SqlBuildingBuffer buffer = new();

		try
		{
			_primaryElement.FormatSql(ref buffer);

			for (int i = 0; i < _secondaryElements.Count; i++)
			{
				buffer.AppendLine();
				_secondaryElements[i].FormatSql(ref buffer);
			}

			return new(buffer.ToString(), buffer.Parameters);
		}
		finally
		{
			buffer.Dispose();
		}
	}
}
