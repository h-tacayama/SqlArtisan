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

	public virtual void FormatAsSelect(SqlBuildingBuffer buffer) =>
		FormatSql(buffer);

	public override bool Equals(object? obj) => base.Equals(obj);

	public override int GetHashCode() => base.GetHashCode();

	public static IEqualityCondition operator ==(
		DateTimeExpr @this,
		DateTimeExpr rightSide) =>
		new EqualityCondition(@this, rightSide);

	public static IEqualityCondition operator ==(
		DateTimeExpr @this,
		DateTime rightSide) =>
		new EqualityCondition(@this, new DateTimeBindValue(rightSide));

	public static IEqualityCondition operator !=(
		DateTimeExpr @this,
		DateTimeExpr rightSide) =>
		new InequalityCondition(@this, rightSide);
	
	public static IEqualityCondition operator !=(
		DateTimeExpr @this,
		DateTime rightSide) =>
		new InequalityCondition(@this, new DateTimeBindValue(rightSide));

	public static IComparisonCondition operator <(
		DateTimeExpr @this,
		DateTimeExpr rightSide) =>
		new LessThanCondition(@this, rightSide);

	public static IComparisonCondition operator <(
		DateTimeExpr @this,
		DateTime rightSide) =>
		new LessThanCondition(@this, new DateTimeBindValue(rightSide));

	public static IComparisonCondition operator >(
		DateTimeExpr @this,
		DateTimeExpr rightSide) =>
		new GreaterThanCondition(@this, rightSide);

	public static IComparisonCondition operator >(
		DateTimeExpr @this,
		DateTime rightSide) =>
		new GreaterThanCondition(@this, new DateTimeBindValue(rightSide));

	public static IComparisonCondition operator <=(
		DateTimeExpr @this,
		DateTimeExpr rightSide) =>
		new LessThanOrEqualCondition(@this, rightSide);

	public static IComparisonCondition operator <=(
		DateTimeExpr @this,
		DateTime rightSide) =>
		new LessThanOrEqualCondition(@this, new DateTimeBindValue(rightSide));

	public static IComparisonCondition operator >=(
		DateTimeExpr @this,
		DateTimeExpr rightSide) =>
		new GreaterThanOrEqualCondition(@this, rightSide);

	public static IComparisonCondition operator >=(
		DateTimeExpr @this,
		DateTime rightSide) =>
		new GreaterThanOrEqualCondition(@this, new DateTimeBindValue(rightSide));

	public static DateTimeExpr operator +(
		DateTimeExpr @this,
		NumericExpr rightSide) =>
		new DateOffsetAdditionOperator(@this, rightSide);

	public static DateTimeExpr operator +(
		DateTimeExpr @this,
		int rightSide) =>
		new DateOffsetAdditionOperator(@this, new NumericBindValue<int>(rightSide));

	public static DateTimeExpr operator -(
		DateTimeExpr @this,
		NumericExpr rightSide) =>
		new DateOffsetSubtractionOperator(@this, rightSide);

	public static DateTimeExpr operator -(
		DateTimeExpr @this,
		int rightSide) =>
		new DateOffsetSubtractionOperator(@this, new NumericBindValue<int>(rightSide));

	public static NumericExpr operator -(
		DateTimeExpr @this,
		DateTimeExpr rightSide) =>
		new DateDiffSubtractionOperator(@this, rightSide);

	public ExprAlias AS(string alias) => new(this, alias);

	public BetweenCondition BETWEEN(
		DateTimeExpr rightSide1,
		DateTimeExpr rightSide2) => new(this, rightSide1, rightSide2);

	public BetweenCondition BETWEEN(
		DateTimeExpr rightSide1,
		DateTime rightSide2) => new(this, rightSide1, new DateTimeBindValue(rightSide2));

	public BetweenCondition BETWEEN(
		DateTime rightSide1,
		DateTimeExpr rightSide2) => new(this, new DateTimeBindValue(rightSide1), rightSide2);

	public BetweenCondition BETWEEN(
		DateTime rightSide1,
		DateTime rightSide2) => new(
			this,
			new DateTimeBindValue(rightSide1),
			new DateTimeBindValue(rightSide2));

	public NotBetweenCondition NOT_BETWEEN(
		DateTimeExpr rightSide1,
		DateTimeExpr rightSide2) => new(this, rightSide1, rightSide2);

	public NotBetweenCondition NOT_BETWEEN(
		DateTimeExpr rightSide1,
		DateTime rightSide2) => new(this, rightSide1, new DateTimeBindValue(rightSide2));

	public NotBetweenCondition NOT_BETWEEN(
		DateTime rightSide1,
		DateTimeExpr rightSide2) => new(this, new DateTimeBindValue(rightSide1), rightSide2);

	public NotBetweenCondition NOT_BETWEEN(
		DateTime rightSide1,
		DateTime rightSide2) => new(
			this,
			new DateTimeBindValue(rightSide1),
			new DateTimeBindValue(rightSide2));

	public InCondition IN(params DateTimeExpr[] expressions) =>
		new(this, expressions);

	public InCondition IN(params DateTime[] values) =>
		new(this, BindValueArrayFactory.Create(values));

	public InSubqueryCondition IN(ISubquery subquery) =>
		new(this, subquery);

	public NotInCondition NOT_IN(params DateTimeExpr[] expressions) =>
		new(this, expressions);

	public NotInCondition NOT_IN(params DateTime[] values) =>
		new(this, BindValueArrayFactory.Create(values));

	public NotInSubqueryCondition NOT_IN(ISubquery subquery) =>
		new(this, subquery);
}
