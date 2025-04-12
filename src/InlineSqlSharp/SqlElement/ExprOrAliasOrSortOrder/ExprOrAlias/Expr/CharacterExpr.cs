using System.Diagnostics;

namespace InlineSqlSharp;

public abstract class CharacterExpr : IAliasable, IDataTypeExpr, ISortable
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
        CharacterExpr @this,
        CharacterExpr rightSide) =>
        new EqualityCondition(@this, rightSide);

    public static IEqualityCondition operator ==(
        CharacterExpr @this,
        char rightSide) =>
        new EqualityCondition(@this, new CharacterBindValue(rightSide));

    public static IEqualityCondition operator ==(
        CharacterExpr @this,
        string rightSide) =>
        new EqualityCondition(@this, new CharacterBindValue(rightSide));

    public static IEqualityCondition operator !=(
        CharacterExpr @this,
        CharacterExpr rightSide) =>
        new InequalityCondition(@this, rightSide);

    public static IEqualityCondition operator !=(
        CharacterExpr @this,
        char rightSide) =>
        new InequalityCondition(@this, new CharacterBindValue(rightSide));

    public static IEqualityCondition operator !=(
        CharacterExpr @this,
        string rightSide) =>
        new InequalityCondition(@this, new CharacterBindValue(rightSide));

    public static IComparisonCondition operator <(
        CharacterExpr @this,
        CharacterExpr rightSide) =>
        new LessThanCondition(@this, rightSide);

    public static IComparisonCondition operator <(
        CharacterExpr @this,
        char rightSide) =>
        new LessThanCondition(@this, new CharacterBindValue(rightSide));

    public static IComparisonCondition operator <(
        CharacterExpr @this,
        string rightSide) =>
        new LessThanCondition(@this, new CharacterBindValue(rightSide));

    public static IComparisonCondition operator >(
        CharacterExpr @this,
        CharacterExpr rightSide) =>
        new GreaterThanCondition(@this, rightSide);

    public static IComparisonCondition operator >(
        CharacterExpr @this,
        char rightSide) =>
        new GreaterThanCondition(@this, new CharacterBindValue(rightSide));

    public static IComparisonCondition operator >(
        CharacterExpr @this,
        string rightSide) =>
        new GreaterThanCondition(@this, new CharacterBindValue(rightSide));

    public static IComparisonCondition operator <=(
        CharacterExpr @this,
        CharacterExpr rightSide) =>
        new LessThanOrEqualCondition(@this, rightSide);

    public static IComparisonCondition operator <=(
        CharacterExpr @this,
        char rightSide) =>
        new LessThanOrEqualCondition(@this, new CharacterBindValue(rightSide));

    public static IComparisonCondition operator <=(
        CharacterExpr @this,
        string rightSide) =>
        new LessThanOrEqualCondition(@this, new CharacterBindValue(rightSide));

    public static IComparisonCondition operator >=(
        CharacterExpr @this,
        CharacterExpr rightSide) =>
        new GreaterThanOrEqualCondition(@this, rightSide);

    public static IComparisonCondition operator >=(
        CharacterExpr @this,
        char rightSide) =>
        new GreaterThanOrEqualCondition(@this, new CharacterBindValue(rightSide));

    public static IComparisonCondition operator >=(
        CharacterExpr @this,
        string rightSide) =>
        new GreaterThanOrEqualCondition(@this, new CharacterBindValue(rightSide));

    public ExprAlias AS(string alias) => new(this, alias);

    public BetweenCondition BETWEEN(
        CharacterExpr rightSide1,
        CharacterExpr rightSide2) => new(this, rightSide1, rightSide2);

    public BetweenCondition BETWEEN(
        CharacterExpr rightSide1,
        char rightSide2) => new(this, rightSide1, new CharacterBindValue(rightSide2));

    public BetweenCondition BETWEEN(
        char rightSide1,
        CharacterExpr rightSide2) => new(this, new CharacterBindValue(rightSide1), rightSide2);

    public BetweenCondition BETWEEN(
        char rightSide1,
        char rightSide2) => new(
            this,
            new CharacterBindValue(rightSide1),
            new CharacterBindValue(rightSide2));

    public BetweenCondition BETWEEN(
        CharacterExpr rightSide1,
        string rightSide2) => new(this, rightSide1, new CharacterBindValue(rightSide2));

    public BetweenCondition BETWEEN(
        string rightSide1,
        CharacterExpr rightSide2) => new(this, new CharacterBindValue(rightSide1), rightSide2);

    public BetweenCondition BETWEEN(
        string rightSide1,
        string rightSide2) => new(
            this,
            new CharacterBindValue(rightSide1),
            new CharacterBindValue(rightSide2));

    public NotBetweenCondition NOT_BETWEEN(
        CharacterExpr rightSide1,
        CharacterExpr rightSide2) => new(this, rightSide1, rightSide2);

    public NotBetweenCondition NOT_BETWEEN(
        CharacterExpr rightSide1,
        char rightSide2) => new(this, rightSide1, new CharacterBindValue(rightSide2));

    public NotBetweenCondition NOT_BETWEEN(
        char rightSide1,
        CharacterExpr rightSide2) => new(this, new CharacterBindValue(rightSide1), rightSide2);

    public NotBetweenCondition NOT_BETWEEN(
        char rightSide1,
        char rightSide2) => new(
            this,
            new CharacterBindValue(rightSide1),
            new CharacterBindValue(rightSide2));

    public NotBetweenCondition NOT_BETWEEN(
        CharacterExpr rightSide1,
        string rightSide2) => new(this, rightSide1, new CharacterBindValue(rightSide2));

    public NotBetweenCondition NOT_BETWEEN(
        string rightSide1,
        CharacterExpr rightSide2) => new(this, new CharacterBindValue(rightSide1), rightSide2);

    public NotBetweenCondition NOT_BETWEEN(
        string rightSide1,
        string rightSide2) => new(
            this,
            new CharacterBindValue(rightSide1),
            new CharacterBindValue(rightSide2));

    public InCondition IN(params CharacterExpr[] expressions) =>
        new(this, expressions);

    public InCondition IN(params char[] values) =>
        new(this, BindValueArrayFactory.FromChar(values));

    public InCondition IN(params string[] values) =>
        new(this, BindValueArrayFactory.FromString(values));

    public InSubqueryCondition IN(ISubquery subquery) =>
        new(this, subquery);

    public NotInCondition NOT_IN(params CharacterExpr[] expressions) =>
        new(this, expressions);

    public NotInCondition NOT_IN(params char[] values) =>
        new(this, BindValueArrayFactory.FromChar(values));

    public NotInCondition NOT_IN(params string[] values) =>
        new(this, BindValueArrayFactory.FromString(values));

    public NotInSubqueryCondition NOT_IN(ISubquery subquery) =>
        new(this, subquery);

    public LikeCondition LIKE(
        CharacterExpr rightSide) => new(this, rightSide);

    public LikeCondition LIKE(
        string rightSide) => new(this, new CharacterBindValue(rightSide));

    public NotLikeCondition NOT_LIKE(
        CharacterExpr rightSide) => new(this, rightSide);

    public NotLikeCondition NOT_LIKE(
        string rightSide) => new(this, new CharacterBindValue(rightSide));
}
