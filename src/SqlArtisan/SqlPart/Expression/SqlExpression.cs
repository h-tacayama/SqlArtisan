using System.Diagnostics;
using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

/// <summary>
/// A SQL value expression — the base type of every column reference, literal,
/// function call, and operator result. Hold a computed value as this type — a
/// helper method's return type, a variable built up conditionally — without
/// naming any concrete expression class.
/// </summary>
public abstract class SqlExpression : SqlPart
{
    /// <summary>
    /// Gets the ascending <c>ORDER BY</c> sort direction for this expression
    /// (<c>expr ASC</c>).
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder Asc => new(this, SortDirection.Asc);

    /// <summary>
    /// Gets the descending <c>ORDER BY</c> sort direction for this expression
    /// (<c>expr DESC</c>).
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder Desc => new(this, SortDirection.Desc);

    /// <summary>
    /// Gets the <c>expr IS NULL</c> condition.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public IsNullCondition IsNull => new(this);

    /// <summary>
    /// Gets the <c>expr IS NOT NULL</c> condition.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public IsNotNullCondition IsNotNull => new(this);

    /// <summary>
    /// Gets the <c>ORDER BY</c> suffix that sorts this expression's <see
    /// langword="null"/> values first (<c>expr NULLS FIRST</c>).
    /// </summary>
    /// <remarks>Not available on MySQL or SQL Server; SQLite 3.30+.</remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder NullsFirst => new(this, NullOrdering.NullsFirst);

    /// <summary>
    /// Gets the <c>ORDER BY</c> suffix that sorts this expression's <see
    /// langword="null"/> values last (<c>expr NULLS LAST</c>).
    /// </summary>
    /// <remarks>Not available on MySQL or SQL Server; SQLite 3.30+.</remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder NullsLast => new(this, NullOrdering.NullsLast);

    /// <summary>
    /// Compares this expression to <paramref name="obj"/> by reference. Two
    /// distinct expressions that emit identical SQL are not equal.
    /// </summary>
    /// <param name="obj">The object to compare against.</param>
    /// <returns><see langword="true"/> if <paramref name="obj"/> is this same instance.</returns>
    public override bool Equals(object? obj) => base.Equals(obj);

    /// <summary>
    /// Gets the reference-based hash code for this expression.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() => base.GetHashCode();

    /// <summary>
    /// The SQL equality comparison: <c><paramref name="this"/> = <paramref name="rightSide"/></c>.
    /// </summary>
    /// <param name="this">The left operand.</param>
    /// <param name="rightSide">The right operand — a literal, another expression, or a scalar subquery.</param>
    /// <returns>The equality condition.</returns>
    public static EqualityBasedCondition operator ==(
        SqlExpression @this,
        object rightSide) =>
        new EqualityCondition(Resolve(@this), Resolve(rightSide));

    /// <summary>
    /// The SQL inequality comparison: <c><paramref name="this"/> &lt;&gt; <paramref name="rightSide"/></c>.
    /// </summary>
    /// <param name="this">The left operand.</param>
    /// <param name="rightSide">The right operand — a literal, another expression, or a scalar subquery.</param>
    /// <returns>The inequality condition.</returns>
    public static EqualityBasedCondition operator !=(
        SqlExpression @this,
        object rightSide) =>
        new InequalityCondition(Resolve(@this), Resolve(rightSide));

    /// <summary>
    /// The SQL less-than comparison: <c><paramref name="this"/> &lt; <paramref name="rightSide"/></c>.
    /// </summary>
    /// <param name="this">The left operand.</param>
    /// <param name="rightSide">The right operand — a literal, another expression, or a scalar subquery.</param>
    /// <returns>The comparison condition.</returns>
    public static SqlCondition operator <(
        SqlExpression @this,
        object rightSide) =>
        new LessThanCondition(@this, Resolve(rightSide));

