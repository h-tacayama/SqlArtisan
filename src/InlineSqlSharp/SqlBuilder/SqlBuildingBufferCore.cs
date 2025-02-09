using System.Text;

namespace InlineSqlSharp;

internal struct SqlBuildingBufferCore(SqlBuildingBuffer parent) : IDisposable
{
	private SqlBuildingBuffer _parent = parent;
	private readonly StringBuilder _statement = new();
	private readonly List<BindParameter> _parameters = [];

	public void Dispose()
	{
		_statement.Clear();
		_parameters.Clear();
	}

	public SqlBuildingBufferCore Append(ISqlElement element)
	{
		element.FormatSql(ref _parent);
		return this;
	}

	public SqlBuildingBufferCore Append(string? value)
	{
		_statement.Append(value);
		return this;
	}

	public SqlBuildingBufferCore AppendCommaSeparated(ISqlElement[] elements)
	{
		if (elements.Length == 0)
		{
			return this;
		}

		elements[0].FormatSql(ref _parent);

		for (int i = 1; i < elements.Length; i++)
		{
			_statement.AppendLine();
			_statement.Append(", ");
			elements[i].FormatSql(ref _parent);
		}

		return this;
	}

	public SqlBuildingBufferCore AppendFormat<T1>(string format, T1 arg1)
	{
		_statement.AppendFormat(format, arg1);
		return this;
	}

	public SqlBuildingBufferCore AppendFormat<T1, T2>(string format, T1 arg1, T2 arg2)
	{
		_statement.AppendFormat(format, arg1, arg2);
		return this;
	}

	public SqlBuildingBufferCore AppendFormat<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3)
	{
		_statement.AppendFormat(format, arg1, arg2, arg3);
		return this;
	}

	public SqlBuildingBufferCore AppendFormatIf<T1>(bool condition, string format, T1 arg1)
	{
		if (condition)
		{
			_statement.AppendFormat(format, arg1);
		}

		return this;
	}

	public SqlBuildingBufferCore AppendLine(ISqlElement element)
	{
		element.FormatSql(ref _parent);
		_statement.AppendLine();
		return this;
	}

	public SqlBuildingBufferCore AppendLine(string? value = null)
	{
		_statement.AppendLine(value);
		return this;
	}

	public SqlBuildingBufferCore AppendLineIf(bool condition, string? value)
	{
		if (condition)
		{
			_statement.AppendLine(value);
		}

		return this;
	}

	public SqlBuildingBufferCore AppendLineSeparated(ISqlElement[] elements)
	{
		if (elements.Length == 0)
		{
			return this;
		}

		elements[0].FormatSql(ref _parent);

		for (int i = 1; i < elements.Length; i++)
		{
			_statement.AppendLine();
			elements[i].FormatSql(ref _parent);
		}

		return this;
	}

	public SqlBuildingBufferCore AppendSpace(ISqlElement element)
	{
		element.FormatSql(ref _parent);
		_statement.Append(" ");
		return this;
	}

	public SqlBuildingBufferCore AppendSpace(string? value = null)
	{
		_statement.AppendFormat("{0} ", value);
		return this;
	}

	public SqlBuildingBufferCore AppendSpaceIf(bool condition, string? value = null)
	{
		if (condition)
		{
			_statement.AppendFormat("{0} ", value);
		}

		return this;
	}

	public SqlBuildingBufferCore AddParameter(IBindValue bindValue)
	{
		int index = _parameters.Count;
		BindParameter parameter = new($":P_{index}", bindValue);
		_parameters.Add(parameter);
		_statement.Append(parameter.Name);
		return this;
	}

	public SqlBuildingBufferCore CloseParenthesis()
	{
		_statement.Append(")");
		return this;
	}

	public SqlBuildingBufferCore CloseParenthesisAfterLine()
	{
		_statement.AppendLine();
		CloseParenthesis();
		return this;
	}

	public SqlBuildingBufferCore EncloseInLines(ISqlElement element)
	{
		OpenParenthesisBeforeLine();
		element.FormatSql(ref _parent);
		CloseParenthesisAfterLine();
		return this;
	}

	public SqlBuildingBufferCore EncloseInLines(string value)
	{
		_statement.AppendLine().AppendLine(value);
		return this;
	}

	public SqlBuildingBufferCore EncloseInSpaces(string value)
	{
		_statement.AppendFormat(" {0} ", value);
		return this;
	}

	public SqlBuildingBufferCore OpenParenthesis()
	{
		_statement.Append("(");
		return this;
	}

	public SqlBuildingBufferCore OpenParenthesisBeforeLine()
	{
		OpenParenthesis();
		_statement.AppendLine();
		return this;
	}

	public SqlBuildingBufferCore PrependLine(string value)
	{
		_statement.AppendLine();
		_statement.Append(value);
		return this;
	}

	public SqlBuildingBufferCore PrependSpace(string value)
	{
		_statement.AppendFormat(" {0}", value);
		return this;
	}

	public SqlCommand ToSqlCommand() =>
		// Return a clone of _parameters to avoid keeping references
		new(_statement.ToString(), _parameters.ToList());
}
