namespace SqlArtisan;

public sealed class SearchedCaseWhenClause : SqlPart
{
    private readonly SearchedCaseWhenCondition _whenCondition;
    private readonly CaseThenExpression _thenExpr;

    internal SearchedCaseWhenClause(
        SearchedCaseWhenCondition whenCondition,
        CaseThenExpression thenExpr)
    {
        _whenCondition = whenCondition;
        _thenExpr = thenExpr;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.When} ")
        .OpenParenthesis()
        .Append(_whenCondition)
        .CloseParenthesis()
        .EncloseInSpaces(Keywords.Then)
        .Append(_thenExpr);
}
