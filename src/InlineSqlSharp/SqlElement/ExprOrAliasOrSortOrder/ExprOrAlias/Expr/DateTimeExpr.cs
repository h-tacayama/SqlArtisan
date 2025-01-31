namespace InlineSqlSharp;

public abstract class DateTimeExpr : IDataExpr
{
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

	public static ComparisonCondition operator ==(
		DateTimeExpr @this,
		DateTime rightSide) =>
		new EqualityCondition(@this, new DateTimeBoundValue(rightSide));

	public static ComparisonCondition operator !=(
		DateTimeExpr @this,
		DateTime rightSide) =>
		new InequalityCondition(@this, new DateTimeBoundValue(rightSide));

	public static ComparisonCondition operator <(
		DateTimeExpr @this,
		DateTime rightSide) =>
		new LessThanCondition(@this, new DateTimeBoundValue(rightSide));

	public static ComparisonCondition operator >(
		DateTimeExpr @this,
		DateTime rightSide) =>
		new GreaterThanCondition(@this, new DateTimeBoundValue(rightSide));

	public static ComparisonCondition operator <=(
		DateTimeExpr @this,
		DateTime rightSide) =>
		new LessThanOrEqualCondition(@this, new DateTimeBoundValue(rightSide));

	public static ComparisonCondition operator >=(
		DateTimeExpr @this,
		DateTime rightSide) =>
		new GreaterThanOrEqualCondition(@this, new DateTimeBoundValue(rightSide));
}
