namespace InlineSqlSharp;

public sealed class SearchedCaseWhenClause(
    SearchedCaseWhenCondition whenCondition,
    CaseThenExpr thenExpr) : ISqlElement
{
    private readonly SearchedCaseWhenCondition _whenCondition = whenCondition;
    private readonly CaseThenExpr _thenExpr = thenExpr;

    public void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.WHEN)
        .OpenParenthesis()
        .Append(_whenCondition)
        .CloseParenthesis()
        .EncloseInSpaces(Keywords.THEN)
        .Append(_thenExpr);
}
