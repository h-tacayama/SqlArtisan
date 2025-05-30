﻿using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    public static MaxFunction Max(object expr) => new(Resolve(expr));

    public static MinFunction Min(object expr) => new(Resolve(expr));

    public static ModFunction Mod(
        object dividend,
        object divisor) => new(
            Resolve(dividend),
            Resolve(divisor));

    public static MonthsBetweenFunction MonthsBetween(
        object date1,
        object date2) => new(
            Resolve(date1),
            Resolve(date2));
}
