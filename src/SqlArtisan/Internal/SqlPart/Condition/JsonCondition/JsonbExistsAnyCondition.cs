namespace SqlArtisan.Internal;

internal sealed class JsonbExistsAnyCondition : JsonbCondition
{
    internal JsonbExistsAnyCondition(SqlExpression leftSide, SqlExpression[] keys)
        : base(leftSide, Operators.JsonbExistsAny, new ArrayConstructorExpression(keys)) { }
}
