using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SqlArtisan.IntegrationTests.Randomized;

/// <summary>Thrown where a query needs a fact the reference does not model: text ordering,
/// which each engine's collation decides.</summary>
internal sealed class ReferenceUnavailableException(string reason) : Exception(reason);

/// <summary>
/// Computes a query's rows from <see cref="RandomSchema"/>'s seed in C#, with three-valued
/// logic and each engine's default text equality, LIKE case rule and NULL ordering. Integer
/// arithmetic cannot overflow at the generated depths.
/// </summary>
internal static class ReferenceEvaluator
{
    /// <summary>The rows in the engine's normalized form, and whether their order is
    /// defined.</summary>
    public static (List<string> Rows, bool Ordered) Evaluate(QueryBlock query, Dbms dbms)
    {
        Collation collation = new(dbms);
        (List<object?[]> rows, bool ordered) = Block(query, Env.Empty, collation);
        return ([.. rows.Select(Normalize)], ordered);
    }

    public static string Normalize(object? value) => value switch
    {
        null or DBNull => "NULL",
        string s => "'" + s + "'",
        bool b => b ? "1" : "0",
        decimal m => m.ToString("0.############", CultureInfo.InvariantCulture),
        double d => d.ToString("R", CultureInfo.InvariantCulture),
        IConvertible c when value is long or int or short or byte =>
            c.ToInt64(CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture)!,
    };

    private static string Normalize(object?[] row) => string.Join("|", row.Select(Normalize));

    /// <summary>The table rows in scope, by table key; a key a LEFT JOIN found no match for
    /// maps to <see langword="null"/>.</summary>
    private sealed record Env(ImmutableDictionary<string, object?> Tables)
    {
        public static readonly Env Empty = new([]);

        public Env With(string key, object? row) => new(Tables.SetItem(key, row));
    }

    /// <summary>One result row before projection: a source row, or a group of them.</summary>
    private sealed record Context(Env Env, IReadOnlyList<Env>? Group);

    private static (List<object?[]> Rows, bool Ordered) Block(
        QueryBlock query,
        Env outer,
        Collation collation)
    {
        List<Context> contexts = Contexts(query, outer, collation);
        bool ordered = query.OrderBy.Count > 0;
        if (query.Tail.Count == 0 && ordered)
        {
            contexts.Sort((a, b) => CompareKeys(query.OrderBy, a, b, collation));
        }

        List<object?[]> rows = [.. contexts.Select(c => query.Items
            .Select(i => Value(i, c, collation))
            .ToArray())];
        if (query.IsDistinct)
        {
            rows = [.. rows.DistinctBy(Normalize, StringComparer.Ordinal)];
        }

        foreach (SetBranch branch in query.Tail)
        {
            (List<object?[]> right, _) = Block(branch.Query, outer, collation);
            rows = SetOperation(branch.Operator, rows, right);
        }

        if (query.Tail.Count > 0 && ordered)
        {
            ordered = SortByPositions(rows, query, collation);
        }

        if (query.Limit is { } limit)
        {
            rows = [.. rows.Skip(limit.Offset).Take(limit.Count)];
        }

        return (rows, ordered);
    }

    private static List<Context> Contexts(QueryBlock query, Env outer, Collation collation)
    {
        List<Env> source = [.. Source(query, outer, collation).Where(e =>
            query.Where is null || Test(query.Where, new Context(e, null), collation) == true)];
        bool grouped = query.GroupBy.Count > 0 || query.Items.Any(i => i.HasAggregate)
            || query.Having is not null;
        if (!grouped)
        {
            return [.. source.Select(e => new Context(e, null))];
        }

        List<Context> groups = query.GroupBy.Count == 0
            ? [new Context(source.FirstOrDefault() ?? outer, source)]
            : [.. source
                .GroupBy(e => string.Join("|", query.GroupBy.Select(k =>
                    Normalize(Value(k, new Context(e, null), collation)))))
                .Select(g => new Context(g.First(), g.ToList()))];
        return [.. groups.Where(g =>
            query.Having is null || Test(query.Having, g, collation) == true)];
    }

