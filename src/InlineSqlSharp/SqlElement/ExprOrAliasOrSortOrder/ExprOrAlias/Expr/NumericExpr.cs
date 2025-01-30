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
		NumericExpr rightSide)　=>
		new(@this, Operators.Equal, rightSide);

	public static ComparisonCondition operator !=(
		NumericExpr @this,
		NumericExpr rightSide)　=>
		new(@this, Operators.NotEqual, rightSide);
}
