namespace SqlArtisan.Internal;

public abstract class SqlPart
{
    internal abstract void Format(SqlBuildingBuffer buffer);

    // True when this condition carries nothing runnable. Used to skip an excluded
    // operand inside a non-empty AND/OR (ConditionIf's contract) and, via
    // EmptyConditionGuard, to reject a written clause whose whole condition is
    // empty (the #236 empty-state policy). Default false; the condition types
    // override it. The check is recursive — an AND/OR/NOT tree whose operands are
    // all empty is itself empty.
    internal virtual bool IsEmpty => false;
}
