namespace SqlArtisan.Internal;

public sealed class ArrayContainsCondition(SqlExpression leftSide, SqlExpression rightSide) :
    ArrayCondition(leftSide, Operators.ArrayContains, rightSide);
