namespace SqlArtisan.Internal;

internal sealed class AdditionOperator : BinaryOperator
{
    internal AdditionOperator(SqlExpression leftSide, SqlExpression rightSide)
        : base(leftSide, Operators.Plus, rightSide) { }
}
