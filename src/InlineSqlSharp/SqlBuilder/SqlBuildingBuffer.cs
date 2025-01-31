using System.Text;

namespace InlineSqlSharp;

public struct SqlBuildingBuffer(int parameterIndex) : IDisposable
{
	private readonly StringBuilder _statement = new();
	private readonly List<BindParameter> _parameters = new();

	public SqlBuildingBuffer() : this(0)
	{
	}

	public void Dispose()
	{
		_statement.Clear();
	}

	public int ParameterIndex { get; private set; } = parameterIndex;

	public IReadOnlyList<BindParameter> Parameters => _parameters;

	public void BindValue(IBoundValue value)
	{
		BindParameter bindParam = new($":B_{ParameterIndex}", value);
		_parameters.Add(bindParam);
		_statement.Append(bindParam.Name);
		ParameterIndex++;
	}

	public void AddParameter(BindParameter parameter)
	{
		_parameters.Add(parameter);
		ParameterIndex += 1;
	}

	public void AddParameters(IReadOnlyList<BindParameter> parameters)
	{
		_parameters.AddRange(parameters);
		ParameterIndex += parameters.Count;
	}

	public void Append(string? value) => _statement.Append(value);

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

	public void AppendLine(string? value) => _statement.AppendLine(value);

	public void AppendLineIf(bool condition, string? value)
	{
		if (condition)
		{
			_statement.AppendLine(value);
		}
	}

	public override string ToString() => _statement.ToString();
}
