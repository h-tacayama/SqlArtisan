namespace SqlArtisan;

public sealed class EqualityCondition(
    AbstractExpr leftSide,
    AbstractExpr rightSide) : AbstractEqualityCondition
{
    internal override AbstractExpr LeftSide => leftSide;

    internal override AbstractExpr RightSide => rightSide;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(LeftSide)
        .Append($" {Operators.Equality} ")
        .Append(RightSide);
}
