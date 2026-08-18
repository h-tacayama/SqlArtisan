namespace SqlArtisan.Internal;

public sealed class ModulusOperator : BinaryOperator
{
    internal ModulusOperator(SqlExpression leftSide, SqlExpression rightSide)
        : base(leftSide, Operators.Percent, rightSide) { }
}
