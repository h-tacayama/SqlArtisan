using static InlineSqlSharp.ExprResolver;

namespace InlineSqlSharp;

public static partial class SqlWordbook
{
    public static AbsFunction ABS(object expr) => new(Resolve(expr));

    public static AbstractCondition AddConditionIf(
        bool addIf,
        AbstractCondition condition) =>
        addIf ? condition : new EmptyCondition();

    public static AddMonthsFunction ADD_MONTHS(
        object dateTime,
        object months) => new(
            Resolve(dateTime),
            Resolve(months));

    public static AndCondition AND(params AbstractCondition[] conditions) =>
        new(conditions);

    public static AvgFunction AVG(object expr) =>
        new(AllOrDistinct.All, Resolve(expr));

    public static AvgFunction AVG(AllOrDistinct allOrDistinct, object expr) =>
        new(allOrDistinct, Resolve(expr));
}
