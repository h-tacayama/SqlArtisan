using System.Numerics;

namespace InlineSqlSharp;

public abstract class NumericExpr : IDataExpr
{
	public abstract void FormatSql(ref SqlBuildingBuffer buffer);

	public virtual void FormatAsSelect(ref SqlBuildingBuffer buffer) =>
		FormatSql(ref buffer);

	public override bool Equals(object? obj) => base.Equals(obj);

	public override int GetHashCode() => base.GetHashCode();

	public static ComparisonCondition operator ==(
		NumericExpr @this,
		NumericExpr rightSide) =>
		new EqualCondition(@this, rightSide);

	public static ComparisonCondition operator !=(
		NumericExpr @this,
		NumericExpr rightSide) =>
		new InequalityCondition(@this, rightSide);

	public static ComparisonCondition operator ==(
		NumericExpr @this,
		int rightSide) => NewEqual(@this, rightSide);

	public static ComparisonCondition operator !=(
		NumericExpr @this,
		int rightSide) => NewNotEqual(@this, rightSide);

	public static ComparisonCondition operator ==(
		NumericExpr @this,
		long rightSide) => NewEqual(@this, rightSide);

	public static ComparisonCondition operator !=(
		NumericExpr @this,
		long rightSide) => NewNotEqual(@this, rightSide);

	public static ComparisonCondition operator ==(
		NumericExpr @this,
		float rightSide) => NewEqual(@this, rightSide);

	public static ComparisonCondition operator !=(
		NumericExpr @this,
		float rightSide) => NewNotEqual(@this, rightSide);

	public static ComparisonCondition operator ==(
		NumericExpr @this,
		double rightSide) => NewEqual(@this, rightSide);

	public static ComparisonCondition operator !=(
		NumericExpr @this,
		double rightSide) => NewNotEqual(@this, rightSide);

	public static ComparisonCondition operator ==(
		NumericExpr @this,
		decimal rightSide) => NewEqual(@this, rightSide);

	public static ComparisonCondition operator !=(
		NumericExpr @this,
		decimal rightSide) => NewNotEqual(@this, rightSide);

	private static ComparisonCondition NewEqual<TValue>(
		NumericExpr @this,
		TValue rightSide)
		where TValue : INumber<TValue> =>
		new EqualCondition(@this, new NumericBoundValue<TValue>(rightSide));

	private static ComparisonCondition NewNotEqual<TValue>(
		NumericExpr @this,
		TValue rightSide)
		where TValue : INumber<TValue> =>
		new InequalityCondition(@this, new NumericBoundValue<TValue>(rightSide));
}
