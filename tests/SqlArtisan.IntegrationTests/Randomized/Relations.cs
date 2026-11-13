using System.Data;
using System.Data.Common;
using System.Text;
using CsCheck;
using Dapper;
using SqlArtisan.Dapper;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Randomized;

/// <summary>A statement's run: its rows, or why it never produced them.</summary>
internal sealed record Outcome(
    string CSharp,
    string Sql,
    string Parameters,
    List<string>? Rows,
    string? Error)
{
    public string Show(string label) => $"{label}\n  C#: {CSharp.Replace("\n", "\n      ")}\n"
        + $"  SQL: {Sql}\n  PARAMS: {Parameters}\n"
        + (Error is not null
            ? $"  ERROR: {Error}\n"
            : $"  ROWS({Rows!.Count}): {string.Join(" ; ", Rows.Take(20))}\n");
}

internal sealed class RandomizedCaseException(string message) : Exception(message);

/// <summary>
/// Runs one generated query and the relations that must hold between it and its rewrites. A
/// rewrite compares on the same engine only, so engine-defined behaviour both sides share
/// cancels out.
/// </summary>
internal sealed class CaseChecker
{
    private readonly IDbConnection _connection;
    private readonly CaseRow _row;

    public CaseChecker(IDbConnection connection, CaseRow row)
    {
        _connection = connection;
        _row = row;
    }

    private Dbms Dbms => _row.Dbms;

    /// <summary>Throws <see cref="RandomizedCaseException"/> naming every relation that does
    /// not hold.</summary>
    public void Check(QuerySpec spec)
    {
        List<string> failures = [];
        Outcome baseline = Run(spec);
        if (baseline.Error is not null)
        {
            throw new RandomizedCaseException(
                "the generated query failed\n" + baseline.Show("BASE"));
        }

        Random random = new(StableHash(spec.ToCSharp()));
        bool ordered = spec.Query.OrderBy.Count > 0;
        foreach ((string name, Func<QuerySpec, Random, QuerySpec?> rewrite) in Rewrites())
        {
            QuerySpec? variant = rewrite(spec, random);
            if (variant is null)
            {
                continue;
            }

            Outcome result = Run(variant);
            bool inOrder = ordered && variant.Query.OrderBy.Count > 0 && !name.StartsWith("M9");
            if (result.Error is not null || !Same(baseline.Rows!, result.Rows!, inOrder))
            {
                failures.Add($"{name} does not hold\n" + baseline.Show("BASE")
                    + result.Show("VARIANT"));
            }
        }

        CheckPartition(spec, random, failures);
        CheckSlice(spec, baseline, failures);
        CheckWrap(spec, baseline, failures);
        CheckSetOperator(spec, failures);
        CheckReference(spec, baseline, failures);
        if (failures.Count > 0)
        {
            throw new RandomizedCaseException(string.Join("\n", failures));
        }
    }

    /// <summary>The negative control: <c>Where(c)</c> against <c>Where(Not(c))</c>, which the
    /// comparison must tell apart unless both happen to return the same rows.</summary>
    public bool? NegatedWhereDiffers(QuerySpec spec)
    {
        if (spec.Query.Where is null)
        {
            return null;
        }

        Outcome kept = Run(spec);
        Outcome negated =
            Run(spec.With(spec.Query with { Where = new Negation(spec.Query.Where) }));
        if (kept.Error is not null || negated.Error is not null)
        {
            throw new RandomizedCaseException(kept.Show("KEPT") + negated.Show("NEGATED"));
        }

        return !Same(kept.Rows!, negated.Rows!, inOrder: false);
    }

    public Outcome Run(QuerySpec spec) => Run(spec.ToCSharp(), () =>
        spec.Query.BuildStatement(new BuildContext(spec.Unaliased), out _));

