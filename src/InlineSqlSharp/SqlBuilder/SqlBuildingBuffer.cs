using System.Text;

namespace InlineSqlSharp;

public sealed class SqlBuildingBuffer()
{
	private readonly StringBuilder _statement = new();
	private readonly List<BindParameter> _parameters = [];

	internal SqlBuildingBuffer Append(ISqlElement element)
	{
		element.FormatSql(this);
		return this;
	}

	internal SqlBuildingBuffer Append(string? value)
	{
		_statement.Append(value);
		return this;
	}

	internal SqlBuildingBuffer AppendCommaSeparated(ISqlElement[] elements)
	{
		if (elements.Length == 0)
		{
			return this;
		}

		elements[0].FormatSql(this);

		for (int i = 1; i < elements.Length; i++)
		{
			_statement.AppendLine();
			_statement.Append(", ");
			elements[i].FormatSql(this);
		}

		return this;
	}

	internal SqlBuildingBuffer AppendLine(ISqlElement element)
	{
		element.FormatSql(this);
		_statement.AppendLine();
		return this;
	}

	internal SqlBuildingBuffer AppendLine(string? value = null)
	{
		_statement.AppendLine(value);
		return this;
	}

	internal SqlBuildingBuffer AppendLineIf(bool condition, ISqlElement element)
	{
		if (condition)
		{
			element.FormatSql(this);
			_statement.AppendLine();
		}

		return this;
	}

	internal SqlBuildingBuffer AppendLineIf(bool condition, string? value)
	{
		if (condition)
		{
			_statement.AppendLine(value);
		}

		return this;
	}

	internal SqlBuildingBuffer AppendLineSeparated(ISqlElement[] elements)
	{
		if (elements.Length == 0)
		{
			return this;
		}

		elements[0].FormatSql(this);

		for (int i = 1; i < elements.Length; i++)
		{
			_statement.AppendLine();
			elements[i].FormatSql(this);
		}

		return this;
	}

	internal SqlBuildingBuffer AppendSpace(ISqlElement element)
	{
		element.FormatSql(this);
		_statement.Append(" ");
		return this;
	}

	internal SqlBuildingBuffer AppendSpace(string? value = null)
	{
		_statement.Append(value);
		_statement.Append(" ");
		return this;
	}

	internal SqlBuildingBuffer AppendSpaceIf(bool condition, string? value = null)
	{
		if (condition)
		{
			_statement.Append(value);
			_statement.Append(" ");
		}

		return this;
	}

	internal SqlBuildingBuffer AppendUnaryFunction(string functionName, ISqlElement arg)
	{
		_statement.Append(functionName);
		_statement.Append("(");
		arg.FormatSql(this);
		_statement.Append(")");
		return this;
	}

	internal SqlBuildingBuffer AddParameter(IBindValue bindValue)
	{
		int index = _parameters.Count;
		BindParameter parameter = new($":P_{index}", bindValue);
		_parameters.Add(parameter);
		_statement.Append(parameter.Name);
		return this;
	}

	internal SqlBuildingBuffer CloseParenthesisAfterLine()
	{
		_statement.AppendLine();
		_statement.Append(")");
		return this;
	}

	internal SqlBuildingBuffer EncloseInParentheses(ISqlElement element)
	{
		_statement.AppendLine("(");
		element.FormatSql(this);
		_statement.AppendLine();
		_statement.Append(")");
		return this;
	}

	internal SqlBuildingBuffer EncloseInLines(string value)
	{
		_statement.AppendLine();
		_statement.AppendLine(value);
		return this;
	}

	internal SqlBuildingBuffer EncloseInSpaces(string value)
	{
		_statement.Append(" ");
		_statement.Append(value);
		_statement.Append(" ");
		return this;
	}

	internal SqlBuildingBuffer OpenParenthesisBeforeLine()
	{
		_statement.AppendLine("(");
		return this;
	}

	internal SqlBuildingBuffer PrependLine(string value)
	{
		_statement.AppendLine();
		_statement.Append(value);
		return this;
	}

	internal SqlBuildingBuffer PrependSpace(string value)
	{
		_statement.Append(" ");
		_statement.Append(value);
		return this;
	}

	internal SqlBuildingBuffer PrependSpaceIf(bool condition, string value)
	{
		if (condition)
		{
			_statement.Append(" ");
			_statement.Append(value);
		}

		return this;
	}

	internal SqlCommand ToSqlCommand() =>
		// Return a clone of _parameters to avoid keeping references
		new(_statement.ToString(), [.. _parameters]);
}
