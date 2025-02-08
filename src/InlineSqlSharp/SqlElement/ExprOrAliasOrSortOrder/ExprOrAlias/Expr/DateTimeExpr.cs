using System.Diagnostics;

namespace InlineSqlSharp;

public abstract class DateTimeExpr : IDataExpr
{
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public IsNullCondition IS_NULL => new(this);

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public IsNotNullCondition IS_NOT_NULL => new(this);

	public abstract void FormatSql(ref SqlBuildingBuffer buffer);

	public virtual void FormatAsSelect(ref SqlBuildingBuffer buffer) =>
		FormatSql(ref buffer);

	public override bool Equals(object? obj) => base.Equals(obj);

	public override int GetHashCode() => base.GetHashCode();

	public static ComparisonCondition operator ==(
		DateTimeExpr @this,
		DateTimeExpr rightSide) =>
		new EqualityCondition(@this, rightSide);

	public static ComparisonCondition operator !=(
		DateTimeExpr @this,
		DateTimeExpr rightSide) =>
		new InequalityCondition(@this, rightSide);

	public static ComparisonCondition operator <(
		DateTimeExpr @this,
		DateTimeExpr rightSide) =>
		new LessThanCondition(@this, rightSide);

	public static ComparisonCondition operator >(
		DateTimeExpr @this,
		DateTimeExpr rightSide) =>
		new GreaterThanCondition(@this, rightSide);

	public static ComparisonCondition operator <=(
		DateTimeExpr @this,
		DateTimeExpr rightSide) =>
		new LessThanOrEqualCondition(@this, rightSide);

	public static ComparisonCondition operator >=(
		DateTimeExpr @this,
		DateTimeExpr rightSide) =>
		new GreaterThanOrEqualCondition(@this, rightSide);

	public BetweenCondition BETWEEN(
		DateTimeExpr rightSide1,
		DateTimeExpr rightSide2) => new(this, rightSide1, rightSide2);

	public NotBetweenCondition NOT_BETWEEN(
		DateTimeExpr rightSide1,
		DateTimeExpr rightSide2) => new(this, rightSide1, rightSide2);

	public InCondition IN(params DateTimeExpr[] expressions) =>
		new(this, expressions);

	public NotInCondition NOT_IN(params DateTimeExpr[] expressions) =>
		new(this, expressions);

	public InSubqueryCondition IN(ISubquery subquery) =>
		new(this, subquery);

	public NotInSubqueryCondition NOT_IN(ISubquery subquery) =>
		new(this, subquery);
}