    private Outcome Run(string csharp, Func<ISqlBuilder> build)
    {
        SqlStatement statement;
        try
        {
            statement = build().Build(Dbms);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException
            or NotSupportedException)
        {
            return new Outcome(csharp, "", "", null, $"Build: {ex.GetType().Name}: {ex.Message}");
        }

        StringBuilder parameters = new();
        statement.Parameters.ForEach((name, bind) =>
            parameters.Append($"{name}={ReferenceEvaluator.Normalize(bind.Value)} "));
        try
        {
            using IDataReader reader = _connection.ExecuteReader(
                statement.Text,
                statement.Parameters.ToDynamicParameters(),
                commandTimeout: 60);
            List<string> rows = [];
            while (reader.Read())
            {
                string[] cells = new string[reader.FieldCount];
                for (int i = 0; i < cells.Length; i++)
                {
                    cells[i] = ReferenceEvaluator.Normalize(
                        reader.IsDBNull(i) ? null : reader.GetValue(i));
                }

                rows.Add(string.Join("|", cells));
            }

            return new Outcome(csharp, statement.Text, parameters.ToString(), rows, null);
        }
        catch (DbException ex)
        {
            return new Outcome(
                csharp,
                statement.Text,
                parameters.ToString(),
                null,
                ex.Message.Split('\n')[0].Trim());
        }
    }

    private List<(string, Func<QuerySpec, Random, QuerySpec?>)> Rewrites() =>
    [
        ("M1 ConditionIf(false) beside a junction's operands", InsertExcludedOperand),
        ("M1b c -> c &/| ConditionIf(false, x)", WrapExcludedOperand),
        ("M1t ConditionIf(true, c) == c", IncludeCondition),
        ("M2 swapped And/Or operands", SwapOperands),
        ("M3 toggled table aliases", (s, _) => ToggleAliases(s)),
        ("M4 Not(Not(c)) == c", DoubleNegate),
        ("M5 literal -> Bind(literal)", (s, _) => BindLiterals(s)),
        ("M7 inner-join WHERE moved into ON", (s, _) => WhereIntoOn(s)),
        ("M8 desugared predicates", (s, _) => Desugar(s)),
        ("M9 Distinct == GroupBy(every item)", (s, _) => DistinctToGroupBy(s)),
        ("M11 In(subquery) == Exists(correlated)", (s, _) => InToExists(s)),
        ("M12 Where(c) == Where(Case(When(c).Then(1), Else(0)) == 1)", (s, _) => CaseLift(s)),
    ];

    private QuerySpec? InsertExcludedOperand(QuerySpec spec, Random random)
    {
        int count = spec.Query.AllConditions().Count(c => c is Junction);
        if (count == 0)
        {
            return null;
        }

        SpecCondition excluded = new ConditionIfTest(false, PlainCondition(spec, random));
        int target = random.Next(count);
        int index = 0;
        return spec.With(spec.Query.Rewrite(SpecRewrite.Conditions(c =>
        {
            if (c is not Junction j || index++ != target)
            {
                return c;
            }

            List<SpecCondition> operands = [.. j.Operands];
            operands.Insert(random.Next(operands.Count + 1), excluded);
            return j with { Operands = [.. operands] };
        })));
    }

    private QuerySpec? WrapExcludedOperand(QuerySpec spec, Random random)
    {
        if (spec.Query.Where is not { } where)
        {
            return null;
        }

        SpecCondition[] operands =
            [where, new ConditionIfTest(false, PlainCondition(spec, random))];
        if (random.Next(2) == 0)
        {
            System.Array.Reverse(operands);
        }

        return spec.With(spec.Query with { Where = new Junction(random.Next(2) == 0, operands) });
    }

    private static QuerySpec? IncludeCondition(QuerySpec spec, Random random) =>
        RewriteNthCondition(spec, random, c => new ConditionIfTest(true, c));

    private static QuerySpec? DoubleNegate(QuerySpec spec, Random random) =>
        RewriteNthCondition(spec, random, c => new Negation(new Negation(c)));

    private static QuerySpec? RewriteNthCondition(
        QuerySpec spec,
        Random random,
        Func<SpecCondition, SpecCondition> rewrite)
    {
        int count = spec.Query.AllConditions().Count;
        if (count == 0)
        {
            return null;
        }

        int target = random.Next(count);
        int index = 0;
        return spec.With(spec.Query.Rewrite(SpecRewrite.Conditions(c =>
            index++ == target ? rewrite(c) : c)));
    }

    private static QuerySpec? SwapOperands(QuerySpec spec, Random random)
    {
        int count = spec.Query.AllConditions().Count(c => c is Junction);
        if (count == 0)
        {
            return null;
        }

        int target = random.Next(count);
        int index = 0;
        return spec.With(spec.Query.Rewrite(SpecRewrite.Conditions(c =>
            c is Junction j && index++ == target
                ? j with { Operands = [.. j.Operands.OrderBy(_ => random.Next())] }
                : c)));
    }

