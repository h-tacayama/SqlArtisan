namespace SqlArtisan.Internal;

public sealed class AdditionOperator(SqlExpression leftSide, SqlExpression rightSide) :
    BinaryOperator(leftSide, Operators.Plus, rightSide);
