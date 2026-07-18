namespace SqlArtisan.Internal;

public sealed class JsonbExistsAllCondition(SqlExpression leftSide, SqlExpression[] keys) :
    JsonbCondition(leftSide, Operators.JsonbExistsAll, new ArrayConstructorExpression(keys));