    // Unaliasing the outer table aliases every inner one, so an inner table's name cannot
    // capture an outer reference; Build() rejects a correlated one, and a join's two tables
    // would share bare column names.
    private static QuerySpec? ToggleAliases(QuerySpec spec)
    {
        bool outerUnaliased = spec.Query.Tables.Any(spec.Unaliased.Contains);
        if (spec.Query.Join is not null || (!outerUnaliased && Correlates(spec.Query)))
        {
            return null;
        }

        HashSet<string> unaliased = outerUnaliased ? [] : [.. spec.Query.Tables];
        return spec with { Unaliased = unaliased };
    }

    private static bool Correlates(QueryBlock query)
    {
        bool found = false;
        void Visit(QueryBlock sub) => sub.Rewrite(SpecRewrite.Expressions(e =>
        {
            found |= e is ColumnRef { Table.Length: 1 };
            return e;
        }));

        query.Rewrite(new SpecRewrite(
            e =>
            {
                if (e is ScalarSubquery s)
                {
                    Visit(s.Query);
                }

                return e;
            },
            c =>
            {
                if (c is InSubquery i)
                {
                    Visit(i.Query);
                }
                else if (c is ExistsTest x)
                {
                    Visit(x.Query);
                }

                return c;
            }));
        return found;
    }

    private static QuerySpec BindLiterals(QuerySpec spec) => spec.With(spec.Query.Rewrite(
        SpecRewrite.Expressions(e => e is Literal l ? l with { IsBound = true } : e)));

    private static QuerySpec? WhereIntoOn(QuerySpec spec)
    {
        if (spec.Query is not { Join: { IsInner: true } join, Where: { } where })
        {
            return null;
        }

        return spec.With(spec.Query with
        {
            Join = join with { On = new Junction(true, [join.On, where]) },
            Where = null,
        });
    }

    private static QuerySpec? Desugar(QuerySpec spec)
    {
        bool changed = false;
        QueryBlock rewritten = spec.Query.Rewrite(SpecRewrite.Conditions(c =>
        {
            SpecCondition? plain = c switch
            {
                BetweenTest b => Negate(
                    new Junction(true, [new Comparison(">=", b.Operand, b.Low),
                        new Comparison("<=", b.Operand, b.High)]),
                    b.Negated),
                InList { Values.Length: 1 } i =>
                    new Comparison(i.Negated ? "!=" : "==", i.Operand, i.Values[0]),
                InList i => new Junction(
                    i.Negated,
                    [.. i.Values.Select(v => (SpecCondition)new Comparison(
                        i.Negated ? "!=" : "==",
                        i.Operand,
                        v))]),
                Comparison { Operator: "!=" } c2 => new Negation(c2 with { Operator = "==" }),
                LikeTest { Negated: true } l => new Negation(l with { Negated = false }),
                NullTest { Negated: true } n => new Negation(n with { Negated = false }),
                ExistsTest { Negated: true } e => new Negation(e with { Negated = false }),
                InSubquery { Negated: true } s => new Negation(s with { Negated = false }),
                _ => null,
            };
            changed |= plain is not null;
            return plain ?? c;
        }));
        return changed ? spec.With(rewritten) : null;
    }

    private static SpecCondition Negate(SpecCondition condition, bool negated) =>
        negated ? new Negation(condition) : condition;

    private static QuerySpec? DistinctToGroupBy(QuerySpec spec)
    {
        QueryBlock q = spec.Query;
        if (!q.IsDistinct || q.GroupBy.Count > 0 || q.Items.Any(e => e.HasAggregate || HasValue(e)))
        {
            return null;
        }

        return spec.With(q with { IsDistinct = false, GroupBy = q.Items });
    }

    // A grouped select item must repeat its GROUP BY key exactly, which a fresh bind never does.
    private static bool HasValue(SpecExpression expression)
    {
        bool found = false;
        expression.Rewrite(SpecRewrite.Expressions(e =>
        {
            found |= e is Literal or ScalarSubquery or CaseExpression;
            return e;
        }));
        return found;
    }

