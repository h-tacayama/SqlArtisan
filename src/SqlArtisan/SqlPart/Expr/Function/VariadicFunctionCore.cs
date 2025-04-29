namespace InlineSqlSharp;

internal sealed class VariadicFunctionCore(
    string functionName,
    params AbstractSqlPart?[] args)
{
    private readonly string _functionName = functionName;
    private readonly AbstractSqlPart?[] _args = args;

    internal void FormatSql(SqlBuildingBuffer buffer)
    {
        buffer.Append(_functionName)
            .OpenParenthesis();

        if (_args.Length > 0
            && _args[0] is not null)
        {
            _args[0]?.FormatSql(buffer);

            for (int i = 1; i < _args.Length; i++)
            {
                if (_args[i] is null)
                {
                    break;
                }

                buffer.Append(", ");
                _args[i]?.FormatSql(buffer);
            }
        }

        buffer.CloseParenthesis();
    }
}
