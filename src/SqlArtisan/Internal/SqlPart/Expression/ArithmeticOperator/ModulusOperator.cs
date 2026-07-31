namespace SqlArtisan.Internal;

public sealed class ModulusOperator(SqlExpression leftSide, SqlExpression rightSide) :
    BinaryOperator(leftSide, Operators.Percent, rightSide);