    private static QuerySpec? InToExists(QuerySpec spec)
    {
        QueryBlock q = spec.Query;
        if (q.Where is null || q.Tables.Any(spec.Unaliased.Contains))
        {
            return null;
        }

        List<SpecCondition> conjuncts = q.Where is Junction { IsAnd: true } j
            ? [.. j.Operands]
            : [q.Where];
        int index = conjuncts.FindIndex(c => c is InSubquery
        {
            Negated: false,
            Operand: ColumnRef,
            Query: { Items: [ColumnRef], Tail.Count: 0, Limit: null, GroupBy.Count: 0 },
        });
        if (index < 0)
        {
            return null;
        }

        InSubquery inSubquery = (InSubquery)conjuncts[index];
        QueryBlock sub = inSubquery.Query;
        if (spec.Unaliased.Contains(sub.From))
        {
            return null;
        }

        SpecCondition link = new Comparison("==", sub.Items[0], inSubquery.Operand);
        conjuncts[index] = new ExistsTest(
            sub with { Where = sub.Where is null ? link : new Junction(true, [sub.Where, link]) },
            false);
        return spec.With(q with
        {
            Where = conjuncts.Count == 1 ? conjuncts[0] : new Junction(true, [.. conjuncts]),
        });
    }

    private static QuerySpec? CaseLift(QuerySpec spec) => spec.Query.Where is not { } where
        ? null
        : spec.With(spec.Query with
        {
            Where = new Comparison(
                "==",
                new CaseExpression(where, new Literal(1), new Literal(0)),
                new Literal(1)),
        });

    // M6: the rows where p holds and those where Not(p) holds partition those where
    // p | Not(p) holds, which also equals the two run as one UNION ALL.
    private void CheckPartition(QuerySpec spec, Random random, List<string> failures)
    {
        QueryBlock q = spec.Query;
        if (q.GroupBy.Count > 0 || q.IsDistinct || q.Limit is not null || q.Tail.Count > 0
            || q.Items.Any(e => e.HasAggregate))
        {
            return;
        }

        SpecCondition p = PlainCondition(spec, random);
        QueryBlock Part(SpecCondition c) => q with
        {
            OrderBy = [],
            Where = q.Where is null ? c : new Junction(true, [q.Where, c]),
        };

        QuerySpec[] parts =
        [
            spec.With(Part(p)),
            spec.With(Part(new Negation(p))),
            spec.With(Part(new Junction(false, [p, new Negation(p)]))),
            spec.With(Part(p) with
            {
                Tail = [new SetBranch(SetOperatorLevel.UnionAll, Part(new Negation(p)))],
            }),
        ];
        Outcome[] results = [.. parts.Select(Run)];
        if (results.Any(r => r.Error is not null))
        {
            failures.Add("M6 partition part failed\n"
                + string.Concat(results.Select((r, i) => r.Show($"PART{i}"))));
            return;
        }

        List<string> union = [.. results[0].Rows!, .. results[1].Rows!];
        if (!Same(union, results[2].Rows!, false) || !Same(union, results[3].Rows!, false))
        {
            failures.Add("M6 partition does not hold\n"
                + string.Concat(results.Select((r, i) => r.Show($"PART{i}"))));
        }
    }

    // M10: a row limit returns a slice of the same query's ordered rows.
    private void CheckSlice(QuerySpec spec, Outcome baseline, List<string> failures)
    {
        if (spec.Query.Limit is not { } limit)
        {
            return;
        }

        Outcome full = Run(spec.With(spec.Query with { Limit = null }));
        List<string>? expected = full.Rows?.Skip(limit.Offset).Take(limit.Count).ToList();
        if (expected is null || !expected.SequenceEqual(baseline.Rows!))
        {
            failures.Add($"M10 row limit is not a slice (expected rows "
                + $"{limit.Offset + 1}..{limit.Offset + limit.Count} of FULL)\n"
                + baseline.Show("BASE") + full.Show("FULL"));
        }
    }

