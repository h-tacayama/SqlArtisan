using static InlineSqlSharp.ExprResolver;

namespace InlineSqlSharp;

public static partial class SqlWordbook
{
    public static LastDayFunction LastDay(object date) =>
        new(Resolve(date));

    public static LeastFunction Least(params object[] expressions) =>
        new(Resolve(expressions));

    public static LengthFunction Length(object source) =>
        new(Resolve(source));

    public static LengthBFunction LengthB(object source) =>
        new(Resolve(source));

    public static LowerFunction Lower(object source) =>
        new(Resolve(source));

    public static LPadFunction LPad(
        object source,
        object length) => new(
            Resolve(source),
            Resolve(length));

    public static LPadFunction LPad(
        object source,
        object length,
        object padding) => new(
            Resolve(source),
            Resolve(length),
            Resolve(padding));

    public static LTrimFunction LTrim(object source) =>
        new(Resolve(source));

    public static LTrimFunction LTrim(
        object source,
        object trimChars) => new(
            Resolve(source),
            Resolve(trimChars));
}
