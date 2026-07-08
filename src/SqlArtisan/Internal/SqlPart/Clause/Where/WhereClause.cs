namespace SqlArtisan.Internal;

internal sealed class WhereClause(SqlCondition condition) : SqlPart
{
    private readonly SqlCondition _condition = condition;

    // An all-empty condition elides the whole clause (a search screen's
    // all-filters-off default) on SELECT; UPDATE/DELETE reject it before Format.
    internal override bool IsEmpty => _condition.IsEmpty;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Where} ")
        .Append(_condition);
}
