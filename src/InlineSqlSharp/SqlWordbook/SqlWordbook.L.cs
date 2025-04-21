using static InlineSqlSharp.ExprResolver;

namespace InlineSqlSharp;

public static partial class SqlWordbook
{
    public static LastDayFunction LAST_DAY(object date) =>
        new(Resolve(date));

    public static LeastFunction LEAST(params object[] expressions) =>
        new(Resolve(expressions));

    public static LengthFunction LENGTH(object source) =>
        new(Resolve(source));

    public static LengthBFunction LENGTHB(object source) =>
        new(Resolve(source));

    public static LowerFunction LOWER(object source) =>
        new(Resolve(source));

    public static LpadFunction LPAD(
        object source,
        object length) => new(
            Resolve(source),
            Resolve(length));

    public static LpadFunction LPAD(
        object source,
        object length,
        object padding) => new(
            Resolve(source),
            Resolve(length),
            Resolve(padding));

    public static LtrimFunction LTRIM(object source) =>
        new(Resolve(source));

    public static LtrimFunction LTRIM(
        object source,
        object trimChars) => new(
            Resolve(source),
            Resolve(trimChars));
}
