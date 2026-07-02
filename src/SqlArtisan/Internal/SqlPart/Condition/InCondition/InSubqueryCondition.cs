namespace SqlArtisan.Internal;

public sealed class InSubqueryCondition : SqlCondition
{
    private readonly SqlExpression _leftSide;
    private readonly ISubquery _subquery;

    internal InSubqueryCondition(SqlExpression leftSide, ISubquery subquey)
    {
        _leftSide = leftSide;
        _subquery = subquey;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(_leftSide)
        .EncloseInSpaces(Keywords.In)
        .EncloseInParentheses(_subquery);
}
