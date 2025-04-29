using System.Diagnostics;
using static InlineSqlSharp.ExprResolver;

namespace InlineSqlSharp;

public abstract class AbstractExpr : AbstractSqlPart
{
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

    public ExprAlias As(string alias) => new(this, alias);

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