    /// <summary>
    /// The SQL greater-than comparison: <c><paramref name="this"/> &gt; <paramref name="rightSide"/></c>.
    /// </summary>
    /// <param name="this">The left operand.</param>
    /// <param name="rightSide">The right operand — a literal, another expression, or a scalar subquery.</param>
    /// <returns>The comparison condition.</returns>
    public static SqlCondition operator >(SqlExpression @this, object rightSide) =>
        new GreaterThanCondition(@this, Resolve(rightSide));

    /// <summary>
    /// The SQL less-than-or-equal comparison: <c><paramref name="this"/> &lt;= <paramref name="rightSide"/></c>.
    /// </summary>
    /// <param name="this">The left operand.</param>
    /// <param name="rightSide">The right operand — a literal, another expression, or a scalar subquery.</param>
    /// <returns>The comparison condition.</returns>
    public static SqlCondition operator <=(
        SqlExpression @this,
        object rightSide) =>
        new LessThanOrEqualCondition(@this, Resolve(rightSide));

    /// <summary>
    /// The SQL greater-than-or-equal comparison: <c><paramref name="this"/> &gt;= <paramref name="rightSide"/></c>.
    /// </summary>
    /// <param name="this">The left operand.</param>
    /// <param name="rightSide">The right operand — a literal, another expression, or a scalar subquery.</param>
    /// <returns>The comparison condition.</returns>
    public static SqlCondition operator >=(
        SqlExpression @this,
        object rightSide) =>
        new GreaterThanOrEqualCondition(@this, Resolve(rightSide));

    /// <summary>
    /// The SQL addition operator: <c><paramref name="this"/> + <paramref name="rightSide"/></c>.
    /// </summary>
    /// <param name="this">The left operand.</param>
    /// <param name="rightSide">The right operand — a literal or another expression.</param>
    /// <returns>The addition expression.</returns>
    public static AdditionOperator operator +(
        SqlExpression @this,
        object rightSide) => new(@this, Resolve(rightSide));

    /// <summary>
    /// The SQL subtraction operator: <c><paramref name="this"/> - <paramref name="rightSide"/></c>.
    /// </summary>
    /// <param name="this">The left operand.</param>
    /// <param name="rightSide">The right operand — a literal or another expression.</param>
    /// <returns>The subtraction expression.</returns>
    public static SubtractionOperator operator -(
        SqlExpression @this,
        object rightSide) => new(@this, Resolve(rightSide));

    /// <summary>
    /// The SQL multiplication operator: <c><paramref name="this"/> * <paramref name="rightSide"/></c>.
    /// </summary>
    /// <param name="this">The left operand.</param>
    /// <param name="rightSide">The right operand — a literal or another expression.</param>
    /// <returns>The multiplication expression.</returns>
    public static MultiplicationOperator operator *(
        SqlExpression @this,
        object rightSide) => new(@this, Resolve(rightSide));

    /// <summary>
    /// The SQL division operator: <c><paramref name="this"/> / <paramref name="rightSide"/></c>.
    /// </summary>
    /// <param name="this">The left operand.</param>
    /// <param name="rightSide">The right operand — a literal or another expression.</param>
    /// <returns>The division expression.</returns>
    public static DivisionOperator operator /(
        SqlExpression @this,
        object rightSide) => new(@this, Resolve(rightSide));

    /// <summary>
    /// The SQL modulus operator: <c><paramref name="this"/> % <paramref name="rightSide"/></c>.
    /// </summary>
    /// <param name="this">The left operand.</param>
    /// <param name="rightSide">The right operand — a literal or another expression.</param>
    /// <returns>The modulus expression.</returns>
    /// <remarks>Not supported by Oracle — use <c>Sql.Mod</c> there.</remarks>
    public static ModulusOperator operator %(
        SqlExpression @this,
        object rightSide) => new(@this, Resolve(rightSide));

