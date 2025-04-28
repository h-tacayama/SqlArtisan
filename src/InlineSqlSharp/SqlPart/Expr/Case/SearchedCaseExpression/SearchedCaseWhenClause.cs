namespace InlineSqlSharp;

public sealed class SearchedCaseWhenClause : AbstractSqlPart
{
    private readonly SearchedCaseWhenCondition _whenCondition;
    private readonly CaseThenExpr _thenExpr;

    internal SearchedCaseWhenClause(
        SearchedCaseWhenCondition whenCondition,
        CaseThenExpr thenExpr)
    {
        _whenCondition = whenCondition;
        _thenExpr = thenExpr;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.When)
        .OpenParenthesis()
        .Append(_whenCondition)
        .CloseParenthesis()
        .EncloseInSpaces(Keywords.Then)
        .Append(_thenExpr);
}