    private static IEnumerable<Env> Source(QueryBlock query, Env outer, Collation collation)
    {
        IEnumerable<Env> left = Rows(query.From).Select(r => outer.With(query.From, r));
        if (query.Join is not { } join)
        {
            return left;
        }

        return left.SelectMany(l =>
        {
            List<Env> matches = [.. Rows(join.Table)
                .Select(r => l.With(join.Table, r))
                .Where(e => Test(join.On, new Context(e, null), collation) == true)];
            return matches.Count > 0 || join.IsInner ? matches : [l.With(join.Table, null)];
        });
    }

    private static IEnumerable<object> Rows(string table) =>
        table[0] == 'u' ? RandomSchema.Users : RandomSchema.Depts;

    private static List<object?[]> SetOperation(
        SetOperatorLevel op,
        List<object?[]> left,
        List<object?[]> right)
    {
        HashSet<string> rightKeys = [.. right.Select(Normalize)];
        return op switch
        {
            SetOperatorLevel.Union => [.. left.Concat(right).DistinctBy(Normalize)],
            SetOperatorLevel.UnionAll => [.. left, .. right],
            SetOperatorLevel.Intersect =>
                [.. left.DistinctBy(Normalize).Where(r => rightKeys.Contains(Normalize(r)))],
            SetOperatorLevel.Except =>
                [.. left.DistinctBy(Normalize).Where(r => !rightKeys.Contains(Normalize(r)))],
            _ => throw new InvalidOperationException(op.ToString()),
        };
    }

    // A compound sorts by select-list position; a text column there sorts by collation, so
    // without a row limit the rows still compare as a multiset.
    private static bool SortByPositions(List<object?[]> rows, QueryBlock query, Collation collation)
    {
        int[] positions = [.. query.OrderBy.Cast<OrdinalKey>().Select(k => k.Position - 1)];
        if (rows.Any(r => positions.Any(p => r[p] is string)))
        {
            return query.Limit is null
                ? false
                : throw new ReferenceUnavailableException("a row limit over a text sort key");
        }

        rows.Sort((a, b) =>
        {
            foreach (int p in positions)
            {
                int order = CompareNullable(
                    (long?)a[p],
                    (long?)b[p],
                    false,
                    NullsPlacement.Default,
                    collation);
                if (order != 0)
                {
                    return order;
                }
            }

            return 0;
        });
        return true;
    }

    private static int CompareKeys(
        IReadOnlyList<SpecExpression> keys,
        Context a,
        Context b,
        Collation collation)
    {
        foreach (SpecExpression key in keys)
        {
            (SpecExpression expression, bool descending, NullsPlacement nulls) = key is SortKey s
                ? (s.Key, s.Descending, s.Nulls)
                : (key, false, NullsPlacement.Default);
            int order = CompareNullable(
                IntegerKey(Value(expression, a, collation)),
                IntegerKey(Value(expression, b, collation)),
                descending,
                nulls,
                collation);
            if (order != 0)
            {
                return order;
            }
        }

        return 0;
    }

    private static long? IntegerKey(object? value) => value is string
        ? throw new ReferenceUnavailableException("a text sort key")
        : (long?)value;

    private static int CompareNullable(
        long? x,
        long? y,
        bool descending,
        NullsPlacement nulls,
        Collation collation)
    {
        bool nullsFirst = nulls switch
        {
            NullsPlacement.First => true,
            NullsPlacement.Last => false,
            _ => collation.NullsSortHigh == descending,
        };
        return (x, y) switch
        {
            (null, null) => 0,
            (null, _) => nullsFirst ? -1 : 1,
            (_, null) => nullsFirst ? 1 : -1,
            _ => descending ? y.Value.CompareTo(x.Value) : x.Value.CompareTo(y.Value),
        };
    }

