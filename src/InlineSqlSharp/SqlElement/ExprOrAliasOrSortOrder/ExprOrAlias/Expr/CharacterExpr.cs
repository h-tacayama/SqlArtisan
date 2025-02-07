using System.Diagnostics;

namespace InlineSqlSharp;

public abstract class CharacterExpr : IDataExpr
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
		CharacterExpr @this,
		CharacterExpr rightSide) =>
		new EqualityCondition(@this, rightSide);

	public static ComparisonCondition operator !=(
		CharacterExpr @this,
		CharacterExpr rightSide) =>
		new InequalityCondition(@this, rightSide);

	public static ComparisonCondition operator <(
		CharacterExpr @this,
		CharacterExpr rightSide) =>
		new LessThanCondition(@this, rightSide);

	public static ComparisonCondition operator >(
		CharacterExpr @this,
		CharacterExpr rightSide) =>
		new GreaterThanCondition(@this, rightSide);

	public static ComparisonCondition operator <=(
		CharacterExpr @this,
		CharacterExpr rightSide) =>
		new LessThanOrEqualCondition(@this, rightSide);

	public static ComparisonCondition operator >=(
		CharacterExpr @this,
		CharacterExpr rightSide) =>
		new GreaterThanOrEqualCondition(@this, rightSide);

	public BetweenCondition BETWEEN(
		CharacterExpr rightSide1,
		CharacterExpr rightSide2) => new(this, rightSide1, rightSide2);

	public NotBetweenCondition NOT_BETWEEN(
		CharacterExpr rightSide1,
		CharacterExpr rightSide2) => new(this, rightSide1, rightSide2);

	public InCondition IN(
		CharacterExpr primaryExpr,
		params CharacterExpr[] secondaryExprs) =>
		new(this, primaryExpr, secondaryExprs);

	public NotInCondition NOT_IN(
		CharacterExpr primaryExpr,
		params CharacterExpr[] secondaryExprs) =>
		new(this, primaryExpr, secondaryExprs);

	public InSubqueryCondition IN(ISubqueryBuilder subquery) =>
		new(this, subquery);

	public NotInSubqueryCondition NOT_IN(ISubqueryBuilder subquery) =>
		new(this, subquery);

	public LikeCondition LIKE(
		CharacterExpr rightSide) => new(this, rightSide);

	public NotLikeCondition NOT_LIKE(
		CharacterExpr rightSide) => new(this, rightSide);
}
