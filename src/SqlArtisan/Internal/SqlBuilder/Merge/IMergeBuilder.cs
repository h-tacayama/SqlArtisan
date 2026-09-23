namespace SqlArtisan.Internal;

/// <summary>
/// The entry state of a <c>MERGE</c> statement: name the target table.
/// </summary>
public interface IMergeBuilder
{
    /// <summary>
    /// Opens <c>MERGE INTO target</c>; name the data source with <c>Using(...)</c>.
    /// </summary>
    /// <param name="target">The table to merge rows into.</param>
    /// <returns>The builder positioned to accept <c>Using(...).On(...)</c>.</returns>
    /// <remarks>Oracle, PostgreSQL (15+), and SQL Server have <c>MERGE</c>, but only
    /// PostgreSQL and SQL Server take a CTE before it: <c>Build(Dbms.Oracle)</c> throws,
    /// since the CTE belongs inside the subquery <c>Using(...)</c> names.</remarks>
    IMergeBuilderTarget MergeInto(DbTableBase target);
}
