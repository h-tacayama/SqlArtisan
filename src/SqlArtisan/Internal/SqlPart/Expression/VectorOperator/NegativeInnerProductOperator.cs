namespace SqlArtisan.Internal;

public sealed class NegativeInnerProductOperator(SqlExpression leftSide, SqlExpression rightSide) :
    BinaryOperator(leftSide, Operators.NegativeInnerProduct, rightSide);
