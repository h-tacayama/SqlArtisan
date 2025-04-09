namespace InlineSqlSharp;

internal sealed class VariadicFunctionCore(
    string functionName,
    params ISqlElement?[] arguments)
{
    private readonly string _functionName = functionName;
    private readonly ISqlElement?[] _arguments = arguments;

    internal void FormatSql(SqlBuildingBuffer buffer)
    {
        buffer.Append(_functionName)
            .OpenParenthesis();

        if (_arguments.Length > 0
            && _arguments[0] is not null)
        {
            _arguments[0]?.FormatSql(buffer);

            for (int i = 1; i < _arguments.Length; i++)
            {
                if (_arguments[i] is null)
                {
                    break;
                }

                buffer.Append(", ");
                _arguments[i]?.FormatSql(buffer);
            }
        }

        buffer.CloseParenthesis();
    }
}
