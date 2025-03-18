using System.Diagnostics;
using System.Numerics;

namespace InlineSqlSharp;

public abstract class NumericExpr : IAliasable, IDataExpr, ISortable
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
		NumericExpr @this,
		NumericExpr rightSide) =>
		new EqualityCondition(@this, rightSide);

	public static IEqualityCondition operator !=(
		NumericExpr @this,
		NumericExpr rightSide) =>
		new InequalityCondition(@this, rightSide);

	public static IComparisonCondition operator <(
		NumericExpr @this,
		NumericExpr rightSide) =>
		new LessThanCondition(@this, rightSide);

	public static IComparisonCondition operator >(
		NumericExpr @this,
		NumericExpr rightSide) =>
		new GreaterThanCondition(@this, rightSide);

	public static IComparisonCondition operator <=(
		NumericExpr @this,
		NumericExpr rightSide) =>
		new LessThanOrEqualCondition(@this, rightSide);

	public static IComparisonCondition operator >=(
		NumericExpr @this,
		NumericExpr rightSide) =>
		new GreaterThanOrEqualCondition(@this, rightSide);


	public static NumericExpr operator +(
		NumericExpr @this,
		NumericExpr rightSide) =>
		new AdditionOperator(@this, rightSide);

	public static NumericExpr operator -(
		NumericExpr @this,
		NumericExpr rightSide) =>
		new SubtractionOperator(@this, rightSide);

	public static NumericExpr operator *(
		NumericExpr @this,
		NumericExpr rightSide) =>
		new MultiplicationOperator(@this, rightSide);

	public static NumericExpr operator /(
		NumericExpr @this,
		NumericExpr rightSide) =>
		new DivisionOperator(@this, rightSide);

	public static NumericExpr operator %(
		NumericExpr @this,
		NumericExpr rightSide) =>
		new ModulusOperator(@this, rightSide);

	public ExprAlias AS(string alias) => new(this, alias);

	public BetweenCondition BETWEEN(
		NumericExpr rightSide1,
		NumericExpr rightSide2) => new(this, rightSide1, rightSide2);

	public NotBetweenCondition NOT_BETWEEN(
		NumericExpr rightSide1,
		NumericExpr rightSide2) => new(this, rightSide1, rightSide2);

	public InCondition IN(params NumericExpr[] expressions) =>
		new(this, expressions);

	public NotInCondition NOT_IN(params NumericExpr[] expressions) =>
		new(this, expressions);

	public InSubqueryCondition IN(ISubquery subquery) =>
		new(this, subquery);

	public NotInSubqueryCondition NOT_IN(ISubquery subquery) =>
		new(this, subquery);
}
