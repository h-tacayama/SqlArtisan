using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// MERGE (Oracle / SQL Server). Follow with
    /// <c>Using(source).On(cond).WhenMatchedThenUpdateSet(...).WhenNotMatchedThenInsert(...).Values(...)</c>.
    /// </summary>
    public static IMergeBuilderUsing MergeInto(DbTableBase target) =>
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
