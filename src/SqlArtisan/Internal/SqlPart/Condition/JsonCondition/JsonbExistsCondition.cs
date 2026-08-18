namespace SqlArtisan.Internal;

public sealed class JsonbExistsCondition : JsonbCondition
{
    internal JsonbExistsCondition(SqlExpression leftSide, SqlExpression rightSide)
        : base(leftSide, Operators.JsonbExists, rightSide) { }
}
