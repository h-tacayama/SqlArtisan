namespace InlineSqlSharp;

public abstract class CharacterExpr : IDataExpr
{
	public abstract void FormatSql(ref SqlBuildingBuffer buffer);

	public virtual void FormatAsSelect(ref SqlBuildingBuffer buffer) =>
		FormatSql(ref buffer);

	public override bool Equals(object? obj) => base.Equals(obj);

	public override int GetHashCode() => base.GetHashCode();

	public static ComparisonCondition operator ==(
		CharacterExpr @this,
		CharacterExpr rightSide) =>
		new EqualCondition(@this, rightSide);

	public static ComparisonCondition operator !=(
		CharacterExpr @this,
		CharacterExpr rightSide) =>
		new InequalityCondition(@this, rightSide);

	public static ComparisonCondition operator ==(
		CharacterExpr @this,
		string rightSide) =>
		new EqualCondition(@this, new CharacterBoundValue(rightSide));

	public static ComparisonCondition operator !=(
		CharacterExpr @this,
		string rightSide) =>
		new InequalityCondition(@this, new CharacterBoundValue(rightSide));
}
