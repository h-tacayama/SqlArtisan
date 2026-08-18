namespace SqlArtisan.Internal;

public sealed class AdditionOperator : BinaryOperator
{
    internal AdditionOperator(SqlExpression leftSide, SqlExpression rightSide)
        : base(leftSide, Operators.Plus, rightSide) { }
}
