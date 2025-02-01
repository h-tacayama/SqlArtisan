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

	public static ComparisonCondition operator ==(
		NumericExpr @this,
		int rightSide) => NewEquality(@this, rightSide);

	public static ComparisonCondition operator !=(
		NumericExpr @this,
		int rightSide) => NewInequality(@this, rightSide);

	public static ComparisonCondition operator <(
		NumericExpr @this,
		int rightSide) => NewLessThan(@this, rightSide);

	public static ComparisonCondition operator >(
		NumericExpr @this,
		int rightSide) => NewGreaterThan(@this, rightSide);

	public static ComparisonCondition operator <=(
		NumericExpr @this,
		int rightSide) => NewLessThanOrEqual(@this, rightSide);

	public static ComparisonCondition operator >=(
		NumericExpr @this,
		int rightSide) => NewGreaterThanOrEqual(@this, rightSide);

	public static ComparisonCondition operator ==(
		NumericExpr @this,
		long rightSide) => NewEquality(@this, rightSide);

	public static ComparisonCondition operator !=(
		NumericExpr @this,
		long rightSide) => NewInequality(@this, rightSide);

	public static ComparisonCondition operator <(
		NumericExpr @this,
		long rightSide) => NewLessThan(@this, rightSide);

	public static ComparisonCondition operator >(
		NumericExpr @this,
		long rightSide) => NewGreaterThan(@this, rightSide);

	public static ComparisonCondition operator <=(
		NumericExpr @this,
		long rightSide) => NewLessThanOrEqual(@this, rightSide);

	public static ComparisonCondition operator >=(
		NumericExpr @this,
		long rightSide) => NewGreaterThanOrEqual(@this, rightSide);

	public static ComparisonCondition operator ==(
		NumericExpr @this,
		float rightSide) => NewEquality(@this, rightSide);

	public static ComparisonCondition operator !=(
		NumericExpr @this,
		float rightSide) => NewInequality(@this, rightSide);

	public static ComparisonCondition operator <(
		NumericExpr @this,
		float rightSide) => NewLessThan(@this, rightSide);

	public static ComparisonCondition operator >(
		NumericExpr @this,
		float rightSide) => NewGreaterThan(@this, rightSide);

	public static ComparisonCondition operator <=(
		NumericExpr @this,
		float rightSide) => NewLessThanOrEqual(@this, rightSide);

	public static ComparisonCondition operator >=(
		NumericExpr @this,
		float rightSide) => NewGreaterThanOrEqual(@this, rightSide);

	public static ComparisonCondition operator ==(
		NumericExpr @this,
		double rightSide) => NewEquality(@this, rightSide);

	public static ComparisonCondition operator !=(
		NumericExpr @this,
		double rightSide) => NewInequality(@this, rightSide);

	public static ComparisonCondition operator <(
		NumericExpr @this,
		double rightSide) => NewLessThan(@this, rightSide);

	public static ComparisonCondition operator >(
		NumericExpr @this,
		double rightSide) => NewGreaterThan(@this, rightSide);

	public static ComparisonCondition operator <=(
		NumericExpr @this,
		double rightSide) => NewLessThanOrEqual(@this, rightSide);

	public static ComparisonCondition operator >=(
		NumericExpr @this,
		double rightSide) => NewGreaterThanOrEqual(@this, rightSide);

	public static ComparisonCondition operator ==(
		NumericExpr @this,
		decimal rightSide) => NewEquality(@this, rightSide);

	public static ComparisonCondition operator !=(
		NumericExpr @this,
		decimal rightSide) => NewInequality(@this, rightSide);

	public static ComparisonCondition operator <(
		NumericExpr @this,
		decimal rightSide) => NewLessThan(@this, rightSide);

	public static ComparisonCondition operator >(
		NumericExpr @this,
		decimal rightSide) => NewGreaterThan(@this, rightSide);

	public static ComparisonCondition operator <=(
		NumericExpr @this,
		decimal rightSide) => NewLessThanOrEqual(@this, rightSide);

	public static ComparisonCondition operator >=(
		NumericExpr @this,
		decimal rightSide) => NewGreaterThanOrEqual(@this, rightSide);

	private static ComparisonCondition NewEquality<TValue>(
		NumericExpr @this,
		TValue rightSide)
		where TValue : INumber<TValue> =>
		new EqualityCondition(@this, new NumericBoundValue<TValue>(rightSide));

	private static ComparisonCondition NewInequality<TValue>(
		NumericExpr @this,
		TValue rightSide)
		where TValue : INumber<TValue> =>
		new InequalityCondition(@this, new NumericBoundValue<TValue>(rightSide));

	private static ComparisonCondition NewLessThan<TValue>(
		NumericExpr @this,
		TValue rightSide)
		where TValue : INumber<TValue> =>
		new LessThanCondition(@this, new NumericBoundValue<TValue>(rightSide));

	private static ComparisonCondition NewGreaterThan<TValue>(
		NumericExpr @this,
		TValue rightSide)
		where TValue : INumber<TValue> =>
		new GreaterThanCondition(@this, new NumericBoundValue<TValue>(rightSide));

	private static ComparisonCondition NewLessThanOrEqual<TValue>(
		NumericExpr @this,
		TValue rightSide)
		where TValue : INumber<TValue> =>
		new LessThanOrEqualCondition(@this, new NumericBoundValue<TValue>(rightSide));

	private static ComparisonCondition NewGreaterThanOrEqual<TValue>(
		NumericExpr @this,
		TValue rightSide)
		where TValue : INumber<TValue> =>
		new GreaterThanOrEqualCondition(@this, new NumericBoundValue<TValue>(rightSide));
}
