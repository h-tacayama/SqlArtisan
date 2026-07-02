namespace SqlArtisan.Internal;

internal sealed class WhereClause(SqlCondition condition) : SqlPart
{
    private readonly SqlCondition _condition = condition;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Where).AppendSpace()
        .Append(_condition);
}
