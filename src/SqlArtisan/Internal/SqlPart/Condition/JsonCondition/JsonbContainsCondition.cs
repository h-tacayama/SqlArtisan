namespace SqlArtisan.Internal;

public sealed class JsonbContainsCondition : JsonbCondition
{
    internal JsonbContainsCondition(SqlExpression leftSide, SqlExpression rightSide)
        : base(leftSide, Operators.JsonbContains, rightSide) { }
}