    private static object? Value(SpecExpression expression, Context context, Collation collation) =>
        expression switch
        {
            ColumnRef c => Column(c, context.Env),
            Literal l => l.Value is int i ? (long)i : l.Value,
            FunctionCall f => Function(f, context, collation),
            AggregateCall a => Aggregate(a, context, collation),
            CaseExpression e => Test(e.When, context, collation) == true
                ? Value(e.Then, context, collation)
                : Value(e.Else, context, collation),
            SortKey s => Value(s.Key, context, collation),
            ScalarSubquery s => Block(s.Query, context.Env, collation).Rows switch
            {
                [] => null,
                [var row] => row[0],
                _ => throw new ReferenceUnavailableException("a scalar subquery of several rows"),
            },
            _ => throw new InvalidOperationException(expression.GetType().Name),
        };

    private static object? Column(ColumnRef column, Env env) => env.Tables[column.Table] switch
    {
        null => null,
        UserRow u => column.Column switch
        {
            "id" => u.Id,
            "name" => u.Name,
            "age" => u.Age,
            _ => u.DeptId,
        },
        DeptRow d => column.Column == "id" ? d.Id : d.Name,
        _ => throw new InvalidOperationException(column.Table),
    };

    private static object? Aggregate(AggregateCall call, Context context, Collation collation)
    {
        IReadOnlyList<Env> group = context.Group
            ?? throw new InvalidOperationException("an aggregate outside a group");
        if (call.Argument is null)
        {
            return (long)group.Count;
        }

        List<object> values = [.. group
            .Select(e => Value(call.Argument, new Context(e, null), collation))
            .OfType<object>()];
        if (call.IsDistinct)
        {
            values = [.. values.DistinctBy(Normalize)];
        }

        return call.Kind switch
        {
            "Count" => (long)values.Count,
            _ when values.Count == 0 => null,
            _ when values.Any(v => v is string) =>
                throw new ReferenceUnavailableException("MAX or MIN over text"),
            "Sum" => values.Cast<long>().Sum(),
            "Max" => values.Cast<long>().Max(),
            "Min" => values.Cast<long>().Min(),
            _ => throw new InvalidOperationException(call.Kind),
        };
    }

    private static object? Function(FunctionCall call, Context context, Collation collation)
    {
        object?[] a = [.. call.Arguments.Select(x => Value(x, context, collation))];
        switch (call.Name)
        {
            case "Coalesce":
                return a[0] ?? a[1];
            case "Nullif":
                return Equal(a[0], a[1], collation) == true ? null : a[0];
        }

        if (a[0] is string s)
        {
            return call.Name switch
            {
                "Upper" => s.ToUpperInvariant(),
                "Lower" => s.ToLowerInvariant(),
                "Trim" => s.Trim(' '),
                "Length" => (long)s.Length,
                "Substr" or "Substring" => s[..Math.Min(2, s.Length)],
                "Replace" => s.Replace((string)a[1]!, (string)a[2]!, StringComparison.Ordinal),
                _ => throw new InvalidOperationException(call.Name),
            };
        }

        if (a[0] is not long x)
        {
            return null;
        }

        if (call.Name == "Abs")
        {
            return Math.Abs(x);
        }

        if (a[1] is not long y)
        {
            return null;
        }

        return call.Name switch
        {
            "+" => x + y,
            "-" => x - y,
            "*" => x * y,
            _ => throw new InvalidOperationException(call.Name),
        };
    }

