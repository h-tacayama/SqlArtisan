using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    public static CaseElseExpression Else(object thenExpr) =>
        new(Resolve(thenExpr));

    /// <summary>
    /// References a column of the row proposed for insertion inside an UPSERT
    /// update clause (PostgreSQL/SQLite <c>EXCLUDED</c>, MySQL row alias).
    /// </summary>
    public static ExcludedColumn Excluded(DbColumn column) => new(column);

    public static ExistsCondition Exists(ISubquery subquery) => new(subquery);

    public static ExtractFunction Extract(Datepart datepart, object source) =>
        new(datepart, Resolve(source));
}
