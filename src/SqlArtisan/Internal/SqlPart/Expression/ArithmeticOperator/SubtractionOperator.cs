namespace SqlArtisan.Internal;

public sealed class SubtractionOperator(SqlExpression leftSide, SqlExpression rightSide) :
    BinaryOperator(leftSide, Operators.Minus, rightSide);
