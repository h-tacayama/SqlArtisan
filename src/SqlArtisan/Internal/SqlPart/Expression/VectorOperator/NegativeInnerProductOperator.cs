namespace SqlArtisan.Internal;

public sealed class NegativeInnerProductOperator(SqlExpression leftSide, SqlExpression rightSide) :
    VectorOperator(leftSide, Operators.NegativeInnerProduct, rightSide);
