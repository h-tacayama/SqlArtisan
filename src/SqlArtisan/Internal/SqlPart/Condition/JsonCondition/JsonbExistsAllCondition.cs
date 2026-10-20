namespace SqlArtisan.Internal;

internal sealed class JsonbExistsAllCondition : JsonbCondition
{
    internal JsonbExistsAllCondition(SqlExpression leftSide, SqlExpression[] keys)
        : base(leftSide, Operators.JsonbExistsAll, new ArrayConstructorExpression(keys)) { }
}
