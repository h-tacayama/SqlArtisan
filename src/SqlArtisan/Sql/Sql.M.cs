using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// Starts a <c>MERGE INTO target</c> statement (Oracle / SQL Server, and
    /// PostgreSQL 15+). Continue with <c>Using(...).On(...)</c> and one or more
    /// <c>WhenMatched</c> / <c>WhenNotMatched</c> branches. The emitted SQL is
    /// per-dialect by design — for example, SQL Server appends the required
    /// terminating semicolon and supports <c>WHEN NOT MATCHED BY SOURCE</c>.
    /// </summary>
    public static IMergeBuilderTarget MergeInto(DbTableBase target) =>
        new MergeBuilder(new MergeIntoClause(target));

    public static MaxFunction Max(object expr) => new(Resolve(expr));

    public static MinFunction Min(object expr) => new(Resolve(expr));

    public static ModFunction Mod(
        object dividend,
        object divisor) => new(
            Resolve(dividend),
            Resolve(divisor));

    public static MonthsBetweenFunction MonthsBetween(
        object date1,
        object date2) => new(
            Resolve(date1),
            Resolve(date2));
}
