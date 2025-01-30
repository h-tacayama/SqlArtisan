namespace InlineSqlSharp;

public abstract class NumberExpr : IDataExpr
{
	public abstract void FormatSql(ref SqlBuildingBuffer buffer);

	public virtual void FormatAsSelect(ref SqlBuildingBuffer buffer) =>
		FormatSql(ref buffer);

	public override bool Equals(object? obj) => base.Equals(obj);

	public override int GetHashCode() => base.GetHashCode();

	public static ComparisonCondition operator ==(NumberExpr @this, NumberExpr rightSide)
		=> new(@this, Operators.Equal, rightSide);

	public static ComparisonCondition operator !=(NumberExpr @this, NumberExpr rightSide)
			=> new(@this, Operators.NotEqual, rightSide);
}
