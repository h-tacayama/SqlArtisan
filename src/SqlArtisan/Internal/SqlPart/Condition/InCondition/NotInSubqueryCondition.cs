namespace SqlArtisan.Internal;

public sealed class NotInSubqueryCondition : SqlCondition
{
    private readonly SqlExpression _leftSide;
    private readonly ISubquery _subquery;

    internal NotInSubqueryCondition(SqlExpression leftSide, ISubquery subquey)
    {
        _leftSide = leftSide;
        _subquery = subquey;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(_leftSide)
        .EncloseInSpaces($"{Keywords.Not} {Keywords.In}")
        .EncloseInParentheses(_subquery);
}
