namespace SqlArtisan.Internal;

public sealed class JsonHashArrowTextOperator : BinaryOperator
{
    internal JsonHashArrowTextOperator(SqlExpression leftSide, SqlExpression rightSide)
        : base(leftSide, Operators.JsonHashArrowText, rightSide) { }
}
