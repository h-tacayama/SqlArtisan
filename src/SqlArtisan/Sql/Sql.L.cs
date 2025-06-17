using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    public static LastDayFunction LastDay(object date) =>
        new(Resolve(date));

    public static LeastFunction Least(params object[] expressions) =>
        new(Resolve(expressions));

    public static LengthFunction Length(object source) =>
        new(Resolve(source));

    public static LengthbFunction Lengthb(object source) =>
        new(Resolve(source));

    public static LowerFunction Lower(object source) =>
        new(Resolve(source));

    public static LpadFunction Lpad(
        object source,
        object length) => new(
            Resolve(source),
            Resolve(length));

    public static LpadFunction Lpad(
        object source,
        object length,
        object padding) => new(
            Resolve(source),
            Resolve(length),
            Resolve(padding));

    public static LtrimFunction Ltrim(object source) =>
        new(Resolve(source));

    public static LtrimFunction Ltrim(
        object source,
        object trimChars) => new(
            Resolve(source),
            Resolve(trimChars));
}
