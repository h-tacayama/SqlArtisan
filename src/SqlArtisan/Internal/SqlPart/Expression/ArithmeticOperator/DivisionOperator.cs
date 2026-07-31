namespace SqlArtisan.Internal;

public sealed class DivisionOperator(SqlExpression leftSide, SqlExpression rightSide) :
    BinaryOperator(leftSide, Operators.Slash, rightSide);
