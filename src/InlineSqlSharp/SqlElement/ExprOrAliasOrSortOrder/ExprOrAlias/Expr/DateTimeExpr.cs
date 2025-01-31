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
		new EqualCondition(@this, rightSide);

	public static ComparisonCondition operator !=(
		DateTimeExpr @this,
		DateTimeExpr rightSide) =>
		new InequalityCondition(@this, rightSide);

	public static ComparisonCondition operator ==(
		DateTimeExpr @this,
		DateTime rightSide) =>
		new EqualCondition(@this, new DateTimeBoundValue(rightSide));

	public static ComparisonCondition operator !=(
		DateTimeExpr @this,
		DateTime rightSide) =>
		new InequalityCondition(@this, new DateTimeBoundValue(rightSide));
}
