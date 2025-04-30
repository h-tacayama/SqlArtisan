using static SqlArtisan.ExprResolver;

namespace SqlArtisan;

public static partial class SqlWordbook
{
    public static AbsFunction Abs(object expr) => new(Resolve(expr));

    public static AbstractCondition ConditionIf(
        bool when,
        AbstractCondition condition) =>
        when ? condition : new EmptyCondition();

    public static AddMonthsFunction AddMonths(
        object dateTime,
        object months) => new(
            Resolve(dateTime),
            Resolve(months));

    public static AvgFunction Avg(object expr) =>
        new(Resolve(expr));

    public static AvgFunction Avg(DistinctKeyword distinct, object expr) =>
        new(distinct, Resolve(expr));
}
