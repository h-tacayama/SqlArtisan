namespace InlineSqlSharp;

internal sealed class AllOrDistinctFunctionCore(
    string functionName,
    AllOrDistinct allOrDistinct,
    AbstractSqlPart expr)
{
    private readonly string _functionName = functionName;
    private readonly AllOrDistinct _allOrDistinct = allOrDistinct;
    private readonly AbstractSqlPart _expr = expr;

    internal void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(_functionName)
        .OpenParenthesis()
        .AppendSpaceIf(_allOrDistinct.IsDistinct, _allOrDistinct)
        .Append(_expr)
        .CloseParenthesis();
}
