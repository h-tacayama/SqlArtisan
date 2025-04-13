using System.Diagnostics;

namespace InlineSqlSharp;

public abstract class NumericExpr : IAliasable, IExpr, ISortable
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

    public static IEqualityCondition operator ==(
        NumericExpr @this,
        sbyte rightSide) =>
        new EqualityCondition(@this, new NumericBindValue<sbyte>(rightSide));

    public static IEqualityCondition operator ==(
        NumericExpr @this,
        byte rightSide) =>
        new EqualityCondition(@this, new NumericBindValue<byte>(rightSide));

    public static IEqualityCondition operator ==(
        NumericExpr @this,
        short rightSide) =>
        new EqualityCondition(@this, new NumericBindValue<short>(rightSide));

    public static IEqualityCondition operator ==(
        NumericExpr @this,
        ushort rightSide) =>
        new EqualityCondition(@this, new NumericBindValue<ushort>(rightSide));

    public static IEqualityCondition operator ==(
        NumericExpr @this,
        int rightSide) =>
        new EqualityCondition(@this, new NumericBindValue<int>(rightSide));

    public static IEqualityCondition operator ==(
        NumericExpr @this,
        uint rightSide) =>
        new EqualityCondition(@this, new NumericBindValue<uint>(rightSide));

    public static IEqualityCondition operator ==(
        NumericExpr @this,
        long rightSide) =>
        new EqualityCondition(@this, new NumericBindValue<long>(rightSide));

    public static IEqualityCondition operator ==(
        NumericExpr @this,
        ulong rightSide) =>
        new EqualityCondition(@this, new NumericBindValue<ulong>(rightSide));

    public static IEqualityCondition operator ==(
        NumericExpr @this,
        float rightSide) =>
        new EqualityCondition(@this, new NumericBindValue<float>(rightSide));

    public static IEqualityCondition operator ==(
        NumericExpr @this,
        double rightSide) =>
        new EqualityCondition(@this, new NumericBindValue<double>(rightSide));

    public static IEqualityCondition operator ==(
        NumericExpr @this,
        decimal rightSide) =>
        new EqualityCondition(@this, new NumericBindValue<decimal>(rightSide));

    public static IEqualityCondition operator ==(
        NumericExpr @this,
        Enum rightSide) =>
        new EqualityCondition(@this, new EnumBindValue(rightSide));

    public static IEqualityCondition operator !=(
        NumericExpr @this,
        NumericExpr rightSide) =>
        new InequalityCondition(@this, rightSide);

    public static IEqualityCondition operator !=(
        NumericExpr @this,
        sbyte rightSide) =>
        new InequalityCondition(@this, new NumericBindValue<sbyte>(rightSide));

    public static IEqualityCondition operator !=(
        NumericExpr @this,
        byte rightSide) =>
        new InequalityCondition(@this, new NumericBindValue<byte>(rightSide));

    public static IEqualityCondition operator !=(
        NumericExpr @this,
        short rightSide) =>
        new InequalityCondition(@this, new NumericBindValue<short>(rightSide));

    public static IEqualityCondition operator !=(
        NumericExpr @this,
        ushort rightSide) =>
        new InequalityCondition(@this, new NumericBindValue<ushort>(rightSide));

    public static IEqualityCondition operator !=(
        NumericExpr @this,
        int rightSide) =>
        new InequalityCondition(@this, new NumericBindValue<int>(rightSide));

    public static IEqualityCondition operator !=(
        NumericExpr @this,
        uint rightSide) =>
        new InequalityCondition(@this, new NumericBindValue<uint>(rightSide));

    public static IEqualityCondition operator !=(
        NumericExpr @this,
        long rightSide) =>
        new InequalityCondition(@this, new NumericBindValue<long>(rightSide));

    public static IEqualityCondition operator !=(
        NumericExpr @this,
        ulong rightSide) =>
        new InequalityCondition(@this, new NumericBindValue<ulong>(rightSide));

    public static IEqualityCondition operator !=(
        NumericExpr @this,
        float rightSide) =>
        new InequalityCondition(@this, new NumericBindValue<float>(rightSide));

    public static IEqualityCondition operator !=(
        NumericExpr @this,
        double rightSide) =>
        new InequalityCondition(@this, new NumericBindValue<double>(rightSide));

    public static IEqualityCondition operator !=(
        NumericExpr @this,
        decimal rightSide) =>
        new InequalityCondition(@this, new NumericBindValue<decimal>(rightSide));

    public static IEqualityCondition operator !=(
        NumericExpr @this,
        Enum rightSide) =>
        new InequalityCondition(@this, new EnumBindValue(rightSide));

    public static IComparisonCondition operator <(
        NumericExpr @this,
        NumericExpr rightSide) =>
        new LessThanCondition(@this, rightSide);

    public static IComparisonCondition operator <(
        NumericExpr @this,
        sbyte rightSide) =>
        new LessThanCondition(@this, new NumericBindValue<sbyte>(rightSide));

    public static IComparisonCondition operator <(
        NumericExpr @this,
        byte rightSide) =>
        new LessThanCondition(@this, new NumericBindValue<byte>(rightSide));

    public static IComparisonCondition operator <(
        NumericExpr @this,
        short rightSide) =>
        new LessThanCondition(@this, new NumericBindValue<short>(rightSide));

    public static IComparisonCondition operator <(
        NumericExpr @this,
        ushort rightSide) =>
        new LessThanCondition(@this, new NumericBindValue<ushort>(rightSide));

    public static IComparisonCondition operator <(
        NumericExpr @this,
        int rightSide) =>
        new LessThanCondition(@this, new NumericBindValue<int>(rightSide));

    public static IComparisonCondition operator <(
        NumericExpr @this,
        uint rightSide) =>
        new LessThanCondition(@this, new NumericBindValue<uint>(rightSide));

    public static IComparisonCondition operator <(
        NumericExpr @this,
        long rightSide) =>
        new LessThanCondition(@this, new NumericBindValue<long>(rightSide));

    public static IComparisonCondition operator <(
        NumericExpr @this,
        ulong rightSide) =>
        new LessThanCondition(@this, new NumericBindValue<ulong>(rightSide));

    public static IComparisonCondition operator <(
        NumericExpr @this,
        float rightSide) =>
        new LessThanCondition(@this, new NumericBindValue<float>(rightSide));

    public static IComparisonCondition operator <(
        NumericExpr @this,
        double rightSide) =>
        new LessThanCondition(@this, new NumericBindValue<double>(rightSide));

    public static IComparisonCondition operator <(
        NumericExpr @this,
        decimal rightSide) =>
        new LessThanCondition(@this, new NumericBindValue<decimal>(rightSide));

    public static IComparisonCondition operator <(
        NumericExpr @this,
        Enum rightSide) =>
        new LessThanCondition(@this, new EnumBindValue(rightSide));

    public static IComparisonCondition operator >(
        NumericExpr @this,
        NumericExpr rightSide) =>
        new GreaterThanCondition(@this, rightSide);

    public static IComparisonCondition operator >(
        NumericExpr @this,
        sbyte rightSide) =>
        new GreaterThanCondition(@this, new NumericBindValue<sbyte>(rightSide));

    public static IComparisonCondition operator >(
        NumericExpr @this,
        byte rightSide) =>
        new GreaterThanCondition(@this, new NumericBindValue<byte>(rightSide));

    public static IComparisonCondition operator >(
        NumericExpr @this,
        short rightSide) =>
        new GreaterThanCondition(@this, new NumericBindValue<short>(rightSide));

    public static IComparisonCondition operator >(
        NumericExpr @this,
        ushort rightSide) =>
        new GreaterThanCondition(@this, new NumericBindValue<ushort>(rightSide));

    public static IComparisonCondition operator >(
        NumericExpr @this,
        int rightSide) =>
        new GreaterThanCondition(@this, new NumericBindValue<int>(rightSide));

    public static IComparisonCondition operator >(
        NumericExpr @this,
        uint rightSide) =>
        new GreaterThanCondition(@this, new NumericBindValue<uint>(rightSide));

    public static IComparisonCondition operator >(
        NumericExpr @this,
        long rightSide) =>
        new GreaterThanCondition(@this, new NumericBindValue<long>(rightSide));

    public static IComparisonCondition operator >(
        NumericExpr @this,
        ulong rightSide) =>
        new GreaterThanCondition(@this, new NumericBindValue<ulong>(rightSide));

    public static IComparisonCondition operator >(
        NumericExpr @this,
        float rightSide) =>
        new GreaterThanCondition(@this, new NumericBindValue<float>(rightSide));

    public static IComparisonCondition operator >(
        NumericExpr @this,
        double rightSide) =>
        new GreaterThanCondition(@this, new NumericBindValue<double>(rightSide));

    public static IComparisonCondition operator >(
        NumericExpr @this,
        decimal rightSide) =>
        new GreaterThanCondition(@this, new NumericBindValue<decimal>(rightSide));

    public static IComparisonCondition operator >(
        NumericExpr @this,
        Enum rightSide) =>
        new GreaterThanCondition(@this, new EnumBindValue(rightSide));

    public static IComparisonCondition operator <=(
        NumericExpr @this,
        NumericExpr rightSide) =>
        new LessThanOrEqualCondition(@this, rightSide);

    public static IComparisonCondition operator <=(
        NumericExpr @this,
        sbyte rightSide) =>
        new LessThanOrEqualCondition(@this, new NumericBindValue<sbyte>(rightSide));

    public static IComparisonCondition operator <=(
        NumericExpr @this,
        byte rightSide) =>
        new LessThanOrEqualCondition(@this, new NumericBindValue<byte>(rightSide));

    public static IComparisonCondition operator <=(
        NumericExpr @this,
        short rightSide) =>
        new LessThanOrEqualCondition(@this, new NumericBindValue<short>(rightSide));

    public static IComparisonCondition operator <=(
        NumericExpr @this,
        ushort rightSide) =>
        new LessThanOrEqualCondition(@this, new NumericBindValue<ushort>(rightSide));

    public static IComparisonCondition operator <=(
        NumericExpr @this,
        int rightSide) =>
        new LessThanOrEqualCondition(@this, new NumericBindValue<int>(rightSide));

    public static IComparisonCondition operator <=(
        NumericExpr @this,
        uint rightSide) =>
        new LessThanOrEqualCondition(@this, new NumericBindValue<uint>(rightSide));

    public static IComparisonCondition operator <=(
        NumericExpr @this,
        long rightSide) =>
        new LessThanOrEqualCondition(@this, new NumericBindValue<long>(rightSide));

    public static IComparisonCondition operator <=(
        NumericExpr @this,
        ulong rightSide) =>
        new LessThanOrEqualCondition(@this, new NumericBindValue<ulong>(rightSide));

    public static IComparisonCondition operator <=(
        NumericExpr @this,
        float rightSide) =>
        new LessThanOrEqualCondition(@this, new NumericBindValue<float>(rightSide));

    public static IComparisonCondition operator <=(
        NumericExpr @this,
        double rightSide) =>
        new LessThanOrEqualCondition(@this, new NumericBindValue<double>(rightSide));

    public static IComparisonCondition operator <=(
        NumericExpr @this,
        decimal rightSide) =>
        new LessThanOrEqualCondition(@this, new NumericBindValue<decimal>(rightSide));

    public static IComparisonCondition operator <=(
        NumericExpr @this,
        Enum rightSide) =>
        new LessThanOrEqualCondition(@this, new EnumBindValue(rightSide));

    public static IComparisonCondition operator >=(
        NumericExpr @this,
        NumericExpr rightSide) =>
        new GreaterThanOrEqualCondition(@this, rightSide);

    public static IComparisonCondition operator >=(
        NumericExpr @this,
        sbyte rightSide) =>
        new GreaterThanOrEqualCondition(@this, new NumericBindValue<sbyte>(rightSide));

    public static IComparisonCondition operator >=(
        NumericExpr @this,
        byte rightSide) =>
        new GreaterThanOrEqualCondition(@this, new NumericBindValue<byte>(rightSide));

    public static IComparisonCondition operator >=(
        NumericExpr @this,
        short rightSide) =>
        new GreaterThanOrEqualCondition(@this, new NumericBindValue<short>(rightSide));

    public static IComparisonCondition operator >=(
        NumericExpr @this,
        ushort rightSide) =>
        new GreaterThanOrEqualCondition(@this, new NumericBindValue<ushort>(rightSide));

    public static IComparisonCondition operator >=(
        NumericExpr @this,
        int rightSide) =>
        new GreaterThanOrEqualCondition(@this, new NumericBindValue<int>(rightSide));

    public static IComparisonCondition operator >=(
        NumericExpr @this,
        uint rightSide) =>
        new GreaterThanOrEqualCondition(@this, new NumericBindValue<uint>(rightSide));

    public static IComparisonCondition operator >=(
        NumericExpr @this,
        long rightSide) =>
        new GreaterThanOrEqualCondition(@this, new NumericBindValue<long>(rightSide));

    public static IComparisonCondition operator >=(
        NumericExpr @this,
        ulong rightSide) =>
        new GreaterThanOrEqualCondition(@this, new NumericBindValue<ulong>(rightSide));

    public static IComparisonCondition operator >=(
        NumericExpr @this,
        float rightSide) =>
        new GreaterThanOrEqualCondition(@this, new NumericBindValue<float>(rightSide));

    public static IComparisonCondition operator >=(
        NumericExpr @this,
        double rightSide) =>
        new GreaterThanOrEqualCondition(@this, new NumericBindValue<double>(rightSide));

    public static IComparisonCondition operator >=(
        NumericExpr @this,
        decimal rightSide) =>
        new GreaterThanOrEqualCondition(@this, new NumericBindValue<decimal>(rightSide));

    public static IComparisonCondition operator >=(
        NumericExpr @this,
        Enum rightSide) =>
        new GreaterThanOrEqualCondition(@this, new EnumBindValue(rightSide));

    public static NumericExpr operator +(
        NumericExpr @this,
        NumericExpr rightSide) =>
        new AdditionOperator(@this, rightSide);

    public static NumericExpr operator +(
        NumericExpr @this,
        sbyte rightSide) =>
        new AdditionOperator(@this, new NumericBindValue<sbyte>(rightSide));

    public static NumericExpr operator +(
        NumericExpr @this,
        byte rightSide) =>
        new AdditionOperator(@this, new NumericBindValue<byte>(rightSide));

    public static NumericExpr operator +(
        NumericExpr @this,
        short rightSide) =>
        new AdditionOperator(@this, new NumericBindValue<short>(rightSide));

    public static NumericExpr operator +(
        NumericExpr @this,
        ushort rightSide) =>
        new AdditionOperator(@this, new NumericBindValue<ushort>(rightSide));

    public static NumericExpr operator +(
        NumericExpr @this,
        int rightSide) =>
        new AdditionOperator(@this, new NumericBindValue<int>(rightSide));

    public static NumericExpr operator +(
        NumericExpr @this,
        uint rightSide) =>
        new AdditionOperator(@this, new NumericBindValue<uint>(rightSide));

    public static NumericExpr operator +(
        NumericExpr @this,
        long rightSide) =>
        new AdditionOperator(@this, new NumericBindValue<long>(rightSide));

    public static NumericExpr operator +(
        NumericExpr @this,
        ulong rightSide) =>
        new AdditionOperator(@this, new NumericBindValue<ulong>(rightSide));

    public static NumericExpr operator +(
        NumericExpr @this,
        float rightSide) =>
        new AdditionOperator(@this, new NumericBindValue<float>(rightSide));

    public static NumericExpr operator +(
        NumericExpr @this,
        double rightSide) =>
        new AdditionOperator(@this, new NumericBindValue<double>(rightSide));

    public static NumericExpr operator +(
        NumericExpr @this,
        decimal rightSide) =>
        new AdditionOperator(@this, new NumericBindValue<decimal>(rightSide));

    public static NumericExpr operator +(
        NumericExpr @this,
        Enum rightSide) =>
        new AdditionOperator(@this, new EnumBindValue(rightSide));

    public static NumericExpr operator -(
        NumericExpr @this,
        NumericExpr rightSide) =>
        new SubtractionOperator(@this, rightSide);

    public static NumericExpr operator -(
        NumericExpr @this,
        sbyte rightSide) =>
        new SubtractionOperator(@this, new NumericBindValue<sbyte>(rightSide));

    public static NumericExpr operator -(
        NumericExpr @this,
        byte rightSide) =>
        new SubtractionOperator(@this, new NumericBindValue<byte>(rightSide));

    public static NumericExpr operator -(
        NumericExpr @this,
        short rightSide) =>
        new SubtractionOperator(@this, new NumericBindValue<short>(rightSide));

    public static NumericExpr operator -(
        NumericExpr @this,
        ushort rightSide) =>
        new SubtractionOperator(@this, new NumericBindValue<ushort>(rightSide));

    public static NumericExpr operator -(
        NumericExpr @this,
        int rightSide) =>
        new SubtractionOperator(@this, new NumericBindValue<int>(rightSide));

    public static NumericExpr operator -(
        NumericExpr @this,
        uint rightSide) =>
        new SubtractionOperator(@this, new NumericBindValue<uint>(rightSide));

    public static NumericExpr operator -(
        NumericExpr @this,
        long rightSide) =>
        new SubtractionOperator(@this, new NumericBindValue<long>(rightSide));

    public static NumericExpr operator -(
        NumericExpr @this,
        ulong rightSide) =>
        new SubtractionOperator(@this, new NumericBindValue<ulong>(rightSide));

    public static NumericExpr operator -(
        NumericExpr @this,
        float rightSide) =>
        new SubtractionOperator(@this, new NumericBindValue<float>(rightSide));

    public static NumericExpr operator -(
        NumericExpr @this,
        double rightSide) =>
        new SubtractionOperator(@this, new NumericBindValue<double>(rightSide));

    public static NumericExpr operator -(
        NumericExpr @this,
        decimal rightSide) =>
        new SubtractionOperator(@this, new NumericBindValue<decimal>(rightSide));

    public static NumericExpr operator -(
        NumericExpr @this,
        Enum rightSide) =>
        new SubtractionOperator(@this, new EnumBindValue(rightSide));

    public static NumericExpr operator *(
        NumericExpr @this,
        NumericExpr rightSide) =>
        new MultiplicationOperator(@this, rightSide);

    public static NumericExpr operator *(
        NumericExpr @this,
        sbyte rightSide) =>
        new MultiplicationOperator(@this, new NumericBindValue<sbyte>(rightSide));

    public static NumericExpr operator *(
        NumericExpr @this,
        byte rightSide) =>
        new MultiplicationOperator(@this, new NumericBindValue<byte>(rightSide));

    public static NumericExpr operator *(
        NumericExpr @this,
        short rightSide) =>
        new MultiplicationOperator(@this, new NumericBindValue<short>(rightSide));

    public static NumericExpr operator *(
        NumericExpr @this,
        ushort rightSide) =>
        new MultiplicationOperator(@this, new NumericBindValue<ushort>(rightSide));

    public static NumericExpr operator *(
        NumericExpr @this,
        int rightSide) =>
        new MultiplicationOperator(@this, new NumericBindValue<int>(rightSide));

    public static NumericExpr operator *(
        NumericExpr @this,
        uint rightSide) =>
        new MultiplicationOperator(@this, new NumericBindValue<uint>(rightSide));

    public static NumericExpr operator *(
        NumericExpr @this,
        long rightSide) =>
        new MultiplicationOperator(@this, new NumericBindValue<long>(rightSide));

    public static NumericExpr operator *(
        NumericExpr @this,
        ulong rightSide) =>
        new MultiplicationOperator(@this, new NumericBindValue<ulong>(rightSide));

    public static NumericExpr operator *(
        NumericExpr @this,
        float rightSide) =>
        new MultiplicationOperator(@this, new NumericBindValue<float>(rightSide));

    public static NumericExpr operator *(
        NumericExpr @this,
        double rightSide) =>
        new MultiplicationOperator(@this, new NumericBindValue<double>(rightSide));

    public static NumericExpr operator *(
        NumericExpr @this,
        decimal rightSide) =>
        new MultiplicationOperator(@this, new NumericBindValue<decimal>(rightSide));

    public static NumericExpr operator *(
        NumericExpr @this,
        Enum rightSide) =>
        new MultiplicationOperator(@this, new EnumBindValue(rightSide));

    public static NumericExpr operator /(
        NumericExpr @this,
        NumericExpr rightSide) =>
        new DivisionOperator(@this, rightSide);

    public static NumericExpr operator /(
        NumericExpr @this,
        sbyte rightSide) =>
        new DivisionOperator(@this, new NumericBindValue<sbyte>(rightSide));

    public static NumericExpr operator /(
        NumericExpr @this,
        byte rightSide) =>
        new DivisionOperator(@this, new NumericBindValue<byte>(rightSide));

    public static NumericExpr operator /(
        NumericExpr @this,
        short rightSide) =>
        new DivisionOperator(@this, new NumericBindValue<short>(rightSide));

    public static NumericExpr operator /(
        NumericExpr @this,
        ushort rightSide) =>
        new DivisionOperator(@this, new NumericBindValue<ushort>(rightSide));

    public static NumericExpr operator /(
        NumericExpr @this,
        int rightSide) =>
        new DivisionOperator(@this, new NumericBindValue<int>(rightSide));

    public static NumericExpr operator /(
        NumericExpr @this,
        uint rightSide) =>
        new DivisionOperator(@this, new NumericBindValue<uint>(rightSide));

    public static NumericExpr operator /(
        NumericExpr @this,
        long rightSide) =>
        new DivisionOperator(@this, new NumericBindValue<long>(rightSide));

    public static NumericExpr operator /(
        NumericExpr @this,
        ulong rightSide) =>
        new DivisionOperator(@this, new NumericBindValue<ulong>(rightSide));

    public static NumericExpr operator /(
        NumericExpr @this,
        float rightSide) =>
        new DivisionOperator(@this, new NumericBindValue<float>(rightSide));

    public static NumericExpr operator /(
        NumericExpr @this,
        double rightSide) =>
        new DivisionOperator(@this, new NumericBindValue<double>(rightSide));

    public static NumericExpr operator /(
        NumericExpr @this,
        decimal rightSide) =>
        new DivisionOperator(@this, new NumericBindValue<decimal>(rightSide));

    public static NumericExpr operator /(
        NumericExpr @this,
        Enum rightSide) =>
        new DivisionOperator(@this, new EnumBindValue(rightSide));

    public static NumericExpr operator %(
        NumericExpr @this,
        NumericExpr rightSide) =>
        new ModulusOperator(@this, rightSide);

    public static NumericExpr operator %(
        NumericExpr @this,
        sbyte rightSide) =>
        new ModulusOperator(@this, new NumericBindValue<sbyte>(rightSide));

    public static NumericExpr operator %(
        NumericExpr @this,
        byte rightSide) =>
        new ModulusOperator(@this, new NumericBindValue<byte>(rightSide));

    public static NumericExpr operator %(
        NumericExpr @this,
        short rightSide) =>
        new ModulusOperator(@this, new NumericBindValue<short>(rightSide));

    public static NumericExpr operator %(
        NumericExpr @this,
        ushort rightSide) =>
        new ModulusOperator(@this, new NumericBindValue<ushort>(rightSide));

    public static NumericExpr operator %(
        NumericExpr @this,
        int rightSide) =>
        new ModulusOperator(@this, new NumericBindValue<int>(rightSide));

    public static NumericExpr operator %(
        NumericExpr @this,
        uint rightSide) =>
        new ModulusOperator(@this, new NumericBindValue<uint>(rightSide));

    public static NumericExpr operator %(
        NumericExpr @this,
        long rightSide) =>
        new ModulusOperator(@this, new NumericBindValue<long>(rightSide));

    public static NumericExpr operator %(
        NumericExpr @this,
        ulong rightSide) =>
        new ModulusOperator(@this, new NumericBindValue<ulong>(rightSide));

    public static NumericExpr operator %(
        NumericExpr @this,
        float rightSide) =>
        new ModulusOperator(@this, new NumericBindValue<float>(rightSide));

    public static NumericExpr operator %(
        NumericExpr @this,
        double rightSide) =>
        new ModulusOperator(@this, new NumericBindValue<double>(rightSide));

    public static NumericExpr operator %(
        NumericExpr @this,
        decimal rightSide) =>
        new ModulusOperator(@this, new NumericBindValue<decimal>(rightSide));

    public static NumericExpr operator %(
        NumericExpr @this,
        Enum rightSide) =>
        new ModulusOperator(@this, new EnumBindValue(rightSide));

    public ExprAlias AS(string alias) => new(this, alias);

    public BetweenCondition BETWEEN(
        NumericExpr rightSide1,
        NumericExpr rightSide2) => new(this, rightSide1, rightSide2);

    public BetweenCondition BETWEEN(
        NumericExpr rightSide1,
        sbyte rightSide2) => new(this, rightSide1, new NumericBindValue<sbyte>(rightSide2));

    public BetweenCondition BETWEEN(
        sbyte rightSide1,
        NumericExpr rightSide2) => new(this, new NumericBindValue<sbyte>(rightSide1), rightSide2);

    public BetweenCondition BETWEEN(
        sbyte rightSide1,
        sbyte rightSide2) => new(this, new NumericBindValue<sbyte>(rightSide1), new NumericBindValue<sbyte>(rightSide2));

    public BetweenCondition BETWEEN(
        NumericExpr rightSide1,
        byte rightSide2) => new(this, rightSide1, new NumericBindValue<byte>(rightSide2));

    public BetweenCondition BETWEEN(
        byte rightSide1,
        NumericExpr rightSide2) => new(this, new NumericBindValue<byte>(rightSide1), rightSide2);

    public BetweenCondition BETWEEN(
        byte rightSide1,
        byte rightSide2) => new(this, new NumericBindValue<byte>(rightSide1), new NumericBindValue<byte>(rightSide2));

    public BetweenCondition BETWEEN(
        NumericExpr rightSide1,
        short rightSide2) => new(this, rightSide1, new NumericBindValue<short>(rightSide2));

    public BetweenCondition BETWEEN(
        short rightSide1,
        NumericExpr rightSide2) => new(this, new NumericBindValue<short>(rightSide1), rightSide2);

    public BetweenCondition BETWEEN(
        short rightSide1,
        short rightSide2) => new(this, new NumericBindValue<short>(rightSide1), new NumericBindValue<short>(rightSide2));
    public BetweenCondition BETWEEN(
        NumericExpr rightSide1,
        ushort rightSide2) => new(this, rightSide1, new NumericBindValue<ushort>(rightSide2));

    public BetweenCondition BETWEEN(
        ushort rightSide1,
        NumericExpr rightSide2) => new(this, new NumericBindValue<ushort>(rightSide1), rightSide2);

    public BetweenCondition BETWEEN(
        ushort rightSide1,
        ushort rightSide2) => new(this, new NumericBindValue<ushort>(rightSide1), new NumericBindValue<ushort>(rightSide2));

    public BetweenCondition BETWEEN(
        NumericExpr rightSide1,
        int rightSide2) => new(this, rightSide1, new NumericBindValue<int>(rightSide2));

    public BetweenCondition BETWEEN(
        int rightSide1,
        NumericExpr rightSide2) => new(this, new NumericBindValue<int>(rightSide1), rightSide2);

    public BetweenCondition BETWEEN(
        int rightSide1,
        int rightSide2) => new(this, new NumericBindValue<int>(rightSide1), new NumericBindValue<int>(rightSide2));

    public BetweenCondition BETWEEN(
        NumericExpr rightSide1,
        uint rightSide2) => new(this, rightSide1, new NumericBindValue<uint>(rightSide2));

    public BetweenCondition BETWEEN(
        uint rightSide1,
        NumericExpr rightSide2) => new(this, new NumericBindValue<uint>(rightSide1), rightSide2);

    public BetweenCondition BETWEEN(
        uint rightSide1,
        uint rightSide2) => new(this, new NumericBindValue<uint>(rightSide1), new NumericBindValue<uint>(rightSide2));

    public BetweenCondition BETWEEN(
        NumericExpr rightSide1,
        long rightSide2) => new(this, rightSide1, new NumericBindValue<long>(rightSide2));

    public BetweenCondition BETWEEN(
        long rightSide1,
        NumericExpr rightSide2) => new(this, new NumericBindValue<long>(rightSide1), rightSide2);

    public BetweenCondition BETWEEN(
        long rightSide1,
        long rightSide2) => new(this, new NumericBindValue<long>(rightSide1), new NumericBindValue<long>(rightSide2));

    public BetweenCondition BETWEEN(
        NumericExpr rightSide1,
        ulong rightSide2) => new(this, rightSide1, new NumericBindValue<ulong>(rightSide2));

    public BetweenCondition BETWEEN(
        ulong rightSide1,
        NumericExpr rightSide2) => new(this, new NumericBindValue<ulong>(rightSide1), rightSide2);

    public BetweenCondition BETWEEN(
        ulong rightSide1,
        ulong rightSide2) => new(this, new NumericBindValue<ulong>(rightSide1), new NumericBindValue<ulong>(rightSide2));

    public BetweenCondition BETWEEN(
        NumericExpr rightSide1,
        float rightSide2) => new(this, rightSide1, new NumericBindValue<float>(rightSide2));

    public BetweenCondition BETWEEN(
        float rightSide1,
        NumericExpr rightSide2) => new(this, new NumericBindValue<float>(rightSide1), rightSide2);

    public BetweenCondition BETWEEN(
        float rightSide1,
        float rightSide2) => new(this, new NumericBindValue<float>(rightSide1), new NumericBindValue<float>(rightSide2));

    public BetweenCondition BETWEEN(
        NumericExpr rightSide1,
        double rightSide2) => new(this, rightSide1, new NumericBindValue<double>(rightSide2));

    public BetweenCondition BETWEEN(
        double rightSide1,
        NumericExpr rightSide2) => new(this, new NumericBindValue<double>(rightSide1), rightSide2);

    public BetweenCondition BETWEEN(
        double rightSide1,
        double rightSide2) => new(this, new NumericBindValue<double>(rightSide1), new NumericBindValue<double>(rightSide2));

    public BetweenCondition BETWEEN(
        NumericExpr rightSide1,
        decimal rightSide2) => new(this, rightSide1, new NumericBindValue<decimal>(rightSide2));

    public BetweenCondition BETWEEN(
        decimal rightSide1,
        NumericExpr rightSide2) => new(this, new NumericBindValue<decimal>(rightSide1), rightSide2);

    public BetweenCondition BETWEEN(
        decimal rightSide1,
        decimal rightSide2) => new(this, new NumericBindValue<decimal>(rightSide1), new NumericBindValue<decimal>(rightSide2));

    public BetweenCondition BETWEEN(
        NumericExpr rightSide1,
        Enum rightSide2) =>
        new(this, rightSide1, new EnumBindValue(rightSide2));

    public BetweenCondition BETWEEN(
        Enum rightSide1,
        NumericExpr rightSide2) =>
        new(this, new EnumBindValue(rightSide1), rightSide2);

    public BetweenCondition BETWEEN<TEnum>(
        TEnum rightSide1,
        TEnum rightSide2)
        where TEnum : Enum =>
        new(this, new EnumBindValue(rightSide1), new EnumBindValue(rightSide2));

    public NotBetweenCondition NOT_BETWEEN(
        NumericExpr rightSide1,
        NumericExpr rightSide2) => new(this, rightSide1, rightSide2);

    public NotBetweenCondition NOT_BETWEEN(
        NumericExpr rightSide1,
        sbyte rightSide2) => new(this, rightSide1, new NumericBindValue<sbyte>(rightSide2));

    public NotBetweenCondition NOT_BETWEEN(
        sbyte rightSide1,
        NumericExpr rightSide2) => new(this, new NumericBindValue<sbyte>(rightSide1), rightSide2);

    public NotBetweenCondition NOT_BETWEEN(
        sbyte rightSide1,
        sbyte rightSide2) => new(this, new NumericBindValue<sbyte>(rightSide1), new NumericBindValue<sbyte>(rightSide2));

    public NotBetweenCondition NOT_BETWEEN(
        NumericExpr rightSide1,
        byte rightSide2) => new(this, rightSide1, new NumericBindValue<byte>(rightSide2));

    public NotBetweenCondition NOT_BETWEEN(
        byte rightSide1,
        NumericExpr rightSide2) => new(this, new NumericBindValue<byte>(rightSide1), rightSide2);

    public NotBetweenCondition NOT_BETWEEN(
        byte rightSide1,
        byte rightSide2) => new(this, new NumericBindValue<byte>(rightSide1), new NumericBindValue<byte>(rightSide2));

    public NotBetweenCondition NOT_BETWEEN(
        NumericExpr rightSide1,
        short rightSide2) => new(this, rightSide1, new NumericBindValue<short>(rightSide2));

    public NotBetweenCondition NOT_BETWEEN(
        short rightSide1,
        NumericExpr rightSide2) => new(this, new NumericBindValue<short>(rightSide1), rightSide2);

    public NotBetweenCondition NOT_BETWEEN(
        short rightSide1,
        short rightSide2) => new(this, new NumericBindValue<short>(rightSide1), new NumericBindValue<short>(rightSide2));

    public NotBetweenCondition NOT_BETWEEN(
        NumericExpr rightSide1,
        ushort rightSide2) => new(this, rightSide1, new NumericBindValue<ushort>(rightSide2));

    public NotBetweenCondition NOT_BETWEEN(
        ushort rightSide1,
        NumericExpr rightSide2) => new(this, new NumericBindValue<ushort>(rightSide1), rightSide2);

    public NotBetweenCondition NOT_BETWEEN(
        ushort rightSide1,
        ushort rightSide2) => new(this, new NumericBindValue<ushort>(rightSide1), new NumericBindValue<ushort>(rightSide2));

    public NotBetweenCondition NOT_BETWEEN(
        NumericExpr rightSide1,
        int rightSide2) => new(this, rightSide1, new NumericBindValue<int>(rightSide2));

    public NotBetweenCondition NOT_BETWEEN(
        int rightSide1,
        NumericExpr rightSide2) => new(this, new NumericBindValue<int>(rightSide1), rightSide2);

    public NotBetweenCondition NOT_BETWEEN(
        int rightSide1,
        int rightSide2) => new(this, new NumericBindValue<int>(rightSide1), new NumericBindValue<int>(rightSide2));

    public NotBetweenCondition NOT_BETWEEN(
        NumericExpr rightSide1,
        uint rightSide2) => new(this, rightSide1, new NumericBindValue<uint>(rightSide2));

    public NotBetweenCondition NOT_BETWEEN(
        uint rightSide1,
        NumericExpr rightSide2) => new(this, new NumericBindValue<uint>(rightSide1), rightSide2);

    public NotBetweenCondition NOT_BETWEEN(
        uint rightSide1,
        uint rightSide2) => new(this, new NumericBindValue<uint>(rightSide1), new NumericBindValue<uint>(rightSide2));

    public NotBetweenCondition NOT_BETWEEN(
        NumericExpr rightSide1,
        long rightSide2) => new(this, rightSide1, new NumericBindValue<long>(rightSide2));

    public NotBetweenCondition NOT_BETWEEN(
        long rightSide1,
        NumericExpr rightSide2) => new(this, new NumericBindValue<long>(rightSide1), rightSide2);

    public NotBetweenCondition NOT_BETWEEN(
        long rightSide1,
        long rightSide2) => new(this, new NumericBindValue<long>(rightSide1), new NumericBindValue<long>(rightSide2));

    public NotBetweenCondition NOT_BETWEEN(
        NumericExpr rightSide1,
        ulong rightSide2) => new(this, rightSide1, new NumericBindValue<ulong>(rightSide2));

    public NotBetweenCondition NOT_BETWEEN(
        ulong rightSide1,
        NumericExpr rightSide2) => new(this, new NumericBindValue<ulong>(rightSide1), rightSide2);

    public NotBetweenCondition NOT_BETWEEN(
        ulong rightSide1,
        ulong rightSide2) => new(this, new NumericBindValue<ulong>(rightSide1), new NumericBindValue<ulong>(rightSide2));

    public NotBetweenCondition NOT_BETWEEN(
        NumericExpr rightSide1,
        float rightSide2) => new(this, rightSide1, new NumericBindValue<float>(rightSide2));

    public NotBetweenCondition NOT_BETWEEN(
        float rightSide1,
        NumericExpr rightSide2) => new(this, new NumericBindValue<float>(rightSide1), rightSide2);

    public NotBetweenCondition NOT_BETWEEN(
        float rightSide1,
        float rightSide2) => new(this, new NumericBindValue<float>(rightSide1), new NumericBindValue<float>(rightSide2));

    public NotBetweenCondition NOT_BETWEEN(
        NumericExpr rightSide1,
        double rightSide2) => new(this, rightSide1, new NumericBindValue<double>(rightSide2));

    public NotBetweenCondition NOT_BETWEEN(
        double rightSide1,
        NumericExpr rightSide2) => new(this, new NumericBindValue<double>(rightSide1), rightSide2);

    public NotBetweenCondition NOT_BETWEEN(
        double rightSide1,
        double rightSide2) => new(this, new NumericBindValue<double>(rightSide1), new NumericBindValue<double>(rightSide2));

    public NotBetweenCondition NOT_BETWEEN(
        NumericExpr rightSide1,
        decimal rightSide2) => new(this, rightSide1, new NumericBindValue<decimal>(rightSide2));

    public NotBetweenCondition NOT_BETWEEN(
        decimal rightSide1,
        NumericExpr rightSide2) => new(this, new NumericBindValue<decimal>(rightSide1), rightSide2);

    public NotBetweenCondition NOT_BETWEEN(
        decimal rightSide1,
        decimal rightSide2) => new(this, new NumericBindValue<decimal>(rightSide1), new NumericBindValue<decimal>(rightSide2));

    public NotBetweenCondition NOT_BETWEEN(
        NumericExpr rightSide1,
        Enum rightSide2) =>
        new(this, rightSide1, new EnumBindValue(rightSide2));

    public NotBetweenCondition NOT_BETWEEN(
        Enum rightSide1,
        NumericExpr rightSide2) =>
        new(this, new EnumBindValue(rightSide1), rightSide2);

    public NotBetweenCondition NOT_BETWEEN<TEnum>(
        TEnum rightSide1,
        TEnum rightSide2)
        where TEnum : Enum =>
        new(this, new EnumBindValue(rightSide1), new EnumBindValue(rightSide2));

    public InCondition IN(params NumericExpr[] expressions) =>
        new(this, expressions);

    public InCondition IN(params sbyte[] values) =>
        new(this, BindValueArrayFactory.FromNumber(values));

    public InCondition IN(params byte[] values) =>
        new(this, BindValueArrayFactory.FromNumber(values));

    public InCondition IN(params short[] values) =>
        new(this, BindValueArrayFactory.FromNumber(values));

    public InCondition IN(params ushort[] values) =>
        new(this, BindValueArrayFactory.FromNumber(values));

    public InCondition IN(params int[] values) =>
        new(this, BindValueArrayFactory.FromNumber(values));

    public InCondition IN(params uint[] values) =>
        new(this, BindValueArrayFactory.FromNumber(values));

    public InCondition IN(params long[] values) =>
        new(this, BindValueArrayFactory.FromNumber(values));

    public InCondition IN(params ulong[] values) =>
        new(this, BindValueArrayFactory.FromNumber(values));

    public InCondition IN(params float[] values) =>
        new(this, BindValueArrayFactory.FromNumber(values));

    public InCondition IN(params double[] values) =>
        new(this, BindValueArrayFactory.FromNumber(values));

    public InCondition IN(params decimal[] values) =>
        new(this, BindValueArrayFactory.FromNumber(values));

    public InCondition IN<TEnum>(params TEnum[] values)
        where TEnum : Enum =>
        new(this, BindValueArrayFactory.FromEnum(values));

    public InSubqueryCondition IN(ISubquery subquery) =>
        new(this, subquery);

    public NotInCondition NOT_IN(params NumericExpr[] expressions) =>
        new(this, expressions);

    public NotInCondition NOT_IN(params sbyte[] values) =>
        new(this, BindValueArrayFactory.FromNumber(values));

    public NotInCondition NOT_IN(params byte[] values) =>
        new(this, BindValueArrayFactory.FromNumber(values));

    public NotInCondition NOT_IN(params short[] values) =>
        new(this, BindValueArrayFactory.FromNumber(values));

    public NotInCondition NOT_IN(params ushort[] values) =>
        new(this, BindValueArrayFactory.FromNumber(values));

    public NotInCondition NOT_IN(params int[] values) =>
        new(this, BindValueArrayFactory.FromNumber(values));

    public NotInCondition NOT_IN(params uint[] values) =>
        new(this, BindValueArrayFactory.FromNumber(values));

    public NotInCondition NOT_IN(params long[] values) =>
        new(this, BindValueArrayFactory.FromNumber(values));

    public NotInCondition NOT_IN(params ulong[] values) =>
        new(this, BindValueArrayFactory.FromNumber(values));

    public NotInCondition NOT_IN(params float[] values) =>
        new(this, BindValueArrayFactory.FromNumber(values));

    public NotInCondition NOT_IN(params double[] values) =>
        new(this, BindValueArrayFactory.FromNumber(values));

    public NotInCondition NOT_IN(params decimal[] values) =>
        new(this, BindValueArrayFactory.FromNumber(values));

    public NotInCondition NOT_IN<TEnum>(params TEnum[] values)
        where TEnum : Enum =>
        new(this, BindValueArrayFactory.FromEnum(values));

    public NotInSubqueryCondition NOT_IN(ISubquery subquery) =>
        new(this, subquery);
}