    /// <summary>
    /// Aliases this expression in a <c>SELECT</c> list: <c>expr AS "<paramref name="alias"/>"</c>.
    /// </summary>
    /// <param name="alias">The column alias.</param>
    /// <returns>The aliased expression.</returns>
    public ExpressionAlias As(string alias) => new(this, alias);

    /// <summary>
    /// Aliases this expression to a CTE / derived-table handle column: <c>expr
    /// <paramref name="column"/></c>, emitted bare to match how
    /// <paramref name="column"/> is referenced through its handle.
    /// </summary>
    /// <param name="column">The target CTE / derived-table column.</param>
    /// <returns>The aliased expression.</returns>
    public ExpressionAlias As(DbColumn column) => new(this, column.Name, quoteAlias: false);

    /// <summary>
    /// The <c>expr BETWEEN <paramref name="rightSide1"/> AND <paramref name="rightSide2"/></c> condition.
    /// </summary>
    /// <param name="rightSide1">The lower bound.</param>
    /// <param name="rightSide2">The upper bound.</param>
    /// <returns>The <c>BETWEEN</c> condition.</returns>
    public BetweenCondition Between(object rightSide1, object rightSide2) =>
        new(this, Resolve(rightSide1), Resolve(rightSide2));

    /// <summary>
    /// The <c>expr NOT BETWEEN <paramref name="rightSide1"/> AND <paramref name="rightSide2"/></c> condition.
    /// </summary>
    /// <param name="rightSide1">The lower bound.</param>
    /// <param name="rightSide2">The upper bound.</param>
    /// <returns>The <c>NOT BETWEEN</c> condition.</returns>
    public NotBetweenCondition NotBetween(object rightSide1, object rightSide2) =>
        new(this, Resolve(rightSide1), Resolve(rightSide2));

    /// <summary>
    /// The <c>expr IN (<paramref name="expressions"/>)</c> condition.
    /// </summary>
    /// <param name="expressions">The candidate values.</param>
    /// <returns>The <c>IN</c> condition.</returns>
    public InCondition In(params object[] expressions) =>
        new(this, Resolve(expressions));

    /// <summary>
    /// The <c>expr IN (<paramref name="values"/>)</c> condition, one bind per
    /// element of an existing collection — pass a <c>List&lt;T&gt;</c>,
    /// <c>HashSet&lt;T&gt;</c>, or any other <see cref="IReadOnlyCollection{T}"/>
    /// directly, without spreading it into the <c>params</c> overload.
    /// </summary>
    /// <param name="values">The candidate values; must be non-empty.</param>
    /// <returns>The <c>IN</c> condition.</returns>
    /// <exception cref="ArgumentException"><paramref name="values"/> is empty (an
    /// empty <c>IN</c> list is invalid SQL).</exception>
    /// <remarks>
    /// The parameter is <see cref="IReadOnlyCollection{T}"/>, not
    /// <see cref="IEnumerable{T}"/>, on purpose: a <see cref="string"/> is an
    /// <c>IEnumerable&lt;char&gt;</c> but not an <c>IReadOnlyCollection&lt;char&gt;</c>,
    /// so <c>In("abc")</c> stays a single-value predicate (via the <c>params</c>
    /// overload) instead of silently expanding into one bind per character.
    /// </remarks>
    public InCondition In<T>(IReadOnlyCollection<T> values)
    {
        CollectionGuard.ThrowIfEmpty(values, "IN requires at least one value.");
        return new(this, Resolve(values));
    }

    /// <summary>
    /// The <c>expr IN (<paramref name="values"/>)</c> condition for an array of
    /// candidate values — one bind per element.
    /// </summary>
    /// <param name="values">The candidate values; must be non-empty.</param>
    /// <returns>The <c>IN</c> condition.</returns>
    /// <exception cref="ArgumentException"><paramref name="values"/> is empty.</exception>
    /// <remarks>
    /// A typed sibling of the <see cref="IReadOnlyCollection{T}"/> overload: a
    /// reference-type array (<c>string[]</c>) is covariantly convertible to
    /// <c>object[]</c>, so without this it would be ambiguous with the
    /// <c>params object[]</c> overload. It binds one value per element either way.
    /// </remarks>
    public InCondition In<T>(T[] values)
    {
        CollectionGuard.ThrowIfEmpty(values, "IN requires at least one value.");
        return new(this, Resolve(values));
    }

