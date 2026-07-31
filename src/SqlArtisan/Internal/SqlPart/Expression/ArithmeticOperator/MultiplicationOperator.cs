namespace SqlArtisan.Internal;

public sealed class MultiplicationOperator(SqlExpression leftSide, SqlExpression rightSide) :
    BinaryOperator(leftSide, Operators.Asterisk, rightSide);
