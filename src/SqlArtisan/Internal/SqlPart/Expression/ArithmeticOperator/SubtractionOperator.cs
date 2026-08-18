namespace SqlArtisan.Internal;

public sealed class SubtractionOperator : BinaryOperator
{
    internal SubtractionOperator(SqlExpression leftSide, SqlExpression rightSide)
        : base(leftSide, Operators.Minus, rightSide) { }
}
