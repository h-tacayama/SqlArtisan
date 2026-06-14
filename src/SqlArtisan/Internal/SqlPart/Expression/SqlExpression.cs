using System.Diagnostics;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan.Internal;

public abstract class SqlExpression : SqlPart
{
    // The DBMS this expression was authored for (e.g. via a per-DBMS namespace
    // such as SqlArtisan.Oracle), or null when portable (columns, literals, and
    // every function created through the DBMS-neutral Sql.* API). Existing
    // subclasses use the implicit base() call and so default to null, leaving
    // their behaviour unchanged.
    internal Dbms? AuthoredFor { get; }

    private protected SqlExpression(Dbms? authoredFor = null) =>
        AuthoredFor = authoredFor;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder Asc => new(this, SortDirection.Asc);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder Desc => new(this, SortDirection.Desc);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public IsNullCondition IsNull => new(this);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public IsNotNullCondition IsNotNull => new(this);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder NullsFirst => new(this, NullOrdering.NullsFirst);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder NullsLast => new(this, NullOrdering.NullsLast);

    public override bool Equals(object? obj) => base.Equals(obj);

    public override int GetHashCode() => base.GetHashCode();

    public static EqualityBasedCondition operator ==(
        SqlExpression @this,
        object rightSide) =>
        new EqualityCondition(Resolve(@this), Resolve(rightSide));

    public static EqualityBasedCondition operator !=(
        SqlExpression @this,
        object rightSide) =>
        new InequalityCondition(Resolve(@this), Resolve(rightSide));

    public static SqlCondition operator <(
        SqlExpression @this,
        object rightSide) =>
        new LessThanCondition(@this, Resolve(rightSide));

    public static SqlCondition operator >(
        SqlExpression @this,
        object rightSide) =>
        new GreaterThanCondition(@this, Resolve(rightSide));

    public static SqlCondition operator <=(
        SqlExpression @this,
        object rightSide) =>
        new LessThanOrEqualCondition(@this, Resolve(rightSide));

    public static SqlCondition operator >=(
        SqlExpression @this,
        object rightSide) =>
        new GreaterThanOrEqualCondition(@this, Resolve(rightSide));

    public static AdditionOperator operator +(
        SqlExpression @this,
        object rightSide) => new(@this, Resolve(rightSide));

    public static SubtractionOperator operator -(
        SqlExpression @this,
        object rightSide) => new(@this, Resolve(rightSide));

    public static MultiplicationOperator operator *(
        SqlExpression @this,
        object rightSide) => new(@this, Resolve(rightSide));

    public static DivisionOperator operator /(
        SqlExpression @this,
        object rightSide) => new(@this, Resolve(rightSide));

    public static ModulusOperator operator %(
        SqlExpression @this,
        object rightSide) => new(@this, Resolve(rightSide));

    public ExpressionAlias As(string alias) => new(this, alias);
    public ExpressionAlias As(DbColumn column) => new(this, column.Name);

    public BetweenCondition Between(
        object rightSide1,
        object rightSide2) => new(this, Resolve(rightSide1), Resolve(rightSide2));

    public NotBetweenCondition NotBetween(
        object rightSide1,
        object rightSide2) => new(this, Resolve(rightSide1), Resolve(rightSide2));

    public InCondition In(params object[] expressions) =>
        new(this, Resolve(expressions));

    public InSubqueryCondition In(ISubquery subquery) =>
        new(this, subquery);

    public NotInCondition NotIn(params object[] expressions) =>
        new(this, Resolve(expressions));

    public NotInSubqueryCondition NotIn(ISubquery subquery) =>
        new(this, subquery);

    public LikeCondition Like(
        object rightSide) => new(this, Resolve(rightSide));

    public NotLikeCondition NotLike(
        object rightSide) => new(this, Resolve(rightSide));
}
