using static InlineSqlSharp.ExprRsolver;

namespace InlineSqlSharp;
public static partial class SqlWordbook
{
    public static CaseThenExpr THEN(object thenExpr) =>
        new(Resolve(thenExpr));

    public static ToDateFunction TO_DATE(
        object text,
        object format) => new(
            Resolve(text),
            Resolve(format));

    public static TrimFunction TRIM(object source) =>
        new(Resolve(source));

    public static TrimFunction TRIM(
        object source,
        object trimChar) => new(
            Resolve(source),
            Resolve(trimChar));

    public static ToCharFunction TO_CHAR(object expr) =>
        new(Resolve(expr));

    public static ToCharFunction TO_CHAR(
        object expr,
        object format) => new(
            Resolve(expr),
            Resolve(format));

    public static ToNumberFunction TO_NUMBER(object expr) =>
        new(Resolve(expr));

    public static ToNumberFunction TO_NUMBER(
        object expr,
        object numericFormat) => new(
            Resolve(expr),
            Resolve(numericFormat));

    public static TruncFunction TRUNC(object expr) =>
        new(Resolve(expr));

    public static TruncFunction TRUNC(
        object expr,
        object format) => new(
            Resolve(expr),
            Resolve(format));
}
