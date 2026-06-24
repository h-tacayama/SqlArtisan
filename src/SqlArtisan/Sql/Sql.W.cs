using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    public static WaitBehavior Wait(int seconds) => new(seconds);

    public static SearchedCaseWhenCondition When(
        SqlCondition whenCondition) => new(whenCondition);

    public static SimpleCaseWhenExpression When(object whenExpr) =>
        new(Resolve(whenExpr));

    /// <summary>
    /// Begins a <c>WITH</c> clause (Common Table Expressions). Define each CTE with
    /// <see cref="CteSchemaBase.As(ISubquery)"/>: subclass <see cref="CteSchemaBase"/>
    /// to expose typed columns, or use the inline <see cref="Cte"/> and read its
    /// columns via <see cref="Cte.Column(string)"/>.
    /// </summary>
    /// <param name="ctes">One or more CTE definitions, each produced by <c>cte.As(subquery)</c>.</param>
    public static IWithBuilderWith With(
        params CommonTableExpression[] ctes) =>
        new WithBuilder(new WithClause(ctes));

    /// <summary>
    /// Begins a <c>WITH RECURSIVE</c> clause. CTEs are defined as for
    /// <see cref="With(CommonTableExpression[])"/> — via <see cref="Cte"/> or a
    /// <see cref="CteSchemaBase"/> subclass.
    /// </summary>
    /// <param name="ctes">One or more CTE definitions, each produced by <c>cte.As(subquery)</c>.</param>
    public static IWithBuilderWith WithRecursive(
        params CommonTableExpression[] ctes) =>
        new WithBuilder(new WithRecursiveClause(ctes));
}
