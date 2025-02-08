using System.Diagnostics;
using System.Numerics;

namespace InlineSqlSharp;

public abstract class NumericExpr : IDataExpr
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
		NumericExpr @this,
		NumericExpr rightSide) =>
		new EqualityCondition(@this, rightSide);

	public static ComparisonCondition operator !=(
		NumericExpr @this,
		NumericExpr rightSide) =>
		new InequalityCondition(@this, rightSide);

	public static ComparisonCondition operator <(
		NumericExpr @this,
		NumericExpr rightSide) =>
		new LessThanCondition(@this, rightSide);

	public static ComparisonCondition operator >(
		NumericExpr @this,
		NumericExpr rightSide) =>
		new GreaterThanCondition(@this, rightSide);

	public static ComparisonCondition operator <=(
		NumericExpr @this,
		NumericExpr rightSide) =>
		new LessThanOrEqualCondition(@this, rightSide);

	public static ComparisonCondition operator >=(
		NumericExpr @this,
		NumericExpr rightSide) =>
		new GreaterThanOrEqualCondition(@this, rightSide);

	public BetweenCondition BETWEEN(
		NumericExpr rightSide1,
		NumericExpr rightSide2) => new(this, rightSide1, rightSide2);

	public NotBetweenCondition NOT_BETWEEN(
		NumericExpr rightSide1,
		NumericExpr rightSide2) => new(this, rightSide1, rightSide2);

	public InCondition IN(params NumericExpr[] expressions) =>
		new(this, expressions);

	public NotInCondition NOT_IN(params NumericExpr[] expressions) =>
		new(this, expressions);

	public InSubqueryCondition IN(ISubqueryBuilder subquery) =>
		new(this, subquery);

	public NotInSubqueryCondition NOT_IN(ISubqueryBuilder subquery) =>
		new(this, subquery);
}
