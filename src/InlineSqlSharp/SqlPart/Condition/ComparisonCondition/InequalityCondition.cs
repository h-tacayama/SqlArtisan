namespace InlineSqlSharp;

internal sealed class InequalityCondition(
    AbstractExpr leftSide,
    AbstractExpr rightSide) : AbstractEqualityCondition
{
    internal override AbstractExpr LeftSide => leftSide;

    internal override AbstractExpr RightSide => rightSide;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(LeftSide)
        .AppendSpace(Operators.Inequality)
        .Append(RightSide);
}
