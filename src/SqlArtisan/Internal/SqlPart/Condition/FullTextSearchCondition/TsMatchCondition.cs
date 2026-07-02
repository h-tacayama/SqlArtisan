namespace SqlArtisan.Internal;

/// <summary>
/// The PostgreSQL text-search match predicate <c>vector @@ query</c>: whether the
/// tsvector matches the tsquery.
/// </summary>
public sealed class TsMatchCondition : SqlCondition
{
    private readonly SqlExpression _vector;
    private readonly SqlExpression _query;

    internal TsMatchCondition(SqlExpression vector, SqlExpression query)
    {
        _vector = vector;
        _query = query;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(_vector)
        .Append($" {Operators.TsMatch} ")
        .Append(_query);
}
