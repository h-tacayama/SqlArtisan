namespace SqlArtisan.Internal;

public abstract class SqlPart
{
    internal abstract void Format(SqlBuildingBuffer buffer);

    // True when this condition carries nothing runnable. Skips an excluded operand
    // inside a non-empty AND/OR (ConditionIf's contract) and, via ConditionGuard,
    // rejects a written clause whose whole condition is empty (the #236 empty-state
    // policy). Recursive — an AND/OR/NOT tree of all-empty operands is itself empty.
    internal virtual bool IsEmpty => false;
}
