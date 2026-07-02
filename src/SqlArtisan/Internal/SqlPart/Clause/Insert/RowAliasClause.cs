namespace SqlArtisan.Internal;

// Emits the MySQL 8.0.19+ row alias (`AS new`) so ON DUPLICATE KEY UPDATE can
// reference the proposed row as `new.column` instead of the deprecated VALUES().
internal sealed class RowAliasClause : SqlPart
{
    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.As).AppendSpace()
        .AppendExcludedName();
}