    // M15: wrapping the query in a derived table or a CTE keeps its rows.
    private void CheckWrap(QuerySpec spec, Outcome baseline, List<string> failures)
    {
        if (_row.Wrap == WrapLevel.None)
        {
            return;
        }

        // An ORDER BY without a row limit has no place in a derived table on SQL Server.
        QueryBlock inner = spec.Query.Limit is null ? spec.Query with { OrderBy = [] } : spec.Query;
        string body = inner.ToCSharp();
        string columns = string.Join(
            ", ",
            Enumerable.Range(0, inner.Items.Count).Select(i => $"{{0}}.Column(c{i})"));
        string csharp = _row.Wrap == WrapLevel.DerivedTable
            ? $"SubqueryDerivedTable t = ({body}).AsTable(\"t\");\n"
                + $"Select({string.Format(columns, "t")}).From(t)"
            : $"Cte w = new(\"w\");\n"
                + $"With(w.As({body})).Select({string.Format(columns, "w")}).From(w)";
        Outcome wrapped = Run(spec.ToCSharp() + "\n" + csharp, () =>
        {
            ISubquery subquery = (ISubquery)inner.BuildStatement(
                new BuildContext(spec.Unaliased),
                out ExpressionAlias[] aliases);
            if (_row.Wrap == WrapLevel.DerivedTable)
            {
                SubqueryDerivedTable t = subquery.AsTable("t");
                return Select([.. aliases.Select(a => (object)t.Column(a))]).From(t);
            }

            Cte w = new("w");
            return With(w.As(subquery))
                .Select([.. aliases.Select(a => (object)w.Column(a))])
                .From(w);
        });
        if (wrapped.Error is not null || !Same(baseline.Rows!, wrapped.Rows!, false))
        {
            failures.Add($"M15 {_row.Wrap} wrap does not keep the rows\n"
                + baseline.Show("BASE") + wrapped.Show("WRAPPED"));
        }
    }

    // M13: the set operator over two branches returns what its definition computes from the
    // branches' own rows.
    private void CheckSetOperator(QuerySpec spec, List<string> failures)
    {
        if (spec.Query.Tail is not [SetBranch branch])
        {
            return;
        }

        QueryBlock compound = spec.Query with { OrderBy = [], Limit = null };
        Outcome left = Run(spec.With(compound with { Tail = [] }));
        Outcome right = Run(spec.With(branch.Query with { AliasesItems = true }));
        Outcome actual = Run(spec.With(compound));
        if (left.Error is not null || right.Error is not null || actual.Error is not null)
        {
            failures.Add("M13 set-operator part failed\n" + left.Show("LEFT")
                + right.Show("RIGHT") + actual.Show("COMPOUND"));
            return;
        }

        List<string> expected = Apply(branch.Operator, left.Rows!, right.Rows!);
        if (!Same(expected, actual.Rows!, false))
        {
            failures.Add($"M13 {branch.Operator} does not match its branches\n"
                + $"EXPECTED({expected.Count}): {string.Join(" ; ", expected.Take(20))}\n"
                + left.Show("LEFT") + right.Show("RIGHT") + actual.Show("COMPOUND"));
        }
    }

    private static List<string> Apply(SetOperatorLevel op, List<string> a, List<string> b) =>
        op switch
        {
            SetOperatorLevel.Union => [.. a.Concat(b).Distinct()],
            SetOperatorLevel.UnionAll => [.. a, .. b],
            SetOperatorLevel.Intersect => [.. a.Distinct().Where(b.Contains)],
            SetOperatorLevel.Except => [.. a.Distinct().Where(x => !b.Contains(x))],
            _ => throw new InvalidOperationException(op.ToString()),
        };

    // M14: the query returns the rows the C# reference computes, where it models them.
    private void CheckReference(QuerySpec spec, Outcome baseline, List<string> failures)
    {
        List<string> expected;
        bool ordered;
        try
        {
            (expected, ordered) = ReferenceEvaluator.Evaluate(spec.Query, Dbms);
        }
        catch (ReferenceUnavailableException)
        {
            return;
        }

        if (!Same(expected, baseline.Rows!, ordered))
        {
            failures.Add($"M14 rows differ from the reference evaluation\n"
                + $"EXPECTED({expected.Count}): {string.Join(" ; ", expected.Take(20))}\n"
                + baseline.Show("ACTUAL"));
        }
    }

    private SpecCondition PlainCondition(QuerySpec spec, Random random) =>
        QueryGenerators.PlainCondition(Dbms, spec.Query.Tables)
            .Generate(new PCG((uint)random.Next(), (ulong)random.Next()), null!, out _);

    private static bool Same(List<string> a, List<string> b, bool inOrder) => inOrder
        ? a.SequenceEqual(b)
        : a.Order(StringComparer.Ordinal).SequenceEqual(b.Order(StringComparer.Ordinal));

    private static int StableHash(string text)
    {
        unchecked
        {
            int hash = (int)2166136261;
            foreach (char ch in text)
            {
                hash = (hash ^ ch) * 16777619;
            }

            return hash;
        }
    }
}
