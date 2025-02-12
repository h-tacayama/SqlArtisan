using System.Diagnostics;

namespace InlineSqlSharp;

public abstract class CharacterExpr : IAliasable, IDataExpr, ISortable
{
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public SortOrder ASC => new(this, SortDirection.Asc);

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public SortOrder DESC => new(this, SortDirection.Desc);

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public IsNullCondition IS_NULL => new(this);

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public IsNotNullCondition IS_NOT_NULL => new(this);

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public SortOrder NULLS_FIRST => new(this, NullOrdering.NullsFirst);

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public SortOrder NULLS_LAST => new(this, NullOrdering.NullsLast);

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

	public ExprAlias AS(string alias) => new(this, alias);

	public BetweenCondition BETWEEN(
		CharacterExpr rightSide1,
		CharacterExpr rightSide2) => new(this, rightSide1, rightSide2);

	public NotBetweenCondition NOT_BETWEEN(
		CharacterExpr rightSide1,
		CharacterExpr rightSide2) => new(this, rightSide1, rightSide2);

	public InCondition IN(params CharacterExpr[] expressions) =>
		new(this, expressions);

	public NotInCondition NOT_IN(params CharacterExpr[] expressions) =>
		new(this, expressions);

	public InSubqueryCondition IN(ISubquery subquery) =>
		new(this, subquery);

	public NotInSubqueryCondition NOT_IN(ISubquery subquery) =>
		new(this, subquery);

	public LikeCondition LIKE(
		CharacterExpr rightSide) => new(this, rightSide);

	public NotLikeCondition NOT_LIKE(
		CharacterExpr rightSide) => new(this, rightSide);
}
