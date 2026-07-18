namespace SqlArtisan.Internal;

public sealed class ArrayContainedByCondition(SqlExpression leftSide, SqlExpression rightSide) :
    ArrayCondition(leftSide, Operators.ArrayContainedBy, rightSide);
