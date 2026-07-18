namespace SqlArtisan.Internal;

public sealed class ArrayOverlapsCondition(SqlExpression leftSide, SqlExpression rightSide) :
    ArrayCondition(leftSide, Operators.ArrayOverlaps, rightSide);
