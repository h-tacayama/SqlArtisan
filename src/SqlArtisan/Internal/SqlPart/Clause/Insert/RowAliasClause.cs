namespace SqlArtisan.Internal;

// Emits the MySQL 8.0.19+ row alias (`AS new`) on every ON DUPLICATE KEY UPDATE, so Excluded
// reads `new.column`, not the deprecated VALUES(): only a walk of each assignment's tree,
// which no build has, could tell whether one reads it (#675).
internal sealed class RowAliasClause : SqlPart
{
    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.As} ")
        .AppendExcludedName();
}
