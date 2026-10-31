using System.Globalization;

namespace SqlArtisan.IntegrationTests.ValueReading;

// The intended result of an E5 example, computed in C# from the seed rows wherever the
// value's category matches the left operand's type; null where no single meaning exists
// (a cross-category pair, integer division, a time zone, a driver-mapped type).
internal static class ValuePositionOracle
{
    private static readonly (int Id, string A, int B, DateTime D)[] Rows =
        [.. ExampleCatalog.Rows
            .Select(r => (r.Id, r.A, r.B, new DateTime(2024, 1, r.Id, 10, 0, 0)))];

    internal static string? Expected(ValuePositionExample e)
    {
        decimal? num = Number(e.Kind);
        bool text = e.Kind is ValueKind.Text or ValueKind.Char or ValueKind.BindText;
        bool nul = e.Kind is ValueKind.BindNull or ValueKind.SqlNull;
        DateTime? date = e.Kind == ValueKind.DateTime
            ? new DateTime(2024, 1, 2, 3, 4, 5) : null;

        switch (e.Family)
        {
            case "CMP":
                if (nul)
                {
                    return "";
                }

                if (e.Kind == ValueKind.Quantified && e.Left == LeftType.Int
                    && e.Class == PositionClass.QuantifiableOperand && e.Op is not ("Like"
                        or "NotLike"))
                {
                    return Ids(r => new[] { 40m, 20m }.Any(x => Cmp(r.B, e.Op, x)));
                }

                if (e.Left == LeftType.Int && e.Op is not ("Like" or "NotLike"))
                {
                    if (num is { } x)
                    {
                        return Ids(r => Cmp(r.B, e.Op, x));
                    }

                    return e.Kind switch
                    {
                        ValueKind.Column => Ids(r => Cmp(r.B, e.Op, r.B)),
                        ValueKind.Expression => Ids(r => Cmp(r.B, e.Op, r.B + r.Id)),
                        ValueKind.Subquery => Ids(r => Cmp(r.B, e.Op, 40)),
                        _ => null,
                    };
                }

                if (e.Left == LeftType.Text && text)
                {
                    return Ids(r => e.Op switch
                    {
                        "Like" => r.A == "b",
                        "NotLike" => r.A != "b",
                        _ => Cmp(string.CompareOrdinal(r.A, "b"), e.Op, 0),
                    });
                }

                return e.Left == LeftType.Date && date is { } dt && e.Op is not ("Like"
                    or "NotLike")
                    ? Ids(r => Cmp(r.D.CompareTo(dt), e.Op, 0))
                    : null;

            case "ARITH":
                if (e.Left == LeftType.Int && num is { } n && e.Op is "+" or "-" or "*")
                {
                    return Each(r => Fmt(e.Op switch
                    {
                        "+" => r.B + n,
                        "-" => r.B - n,
                        _ => r.B * n,
                    }));
                }

                if (e.Left == LeftType.Int && e.Kind is ValueKind.Column or ValueKind.Expression
                    && e.Op is "+" or "-" or "*")
                {
                    return Each(r =>
                    {
                        decimal y = e.Kind == ValueKind.Column ? r.B : r.B + r.Id;
                        return Fmt(e.Op == "+" ? r.B + y : e.Op == "-" ? r.B - y : r.B * y);
                    });
                }

                return e.Left == LeftType.Date && e.Kind == ValueKind.TimeSpan
                    && e.Op is "+" or "-"
                    ? Each(r => (e.Op == "+" ? r.D.AddHours(2) : r.D.AddHours(-2))
                        .ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))
                    : null;

            case "CASE":
                if (nul)
                {
                    return Each(_ => "miss");
                }

                return e.Left switch
                {
                    LeftType.Int when num is { } c => Each(r => r.B == c ? "hit" : "miss"),
                    LeftType.Int when e.Kind == ValueKind.Column => Each(_ => "hit"),
                    LeftType.Int when e.Kind == ValueKind.Expression => Each(_ => "miss"),
                    LeftType.Text when text => Each(r => r.A == "b" ? "hit" : "miss"),
                    LeftType.Date when date is { } cd => Each(r => r.D == cd ? "hit" : "miss"),
                    _ => null,
                };

            case "COALESCE":
                return e.Left switch
                {
                    LeftType.Int when num is not null || nul
                        || e.Kind is ValueKind.Column or ValueKind.Expression
                        or ValueKind.Subquery => Each(r => Fmt(r.B)),
                    LeftType.Text when text || nul => Each(r => r.A),
                    LeftType.Date when date is not null || nul
                        => Each(r => r.D.ToString(
                            "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
                    _ => null,
                };

            case "SET2":
            case "ODKU":
                // Standard SQL reads every right side from the row before the UPDATE, so
                // X2 takes X's original value whatever was written to X.
                string? written = Expected(e with { Family = "SET" });
                string original = e.Left switch
                {
                    LeftType.Int => "10",
                    LeftType.Text => "x",
                    _ => "2024-01-01 10:00:00",
                };
                return written is null ? null : $"{written}:{original}";

            case "INS":
            case "SET":
                if (nul)
                {
                    return "NULL";
                }

                return e.Left switch
                {
                    LeftType.Int when num is { } i && i == decimal.Truncate(i) => Fmt(i),
                    LeftType.Int when e.Kind == ValueKind.Subquery && e.Family == "INS" => "40",
                    LeftType.Text when text => "b",
                    LeftType.Date when date is { } wd
                        => wd.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                    _ => null,
                };

            default:
                return Echo(e, num, text, nul);
        }
    }

    private static string? Echo(ValuePositionExample e, decimal? num, bool text, bool nul)
    {
        if (e.Family == "UPPER")
        {
            return text ? "B" : nul ? "NULL" : null;
        }

        if (e.Family == "ABS")
        {
            return num is { } a ? Fmt(Math.Abs(a)) : nul ? "NULL" : null;
        }

        if (e.Family == "BIND" && e.Kind == ValueKind.FunctionArgEnum)
        {
            return Fmt((int)DateTimePart.Year);
        }

        return num is { } v ? Fmt(v)
            : text ? "b"
            : nul ? "NULL"
            : e.Kind switch
            {
                ValueKind.Bool => "1",
                ValueKind.DateTime => "2024-01-02 03:04:05",
                ValueKind.Column => "50",
                ValueKind.Expression => "51",
                ValueKind.Subquery => "40",
                _ => null,
            };
    }

    private static decimal? Number(ValueKind k) => k switch
    {
        ValueKind.IntZero => 0,
        ValueKind.IntPositive or ValueKind.Long or ValueKind.Short or ValueKind.Byte
            or ValueKind.BindInt or ValueKind.UserEnum or ValueKind.DoubleWhole => 2,
        ValueKind.IntNegative => -1,
        ValueKind.Double or ValueKind.Decimal => 2.5m,
        _ => null,
    };

    private static bool Cmp(decimal l, string op, decimal r) => op switch
    {
        "==" or "Between" or "In" or "Like" => l == r,
        "!=" or "NotBetween" or "NotIn" or "NotLike" => l != r,
        "<" => l < r,
        "<=" => l <= r,
        ">" => l > r,
        _ => l >= r,
    };

    private static string Ids(Func<(int Id, string A, int B, DateTime D), bool> keep) =>
        string.Join(",", Rows.Where(keep).Select(r => r.Id.ToString(CultureInfo.InvariantCulture)));

    private static string Each(Func<(int Id, string A, int B, DateTime D), string> value) =>
        string.Join(",", Rows.Select(r => $"{r.Id}:{value(r)}"));

    private static string Fmt(decimal d) =>
        d.ToString("0.############", CultureInfo.InvariantCulture);
}
