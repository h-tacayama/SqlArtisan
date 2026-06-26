using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// The <c>WAIT n</c> behavior for a <c>FOR UPDATE</c> clause: block up to
    /// <paramref name="seconds"/> seconds for a locked row before failing.
    /// </summary>
    /// <param name="seconds">The number of seconds to wait.</param>
    /// <returns>A <c>WAIT</c> lock behavior.</returns>
    public static WaitBehavior Wait(int seconds) => new(seconds);

    /// <summary>
    /// A <c>WHEN condition THEN ...</c> arm of a searched <c>CASE</c> expression.
    /// Complete it with <c>.Then(...)</c> and pass the result to a
    /// <c>Sql.Case(...)</c> overload.
    /// </summary>
    /// <param name="whenCondition">The condition tested by this arm.</param>
    /// <returns>A searched <c>WHEN</c> arm awaiting <c>.Then(...)</c>.</returns>
    public static SearchedCaseWhenCondition When(
        SqlCondition whenCondition) => new(whenCondition);

    /// <summary>
    /// A <c>WHEN value THEN ...</c> arm of a simple <c>CASE expr</c> expression: the
    /// value compared against the case operand. Complete it with <c>.Then(...)</c>
    /// and pass the result to a <c>Sql.Case(expr, ...)</c> overload.
    /// </summary>
    /// <param name="whenExpr">The value compared against the case operand.</param>
    /// <returns>A simple <c>WHEN</c> arm awaiting <c>.Then(...)</c>.</returns>
    public static SimpleCaseWhenExpression When(object whenExpr) =>
        new(Resolve(whenExpr));

    /// <summary>
    /// Begins a <c>WITH</c> clause (Common Table Expressions). Define each CTE with
    /// <see cref="CteBase.As(ISubquery)"/>: subclass <see cref="CteBase"/>
    /// to expose typed columns, or use the inline <see cref="Cte"/> and read its
    /// columns via <see cref="Cte.Column(string)"/>.
    /// </summary>
    /// <param name="ctes">One or more CTE definitions, each produced by <c>cte.As(subquery)</c>.</param>
    /// <returns>A <c>WITH</c> builder positioned for the following statement.</returns>
    public static IWithBuilderWith With(
        params CommonTableExpression[] ctes) =>
        new WithBuilder(new WithClause(ctes));

    /// <summary>
    /// Begins a <c>WITH RECURSIVE</c> clause. CTEs are defined as for
    /// <see cref="With(CommonTableExpression[])"/> — via <see cref="Cte"/> or a
    /// <see cref="CteBase"/> subclass.
    /// </summary>
    /// <param name="ctes">One or more CTE definitions, each produced by <c>cte.As(subquery)</c>.</param>
    /// <returns>A <c>WITH RECURSIVE</c> builder positioned for the following statement.</returns>
    public static IWithBuilderWith WithRecursive(
        params CommonTableExpression[] ctes) =>
        new WithBuilder(new WithRecursiveClause(ctes));
}
