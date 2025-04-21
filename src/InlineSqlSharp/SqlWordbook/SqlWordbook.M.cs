using static InlineSqlSharp.ExprResolver;

namespace InlineSqlSharp;

public static partial class SqlWordbook
{
    public static MaxFunction MAX(object expr) => new(Resolve(expr));

    public static MinFunction MIN(object expr) => new(Resolve(expr));

    public static ModFunction MOD(
        object dividend,
        object divisor) => new(
            Resolve(dividend),
            Resolve(divisor));

    public static MonthsBetweenFunction MONTHS_BETWEEN(
        object date1,
        object date2) => new(
            Resolve(date1),
            Resolve(date2));
}
