namespace SqlArtisan.Internal;

public sealed class JsonbExistsAnyCondition(SqlExpression leftSide, SqlExpression[] keys) :
    JsonbCondition(leftSide, Operators.JsonbExistsAny, new ArrayConstructorExpression(keys));
