namespace SqlArtisan.Internal;

// A joined UPDATE or DELETE has no CrossJoin stage, so its message names no CROSS JOIN
// remedy: a remedy must be reachable from the chain that threw.
internal sealed class DmlOnClause(SqlCondition condition) : OnClause(condition)
{
    private protected override string EmptyMessage => "A JOIN's ON clause requires a condition.";
}
