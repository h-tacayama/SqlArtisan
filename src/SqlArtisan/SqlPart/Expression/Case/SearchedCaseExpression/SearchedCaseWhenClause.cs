using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// A completed <c>WHEN condition THEN result</c> branch of a searched <c>CASE</c>,
/// from <c>Sql.When(condition).Then(...)</c>.
/// </summary>
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

    internal override void Format(SqlBuildingBuffer buffer)
    {
        ConditionGuard.ThrowIfEmpty(_whenCondition, "A CASE WHEN branch requires a condition.");

        buffer
            .Append($"{Keywords.When} ")
            .OpenParenthesis()
            .Append(_whenCondition)
            .CloseParenthesis()
            .EncloseInSpaces(Keywords.Then)
            .Append(_thenExpr);
    }
}