    /// <summary>
    /// The <c>expr IN (<paramref name="subquery"/>)</c> condition.
    /// </summary>
    /// <param name="subquery">The subquery whose result set is tested against.</param>
    /// <returns>The <c>IN</c> condition.</returns>
    public InSubqueryCondition In(ISubquery subquery) =>
        new(this, subquery);

    /// <summary>
    /// The <c>expr NOT IN (<paramref name="expressions"/>)</c> condition.
    /// </summary>
    /// <param name="expressions">The candidate values.</param>
    /// <returns>The <c>NOT IN</c> condition.</returns>
    public NotInCondition NotIn(params object[] expressions) =>
        new(this, Resolve(expressions));

    /// <inheritdoc cref="In{T}(System.Collections.Generic.IReadOnlyCollection{T})"/>
    /// <summary>
    /// The <c>expr NOT IN (<paramref name="values"/>)</c> condition, one bind per
    /// element of an existing collection — pass a <c>List&lt;T&gt;</c>,
    /// <c>HashSet&lt;T&gt;</c>, or any other <see cref="IReadOnlyCollection{T}"/>
    /// directly, without spreading it into the <c>params</c> overload.
    /// </summary>
    /// <param name="values">The candidate values; must be non-empty.</param>
    /// <returns>The <c>NOT IN</c> condition.</returns>
    /// <exception cref="ArgumentException"><paramref name="values"/> is empty (an
    /// empty <c>NOT IN</c> list is invalid SQL).</exception>
    public NotInCondition NotIn<T>(IReadOnlyCollection<T> values)
    {
        CollectionGuard.ThrowIfEmpty(values, "NOT IN requires at least one value.");
        return new(this, Resolve(values));
    }

    /// <inheritdoc cref="In{T}(T[])"/>
    /// <summary>
    /// The <c>expr NOT IN (<paramref name="values"/>)</c> condition for an array of
    /// candidate values — one bind per element.
    /// </summary>
    /// <param name="values">The candidate values; must be non-empty.</param>
    /// <returns>The <c>NOT IN</c> condition.</returns>
    /// <exception cref="ArgumentException"><paramref name="values"/> is empty.</exception>
    public NotInCondition NotIn<T>(T[] values)
    {
        CollectionGuard.ThrowIfEmpty(values, "NOT IN requires at least one value.");
        return new(this, Resolve(values));
    }

    /// <summary>
    /// The <c>expr NOT IN (<paramref name="subquery"/>)</c> condition.
    /// </summary>
    /// <param name="subquery">The subquery whose result set is tested against.</param>
    /// <returns>The <c>NOT IN</c> condition.</returns>
    public NotInSubqueryCondition NotIn(ISubquery subquery) =>
        new(this, subquery);

    /// <summary>
    /// The <c>expr LIKE <paramref name="rightSide"/></c> pattern-match condition.
    /// </summary>
    /// <param name="rightSide">The pattern, with <c>%</c> / <c>_</c> wildcards.</param>
    /// <returns>The <c>LIKE</c> condition.</returns>
    public LikeCondition Like(object rightSide) => new(this, Resolve(rightSide));

    /// <summary>
    /// The <c>expr NOT LIKE <paramref name="rightSide"/></c> pattern-match condition.
    /// </summary>
    /// <param name="rightSide">The pattern, with <c>%</c> / <c>_</c> wildcards.</param>
    /// <returns>The <c>NOT LIKE</c> condition.</returns>
    public NotLikeCondition NotLike(object rightSide) => new(this, Resolve(rightSide));
}
