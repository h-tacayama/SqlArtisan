namespace SqlArtisan.Internal;

public sealed class JsonbContainsCondition(SqlExpression leftSide, SqlExpression rightSide) :
    JsonbCondition(leftSide, Operators.JsonbContains, rightSide);
