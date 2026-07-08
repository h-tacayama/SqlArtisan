namespace SqlArtisan.Internal;

public abstract class SqlPart
{
    internal abstract void Format(SqlBuildingBuffer buffer);

    // True when this part renders no SQL, so a space-separated clause list skips
    // it instead of emitting a dangling separator (the #236 empty-state policy).
    // Default false; conditions and the clauses that wrap them override this. For
    // a condition the check is recursive — an AND/OR/NOT tree whose operands are
    // all empty is itself empty.
    internal virtual bool IsEmpty => false;
}
