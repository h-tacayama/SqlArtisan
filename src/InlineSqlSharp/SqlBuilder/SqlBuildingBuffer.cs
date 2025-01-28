using System.Text;

namespace InlineSqlSharp;

public struct SqlBuildingBuffer : IDisposable
{
	private readonly StringBuilder _statement = new();

	public SqlBuildingBuffer()
	{
	}

	public void Dispose()
	{
		_statement.Clear();
	}

	public void Append(string value) => _statement.Append(value);

	public void AppendFormat<T1>(string format, T1 arg1) =>
		_statement.AppendFormat(format, arg1);

	public void AppendFormat<T1, T2>(string format, T1 arg1, T2 arg2) =>
		_statement.AppendFormat(format, arg1, arg2);

	public void AppendFormat<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3) =>
		_statement.AppendFormat(format, arg1, arg2, arg3);

	public void AppendFormatIf<T1>(bool condition, string format, T1 arg1)
	{
		if (condition)
		{
			_statement.AppendFormat(format, arg1);
		}
	}

	public void AppendLine() => _statement.AppendLine();

	public void AppendLine(string value) => _statement.AppendLine(value);

	public void AppendLineIf(bool condition, string value)
	{
		if (condition)
		{
			_statement.AppendLine(value);
		}
	}

	public override string ToString() => _statement.ToString();
}
