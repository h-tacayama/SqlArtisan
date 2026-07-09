namespace SqlArtisan.Internal;

public sealed class NotCondition : SqlCondition
{
    private readonly SqlCondition _condition;

    internal NotCondition(SqlCondition condition)
    {
        _condition = condition;
    }

    // `NOT` over an empty operand is itself empty — a plain AND/OR walk would
    // otherwise let `NOT ()` through in a mixed all-empty subtree.
    internal override bool IsEmpty => _condition.IsEmpty;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Not} ")
        .EncloseInParentheses(_condition);
}
