namespace SqlArtisan.Internal;

public sealed class NegativeInnerProductOperator : BinaryOperator
{
    internal NegativeInnerProductOperator(SqlExpression leftSide, SqlExpression rightSide)
        : base(leftSide, Operators.NegativeInnerProduct, rightSide) { }
}
