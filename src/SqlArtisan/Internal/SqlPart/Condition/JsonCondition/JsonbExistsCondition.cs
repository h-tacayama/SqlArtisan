namespace SqlArtisan.Internal;

public sealed class JsonbExistsCondition(SqlExpression leftSide, SqlExpression rightSide) :
    JsonbCondition(leftSide, Operators.JsonbExists, rightSide);
