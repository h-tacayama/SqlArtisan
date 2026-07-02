namespace SqlArtisan.Internal;

public sealed class NotCondition : SqlCondition
{
    private readonly SqlCondition _condition;

    internal NotCondition(SqlCondition condition)
    {
        _condition = condition;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Not).AppendSpace()
        .EncloseInParentheses(_condition);
}
