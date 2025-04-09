namespace InlineSqlSharp;

internal sealed class UnaryFunctionCore(
    string functionName,
    IExpr arg)
{
    private readonly string _functionName = functionName;
    private readonly IExpr _arg = arg;

    internal void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(_functionName)
        .OpenParenthesis()
        .Append(_arg)
        .CloseParenthesis();
}
