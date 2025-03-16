using System.Diagnostics;

namespace InlineSqlSharp;

public abstract class DateTimeExpr : IAliasable, IDataExpr, ISortable
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

	public abstract void FormatSql(SqlBuildingBuffer buffer);

	public virtual void FormatAsSelect(ref SqlBuildingBuffer buffer) =>
		FormatSql(buffer);

	public override bool Equals(object? obj) => base.Equals(obj);

	public override int GetHashCode() => base.GetHashCode();

	public static IEqualityCondition operator ==(
		DateTimeExpr @this,
		DateTimeExpr rightSide) =>
		new EqualityCondition(@this, rightSide);

	public static IEqualityCondition operator !=(
		DateTimeExpr @this,
		DateTimeExpr rightSide) =>
		new InequalityCondition(@this, rightSide);

	public static IComparisonCondition operator <(
		DateTimeExpr @this,
		DateTimeExpr rightSide) =>
		new LessThanCondition(@this, rightSide);

	public static IComparisonCondition operator >(
		DateTimeExpr @this,
		DateTimeExpr rightSide) =>
		new GreaterThanCondition(@this, rightSide);

	public static IComparisonCondition operator <=(
		DateTimeExpr @this,
		DateTimeExpr rightSide) =>
		new LessThanOrEqualCondition(@this, rightSide);

	public static IComparisonCondition operator >=(
		DateTimeExpr @this,
		DateTimeExpr rightSide) =>
		new GreaterThanOrEqualCondition(@this, rightSide);

	public static DateTimeExpr operator +(
		DateTimeExpr @this,
		NumericExpr rightSide) =>
		new DateOffsetAdditionOperator(@this, rightSide);

	public static DateTimeExpr operator -(
		DateTimeExpr @this,
		NumericExpr rightSide) =>
		new DateOffsetSubtractionOperator(@this, rightSide);

	public static NumericExpr operator -(
		DateTimeExpr @this,
		DateTimeExpr rightSide) =>
		new DateDiffSubtractionOperator(@this, rightSide);

	public ExprAlias AS(string alias) => new(this, alias);

	public BetweenCondition BETWEEN(
		DateTimeExpr rightSide1,
		DateTimeExpr rightSide2) => new(this, rightSide1, rightSide2);

	public NotBetweenCondition NOT_BETWEEN(
		DateTimeExpr rightSide1,
		DateTimeExpr rightSide2) => new(this, rightSide1, rightSide2);

	public InCondition IN(params DateTimeExpr[] expressions) =>
		new(this, expressions);

	public NotInCondition NOT_IN(params DateTimeExpr[] expressions) =>
		new(this, expressions);

	public InSubqueryCondition IN(ISubquery subquery) =>
		new(this, subquery);

	public NotInSubqueryCondition NOT_IN(ISubquery subquery) =>
		new(this, subquery);
}