    private static bool? Test(SpecCondition condition, Context context, Collation collation)
    {
        switch (condition)
        {
            case Comparison c:
                {
                    object? left = Value(c.Left, context, collation);
                    object? right = Value(c.Right, context, collation);
                    if (left is string || right is string)
                    {
                        bool? equal = Equal(left, right, collation);
                        return c.Operator switch
                        {
                            "==" => equal,
                            "!=" => !equal,
                            _ => throw new ReferenceUnavailableException(
                                "an ordered text comparison"),
                        };
                    }

                    if (left is not long x || right is not long y)
                    {
                        return null;
                    }

                    int k = x.CompareTo(y);
                    return c.Operator switch
                    {
                        "==" => k == 0,
                        "!=" => k != 0,
                        "<" => k < 0,
                        ">" => k > 0,
                        "<=" => k <= 0,
                        _ => k >= 0,
                    };
                }

            case NullTest n:
                return (Value(n.Operand, context, collation) is null) != n.Negated;
            case InList i:
                {
                    bool? any = In(
                        Value(i.Operand, context, collation),
                        i.Values.Select(l => Value(l, context, collation)),
                        collation);
                    return i.Negated ? !any : any;
                }

            case InSubquery s:
                {
                    bool? any = In(
                        Value(s.Operand, context, collation),
                        Block(s.Query, context.Env, collation).Rows.Select(r => r[0]),
                        collation);
                    return s.Negated ? !any : any;
                }

            case ExistsTest e:
                return (Block(e.Query, context.Env, collation).Rows.Count > 0) != e.Negated;
            case LikeTest l:
                {
                    if (Value(l.Operand, context, collation) is not string text)
                    {
                        return null;
                    }

                    return collation.Like((string)l.Pattern.Value).IsMatch(text) != l.Negated;
                }

            case BetweenTest b:
                {
                    object? x = Value(b.Operand, context, collation);
                    bool? inRange = And(
                        Ordered(x, Value(b.Low, context, collation), k => k >= 0),
                        Ordered(x, Value(b.High, context, collation), k => k <= 0));
                    return b.Negated ? !inRange : inRange;
                }

            case Junction j:
                {
                    bool? result = j.IsAnd;
                    foreach (SpecCondition operand in j.Operands.Where(o => !o.IsExcluded))
                    {
                        bool? value = Test(operand, context, collation);
                        result = j.IsAnd ? And(result, value) : Or(result, value);
                    }

                    return result;
                }

            case Negation n:
                return !Test(n.Operand, context, collation);
            case ConditionIfTest { Include: true } t:
                return Test(t.Operand, context, collation);
            default:
                throw new InvalidOperationException(condition.GetType().Name);
        }
    }

    private static bool? In(object? value, IEnumerable<object?> candidates, Collation collation)
    {
        bool? any = false;
        foreach (object? candidate in candidates)
        {
            any = Or(any, Equal(value, candidate, collation));
        }

        return any;
    }

    private static bool? Ordered(object? a, object? b, Func<int, bool> holds) =>
        a is long x && b is long y ? holds(x.CompareTo(y)) : null;

    private static bool? Equal(object? a, object? b, Collation collation) => (a, b) switch
    {
        (null, _) or (_, null) => null,
        (string x, string y) => string.Equals(x, y, collation.Equality),
        (long x, long y) => x == y,
        _ => throw new InvalidOperationException("text compared with a number"),
    };

    private static bool? And(bool? a, bool? b) =>
        a == false || b == false ? false : a == true && b == true ? true : null;

    private static bool? Or(bool? a, bool? b) =>
        a == true || b == true ? true : a == false && b == false ? false : null;

    /// <summary>Each engine's default text equality, LIKE case rule and NULL sort
    /// position.</summary>
    private sealed class Collation(Dbms dbms)
    {
        // MySQL's utf8mb4_0900_ai_ci and SQL Server's default database collation.
        public StringComparison Equality { get; } = dbms is Dbms.MySql or Dbms.SqlServer
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        // SQLite's LIKE folds ASCII case as well.
        private readonly bool _likeIgnoresCase =
            dbms is Dbms.MySql or Dbms.SqlServer or Dbms.Sqlite;

        public bool NullsSortHigh { get; } = dbms is Dbms.PostgreSql or Dbms.Oracle;

        public Regex Like(string pattern)
        {
            string body = string.Concat(pattern.Select(ch => ch switch
            {
                '%' => ".*",
                '_' => ".",
                _ => Regex.Escape(ch.ToString()),
            }));
            return new Regex(
                "^" + body + "$",
                RegexOptions.Singleline | (_likeIgnoresCase ? RegexOptions.IgnoreCase : 0));
        }
    }
}
