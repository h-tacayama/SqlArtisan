namespace InlineSqlSharp;

internal sealed class UnaryFunctionCore(
    string functionName,
    AbstractSqlPart arg)
{
    private readonly string _functionName = functionName;
    private readonly AbstractSqlPart _arg = arg;

    internal void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(_functionName)
        .OpenParenthesis()
        .Append(_arg)
        .CloseParenthesis();
}
