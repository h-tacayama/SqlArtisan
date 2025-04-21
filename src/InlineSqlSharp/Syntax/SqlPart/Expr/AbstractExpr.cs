using System.Diagnostics;
using static InlineSqlSharp.ExprRsolver;

namespace InlineSqlSharp;

public abstract class AbstractExpr : AbstractSqlPart
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder ASC => new(this, SortDirection.Asc);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder DESC => new(this, SortDirection.Desc);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public IsNullCondition IS_NULL => new(this);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public IsNotNullCondition IS_NOT_NULL => new(this);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder NULLS_FIRST => new(this, NullOrdering.NullsFirst);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder NULLS_LAST => new(this, NullOrdering.NullsLast);

    public override bool Equals(object? obj) => base.Equals(obj);

    public override int GetHashCode() => base.GetHashCode();

    public static AbstractEqualityCondition operator ==(
        AbstractExpr @this,
        object rightSide) =>
        new EqualityCondition(Resolve(@this), Resolve(rightSide));

    public static AbstractEqualityCondition operator !=(
        AbstractExpr @this,
        object rightSide) =>
        new InequalityCondition(Resolve(@this), Resolve(rightSide));

    public static AbstractCondition operator <(
        AbstractExpr @this,
        object rightSide) =>
        new LessThanCondition(@this, Resolve(rightSide));

    public static AbstractCondition operator >(
        AbstractExpr @this,
        object rightSide) =>
        new GreaterThanCondition(@this, Resolve(rightSide));

    public static AbstractCondition operator <=(
        AbstractExpr @this,
        object rightSide) =>
        new LessThanOrEqualCondition(@this, Resolve(rightSide));

    public static AbstractCondition operator >=(
        AbstractExpr @this,
        object rightSide) =>
        new GreaterThanOrEqualCondition(@this, Resolve(rightSide));

    public static AbstractExpr operator +(
        AbstractExpr @this,
        object rightSide) =>
        new AdditionOperator(@this, Resolve(rightSide));

    public static AbstractExpr operator -(
        AbstractExpr @this,
        object rightSide) =>
        new SubtractionOperator(@this, Resolve(rightSide));

    public static AbstractExpr operator *(
        AbstractExpr @this,
        object rightSide) =>
        new MultiplicationOperator(@this, Resolve(rightSide));

    public static AbstractExpr operator /(
        AbstractExpr @this,
        object rightSide) =>
        new DivisionOperator(@this, Resolve(rightSide));

    public static AbstractExpr operator %(
        AbstractExpr @this,
        object rightSide) =>
        new ModulusOperator(@this, Resolve(rightSide));

    public ExprAlias AS(string alias) => new(this, alias);

    public BetweenCondition BETWEEN(
        object rightSide1,
        object rightSide2) => new(this, Resolve(rightSide1), Resolve(rightSide2));

    public NotBetweenCondition NOT_BETWEEN(
        object rightSide1,
        object rightSide2) => new(this, Resolve(rightSide1), Resolve(rightSide2));

    public InCondition IN(params object[] expressions) =>
        new(this, Resolve(expressions));

    public InSubqueryCondition IN(ISubquery subquery) =>
        new(this, subquery);

    public NotInCondition NOT_IN(params object[] expressions) =>
        new(this, Resolve(expressions));

    public NotInSubqueryCondition NOT_IN(ISubquery subquery) =>
        new(this, subquery);

    public LikeCondition LIKE(
        object rightSide) => new(this, Resolve(rightSide));

    public NotLikeCondition NOT_LIKE(
        object rightSide) => new(this, Resolve(rightSide));
}
