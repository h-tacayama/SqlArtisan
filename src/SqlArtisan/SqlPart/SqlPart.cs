using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// The base of every piece of SQL the library renders: expressions, conditions,
/// clauses, and the markers and branches the factories hand back.
/// </summary>
public abstract class SqlPart
{
    private protected SqlPart()
    {
    }

    internal abstract void Format(SqlBuildingBuffer buffer);

    // True when a condition renders nothing — recursive over AND/OR/NOT. Lets a
    // non-empty AND/OR skip an empty operand, and ConditionGuard reject an empty clause.
    internal virtual bool IsEmpty => false;
}
