namespace SqlArtisan.Internal;

// Stringified once at construction, not per Format — the flag set is fixed at
// the call site (ADR 0006; WaitBehavior's shape).
internal sealed class RegexpOptionsValue(RegexpOptions options) : SqlPart
{
    private readonly string _sql = options.ToSql();

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(_sql);
}
