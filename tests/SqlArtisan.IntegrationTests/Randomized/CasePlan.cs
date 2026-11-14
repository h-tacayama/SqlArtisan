using CsCheck;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Randomized;

/// <summary>
/// Every statement <see cref="CaseChecker"/> runs for one generated query, and its negated
/// WHERE, planned without a connection so the DB-less Build gate builds all a lane runs.
/// </summary>
internal sealed record CasePlan(
    QuerySpec Spec,
    IReadOnlyList<(string Name, QuerySpec Variant)> Rewrites,
    QuerySpec[]? PartitionParts,
    QuerySpec? Unlimited,
    PlannedStatement? Wrapped,
    (QuerySpec Left, QuerySpec Right, QuerySpec Compound)? SetOperatorParts,
    QuerySpec? NegatedWhere)
{
    public IEnumerable<PlannedStatement> Statements()
    {
        yield return PlannedStatement.Of("BASE", Spec);
        foreach ((string name, QuerySpec variant) in Rewrites)
        {
            yield return PlannedStatement.Of(name, variant);
        }

        QuerySpec[] partition = PartitionParts ?? [];
        for (int i = 0; i < partition.Length; i++)
        {
            yield return PlannedStatement.Of($"M6 PART{i}", partition[i]);
        }

        if (Unlimited is not null)
        {
            yield return PlannedStatement.Of("M10 FULL", Unlimited);
        }

        if (Wrapped is not null)
        {
            yield return Wrapped;
        }

        if (SetOperatorParts is { } parts)
        {
            yield return PlannedStatement.Of("M13 LEFT", parts.Left);
            yield return PlannedStatement.Of("M13 RIGHT", parts.Right);
            yield return PlannedStatement.Of("M13 COMPOUND", parts.Compound);
        }

        if (NegatedWhere is not null)
        {
            yield return PlannedStatement.Of("NEGATED", NegatedWhere);
        }
    }
}

/// <summary>Derives a generated query's rewrites and relation parts, seeded by the query's own
/// text so a lane and the Build gate derive the same ones.</summary>
internal sealed class CasePlanner
{
    private readonly CaseRow _row;

    public CasePlanner(CaseRow row)
    {
        _row = row;
    }

    private Dbms Dbms => _row.Dbms;

    // The rewrites draw from one Random in list order, then the partition draws, so this order
    // fixes every variant a seed produces.
    public CasePlan Plan(QuerySpec spec)
    {
        Random random = new(StableHash(spec.ToCSharp()));
        List<(string, QuerySpec)> rewrites = [];
        foreach ((string name, Func<QuerySpec, Random, QuerySpec?> rewrite) in Rewrites())
        {
            if (rewrite(spec, random) is { } variant)
            {
                rewrites.Add((name, variant));
            }
        }

        QuerySpec[]? partition = PartitionParts(spec, random);
        return new CasePlan(
            spec,
            rewrites,
            partition,
            spec.Query.Limit is null ? null : spec.With(spec.Query with { Limit = null }),
            Wrapped(spec),
            SetOperatorParts(spec),
            NegateWhere(spec));
    }

    public static QuerySpec? NegateWhere(QuerySpec spec) => spec.Query.Where is null
        ? null
        : spec.With(spec.Query with { Where = new Negation(spec.Query.Where) });

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
    private QuerySpec[]? PartitionParts(QuerySpec spec, Random random)
    {
        QueryBlock q = spec.Query;
        if (q.GroupBy.Count > 0 || q.IsDistinct || q.Limit is not null || q.Tail.Count > 0
            || q.Items.Any(e => e.HasAggregate))
        {
            return null;
        }

        SpecCondition p = PlainCondition(spec, random);
        QueryBlock Part(SpecCondition c) => q with
        {
            OrderBy = [],
            Where = q.Where is null ? c : new Junction(true, [q.Where, c]),
        };

        return
        [
            spec.With(Part(p)),
            spec.With(Part(new Negation(p))),
            spec.With(Part(new Junction(false, [p, new Negation(p)]))),
            spec.With(Part(p) with
            {
                Tail = [new SetBranch(SetOperatorLevel.UnionAll, Part(new Negation(p)))],
            }),
        ];
    }

    // M15: wrapping the query in a derived table or a CTE keeps its rows.
    private PlannedStatement? Wrapped(QuerySpec spec)
    {
        if (_row.Wrap == WrapLevel.None)
        {
            return null;
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
        WrapLevel wrap = _row.Wrap;
        return new PlannedStatement($"M15 {wrap}", spec.ToCSharp() + "\n" + csharp, () =>
        {
            ISubquery subquery = (ISubquery)inner.BuildStatement(
                new BuildContext(spec.Unaliased),
                out ExpressionAlias[] aliases);
            if (wrap == WrapLevel.DerivedTable)
            {
                SubqueryDerivedTable t = subquery.AsTable("t");
                return Select([.. aliases.Select(a => (object)t.Column(a))]).From(t);
            }

            Cte w = new("w");
            return With(w.As(subquery))
                .Select([.. aliases.Select(a => (object)w.Column(a))])
                .From(w);
        });
    }

    // M13: the set operator over two branches returns what its definition computes from the
    // branches' own rows.
    private static (QuerySpec, QuerySpec, QuerySpec)? SetOperatorParts(QuerySpec spec)
    {
        if (spec.Query.Tail is not [SetBranch branch])
        {
            return null;
        }

        QueryBlock compound = spec.Query with { OrderBy = [], Limit = null };
        return (
            spec.With(compound with { Tail = [] }),
            spec.With(branch.Query with { AliasesItems = true }),
            spec.With(compound));
    }

    private SpecCondition PlainCondition(QuerySpec spec, Random random) =>
        QueryGenerators.PlainCondition(Dbms, spec.Query.Tables)
            .Generate(new PCG((uint)random.Next(), (ulong)random.Next()), null!, out _);

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
