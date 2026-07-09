namespace SqlArtisan.Internal;

public abstract class SqlPart
{
    internal abstract void Format(SqlBuildingBuffer buffer);

    // True when a condition renders nothing — recursive over AND/OR/NOT. Lets a
    // non-empty AND/OR skip an empty operand, and ConditionGuard reject an empty clause.
    internal virtual bool IsEmpty => false;
}
