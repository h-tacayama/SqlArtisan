using System.Text;

namespace InlineSqlSharp;

public struct SqlBuildingBuffer() : IDisposable
{
	private readonly StringBuilder _statement = new();
	private readonly List<BindParameter> _parameters = [];

	public void Dispose()
	{
		_statement.Clear();
		_parameters.Clear();
	}

	public SqlBuildingBuffer Append(ISqlElement element)
	{
		element.FormatSql(ref this);
		return this;
	}

	public SqlBuildingBuffer Append(string? value)
	{
		_statement.Append(value);
		return this;
	}

	public SqlBuildingBuffer AppendCommaSeparated(ISqlElement[] elements)
	{
		if (elements.Length == 0)
		{
			return this;
		}

		elements[0].FormatSql(ref this);

		for (int i = 1; i < elements.Length; i++)
		{
			_statement.AppendLine();
			_statement.Append(", ");
			elements[i].FormatSql(ref this);
		}

		return this;
	}

	public SqlBuildingBuffer AppendFormat<T1>(string format, T1 arg1)
	{
		_statement.AppendFormat(format, arg1);
		return this;
	}

	public SqlBuildingBuffer AppendFormat<T1, T2>(string format, T1 arg1, T2 arg2)
	{
		_statement.AppendFormat(format, arg1, arg2);
		return this;
	}

	public SqlBuildingBuffer AppendFormat<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3)
	{
		_statement.AppendFormat(format, arg1, arg2, arg3);
		return this;
	}

	public SqlBuildingBuffer AppendFormatIf<T1>(bool condition, string format, T1 arg1)
	{
		if (condition)
		{
			_statement.AppendFormat(format, arg1);
		}

		return this;
	}

	public SqlBuildingBuffer AppendLine(ISqlElement element)
	{
		element.FormatSql(ref this);
		_statement.AppendLine();
		return this;
	}

	public SqlBuildingBuffer AppendLine(string? value = null)
	{
		_statement.AppendLine(value);
		return this;
	}

	public SqlBuildingBuffer AppendLineIf(bool condition, string? value)
	{
		if (condition)
		{
			_statement.AppendLine(value);
		}

		return this;
	}

	public SqlBuildingBuffer AppendLineSeparated(ISqlElement[] elements)
	{
		if (elements.Length == 0)
		{
			return this;
		}

		elements[0].FormatSql(ref this);

		for (int i = 1; i < elements.Length; i++)
		{
			_statement.AppendLine();
			elements[i].FormatSql(ref this);
		}

		return this;
	}

	public SqlBuildingBuffer AppendSpace(ISqlElement element)
	{
		element.FormatSql(ref this);
		_statement.Append(" ");
		return this;
	}

	public SqlBuildingBuffer AppendSpace(string? value = null)
	{
		_statement.AppendFormat("{0} ", value);
		return this;
	}

	public SqlBuildingBuffer AppendSpaceIf(bool condition, string? value = null)
	{
		if (condition)
		{
			_statement.AppendFormat("{0} ", value);
		}

		return this;
	}

	public SqlBuildingBuffer AddParameter(IBindValue bindValue)
	{
		int index = _parameters.Count;
		BindParameter parameter = new($":P_{index}", bindValue);
		_parameters.Add(parameter);
		_statement.Append(parameter.Name);
		return this;
	}

	public SqlBuildingBuffer CloseParenthesis()
	{
		_statement.Append(")");
		return this;
	}

	public SqlBuildingBuffer CloseParenthesisAfterLine()
	{
		_statement.AppendLine();
		CloseParenthesis();
		return this;
	}

	public SqlBuildingBuffer EncloseInLines(ISqlElement element)
	{
		OpenParenthesisBeforeLine();
		element.FormatSql(ref this);
		CloseParenthesisAfterLine();
		return this;
	}

	public SqlBuildingBuffer EncloseInLines(string value)
	{
		_statement.AppendLine().AppendLine(value);
		return this;
	}

	public SqlBuildingBuffer EncloseInSpaces(string value)
	{
		_statement.AppendFormat(" {0} ", value);
		return this;
	}

	public SqlBuildingBuffer OpenParenthesis()
	{
		_statement.Append("(");
		return this;
	}

	public SqlBuildingBuffer OpenParenthesisBeforeLine()
	{
		OpenParenthesis();
		_statement.AppendLine();
		return this;
	}

	public SqlBuildingBuffer PrependLine(string value)
	{
		_statement.AppendLine();
		_statement.Append(value);
		return this;
	}

	public SqlBuildingBuffer PrependSpace(string value)
	{
		_statement.AppendFormat(" {0}", value);
		return this;
	}

	public SqlCommand ToSqlCommand() =>
		// Return a clone of _parameters to avoid keeping references
		new(_statement.ToString(), _parameters.ToList());
}
