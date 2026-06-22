namespace SqlArtisan.Internal;

/// <summary>
/// Base type for a GROUP BY grouping extension (<c>ROLLUP</c> / <c>CUBE</c> /
/// <c>GROUPING SETS</c>) that <c>GroupBy(...)</c> accepts alongside plain
/// grouping columns. Built with <c>Sql.Rollup(...)</c>, <c>Sql.Cube(...)</c>, or
/// <c>Sql.GroupingSets(...)</c>.
/// </summary>
public abstract class GroupingElement : SqlPart
{
}
