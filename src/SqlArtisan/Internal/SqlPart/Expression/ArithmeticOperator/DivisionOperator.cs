namespace SqlArtisan.Internal;

public sealed class DivisionOperator : BinaryOperator
{
    internal DivisionOperator(SqlExpression leftSide, SqlExpression rightSide)
        : base(leftSide, Operators.Slash, rightSide) { }
}
