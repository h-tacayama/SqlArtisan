namespace InlineSqlSharp;

internal sealed class BinaryFunctionCore(
    string functionName,
    AbstractSqlPart arg1,
    AbstractSqlPart arg2)
{
    private readonly string _functionName = functionName;
    private readonly AbstractSqlPart _arg1 = arg1;
    private readonly AbstractSqlPart _arg2 = arg2;

    internal void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(_functionName)
        .OpenParenthesis()
        .Append(_arg1)
        .PrependComma(_arg2)
        .CloseParenthesis();
}
