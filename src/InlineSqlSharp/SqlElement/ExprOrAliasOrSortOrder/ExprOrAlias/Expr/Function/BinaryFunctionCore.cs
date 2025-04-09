namespace InlineSqlSharp;

internal sealed class BinaryFunctionCore(
    string functionName,
    IExpr arg1,
    IExpr arg2)
{
    private readonly string _functionName = functionName;
    private readonly IExpr _arg1 = arg1;
    private readonly IExpr _arg2 = arg2;

    internal void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(_functionName)
        .OpenParenthesis()
        .Append(_arg1)
        .PrependComma(_arg2)
        .CloseParenthesis();
}
