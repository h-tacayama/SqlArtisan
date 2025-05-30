﻿using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;
public static partial class Sql
{
    public static ToCharFunction ToChar(object expr) =>
        new(Resolve(expr));

    public static ToCharFunction ToChar(
        object expr,
        object format) => new(
            Resolve(expr),
            Resolve(format));

    public static ToDateFunction ToDate(
        object text,
        object format) => new(
            Resolve(text),
            Resolve(format));

    public static ToNumberFunction ToNumber(object expr) =>
        new(Resolve(expr));

    public static ToNumberFunction ToNumber(
        object expr,
        object numericFormat) => new(
            Resolve(expr),
            Resolve(numericFormat));

    public static ToTimestampFunction ToTimestamp(
        object text,
        object format) => new(
            Resolve(text),
            Resolve(format));

    public static TrimFunction Trim(object source) =>
        new(Resolve(source));

    public static TrimFunction Trim(
        object source,
        object trimChar) => new(
            Resolve(source),
            Resolve(trimChar));

    public static TruncFunction Trunc(object expr) =>
        new(Resolve(expr));

    public static TruncFunction Trunc(
        object expr,
        object format) => new(
            Resolve(expr),
            Resolve(format));
}
