namespace InlineSqlSharp;

internal sealed class AllOrDistinctFunctionCore(
    string functionName,
    Distinct? distinct,
    AbstractSqlPart expr)
{
    private readonly string _functionName = functionName;
    private readonly Distinct? _distinct = distinct;
    private readonly AbstractSqlPart _expr = expr;

    internal void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(_functionName)
        .OpenParenthesis()
        .AppendSpaceIfNotNull(_distinct)
        .Append(_expr)
        .CloseParenthesis();
}
