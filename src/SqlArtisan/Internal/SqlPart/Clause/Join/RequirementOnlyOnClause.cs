namespace SqlArtisan.Internal;

// No CROSS JOIN remedy where it would not hold: a joined UPDATE or DELETE has no CrossJoin
// stage, and an outer join keeps rows a CROSS JOIN drops when a side is empty.
internal sealed class RequirementOnlyOnClause(SqlCondition condition) : OnClause(condition)
{
    private protected override string EmptyMessage => "A JOIN's ON clause requires a condition.";
}
