using System.Data;
using CsCheck;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Randomized;

/// <summary>Every statement <see cref="DmlChecker"/> runs for one generated DML statement,
/// planned without a connection so the DB-less Build gate builds all a lane runs.</summary>
internal sealed record DmlPlan(
    DmlSpec Spec,
    IReadOnlyList<(string Name, DmlSpec Variant)> Rewrites,
    DmlSpec? Negated)
{
    public IEnumerable<PlannedStatement> Statements()
    {
        yield return Spec.Planned("BASE");
        foreach ((string name, DmlSpec variant) in Rewrites)
        {
            yield return variant.Planned(name);
        }

        if (Spec.PlannedMatch("MATCH") is { } match)
        {
            yield return match;
        }

        if (Negated is not null)
        {
            yield return Negated.Planned("NEGATED");
        }
    }
}

/// <summary>Derives a DML statement's rewrites, seeded by its own text so a lane and the Build
/// gate derive the same ones.</summary>
internal static class DmlPlanner
{
    public static DmlPlan Plan(DmlSpec spec)
    {
        Random random = new(CasePlanner.StableHash(spec.ToCSharp()));
        List<(string, DmlSpec)> rewrites = [];
        foreach ((string name, Func<DmlSpec, Random, DmlSpec?> rewrite) in Rewrites())
        {
            if (rewrite(spec, random) is { } variant)
            {
                rewrites.Add((name, variant));
            }
        }

        return new DmlPlan(spec, rewrites, NegateWhere(spec));
    }

    /// <summary>The statement with its filter negated: an UPDATE's or DELETE's WHERE (not
    /// Oracle's pin), or the WHERE of an INSERT's feeding SELECT.</summary>
    public static DmlSpec? NegateWhere(DmlSpec spec) => spec.Kind == DmlKind.Insert
        ? spec.Insert!.Feed is { Where: { } feedWhere } feed
            ? spec with
            {
                Insert = spec.Insert with { Feed = feed with { Where = new Negation(feedWhere) } },
            }
            : null
        : spec.Where is null ? null : spec with { Where = new Negation(spec.Where) };

    private static List<(string, Func<DmlSpec, Random, DmlSpec?>)> Rewrites() =>
    [
        ("M1 ConditionIf(false) beside a junction's operands", InsertExcludedOperand),
        ("M1b c -> c &/| ConditionIf(false, x)", WrapExcludedOperand),
        ("M1t ConditionIf(true, c) == c",
            (s, r) => RewriteNth(s, r, c => new ConditionIfTest(true, c))),
        ("M2 swapped And/Or operands", SwapOperands),
        ("M3 toggled target alias", (s, _) => ToggleTargetAlias(s)),
        ("M4 Not(Not(c)) == c", (s, r) => RewriteNth(s, r, c => new Negation(new Negation(c)))),
        ("M5 literal -> Bind(literal)", (s, _) => BindLiterals(s)),
        ("M8 desugared predicates", (s, _) => Desugar(s)),
        ("M11 In(subquery) == Exists(correlated)", (s, _) => InToExists(s)),
    ];

    private static IReadOnlyList<string> FilterTables(DmlSpec spec) => spec.Kind == DmlKind.Insert
        ? [spec.Insert!.Feed!.From]
        : spec.Join is null ? ["u"] : ["u", "d"];

    private static SpecCondition PlainCondition(DmlSpec spec, Random random) =>
        QueryGenerators.PlainCondition(spec.Dbms, FilterTables(spec))
            .Generate(new PCG((uint)random.Next(), (ulong)random.Next()), null!, out _);

    private static DmlSpec? InsertExcludedOperand(DmlSpec spec, Random random)
    {
        int count = spec.AllConditions().Count(c => c is Junction);
        if (count == 0 || (spec.Kind == DmlKind.Insert && spec.Insert!.Feed is null))
        {
            return null;
        }

        SpecCondition excluded = new ConditionIfTest(false, PlainCondition(spec, random));
        int target = random.Next(count);
        int index = 0;
        return spec.Rewrite(SpecRewrite.Conditions(c =>
        {
            if (c is not Junction j || index++ != target)
            {
                return c;
            }

            List<SpecCondition> operands = [.. j.Operands];
            operands.Insert(random.Next(operands.Count + 1), excluded);
            return j with { Operands = [.. operands] };
        }));
    }

    private static DmlSpec? WrapExcludedOperand(DmlSpec spec, Random random)
    {
        SpecCondition? where = spec.Kind == DmlKind.Insert ? spec.Insert!.Feed?.Where : spec.Where;
        if (where is null)
        {
            return null;
        }

        SpecCondition[] operands =
            [where, new ConditionIfTest(false, PlainCondition(spec, random))];
        if (random.Next(2) == 0)
        {
            System.Array.Reverse(operands);
        }

        SpecCondition wrapped = new Junction(random.Next(2) == 0, operands);
        return spec.Kind == DmlKind.Insert
            ? spec with
            {
                Insert = spec.Insert! with { Feed = spec.Insert.Feed! with { Where = wrapped } },
            }
            : spec with { Where = wrapped };
    }

    private static DmlSpec? RewriteNth(
        DmlSpec spec,
        Random random,
        Func<SpecCondition, SpecCondition> rewrite)
    {
        int count = spec.AllConditions().Count;
        if (count == 0)
        {
            return null;
        }

        int target = random.Next(count);
        int index = 0;
        return spec.Rewrite(SpecRewrite.Conditions(c => index++ == target ? rewrite(c) : c));
    }

    private static DmlSpec? SwapOperands(DmlSpec spec, Random random)
    {
        int count = spec.AllConditions().Count(c => c is Junction);
        if (count == 0)
        {
            return null;
        }

        int target = random.Next(count);
        int index = 0;
        return spec.Rewrite(SpecRewrite.Conditions(c =>
            c is Junction j && index++ == target
                ? j with { Operands = [.. j.Operands.OrderBy(_ => random.Next())] }
                : c));
    }

    // Only where both spellings build: an INSERT target off MySQL and SQL Server, an UPDATE's
    // or DELETE's off SQL Server and un-joined, and no subquery correlated to it.
    private static DmlSpec? ToggleTargetAlias(DmlSpec spec)
    {
        bool allowed = spec.Kind == DmlKind.Insert
            ? spec.Dbms is not (Dbms.MySql or Dbms.SqlServer)
            : spec.Dbms != Dbms.SqlServer && spec.Join is null;
        if (!allowed || ReadsTargetInSubquery(spec))
        {
            return null;
        }

        // Unaliasing the target aliases every inner block, as QuerySpec's M3 does.
        return spec with
        {
            Unaliased = spec.TargetAliased ? new HashSet<string> { "u" } : new HashSet<string>(),
        };
    }

    private static bool ReadsTargetInSubquery(DmlSpec spec)
    {
        bool found = false;
        void Visit(QueryBlock sub) => sub.Rewrite(SpecRewrite.Expressions(e =>
        {
            found |= e is ColumnRef { Table: "u" };
            return e;
        }));

        spec.Rewrite(new SpecRewrite(
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

    private static DmlSpec BindLiterals(DmlSpec spec) => spec.Rewrite(
        SpecRewrite.Expressions(e => e is Literal l ? l with { IsBound = true } : e));

    private static DmlSpec? Desugar(DmlSpec spec)
    {
        bool changed = false;
        DmlSpec rewritten = spec.Rewrite(SpecRewrite.Conditions(c =>
        {
            SpecCondition? plain = CasePlanner.Desugared(c);
            changed |= plain is not null;
            return plain ?? c;
        }));
        return changed ? rewritten : null;
    }

    // The EXISTS correlates to the IN operand's table, so that table must be aliased, as the
    // subquery's must be for the link to bind inside it.
    private static DmlSpec? InToExists(DmlSpec spec)
    {
        SpecCondition? where = spec.Kind == DmlKind.Insert ? spec.Insert!.Feed?.Where : spec.Where;
        if (where is null)
        {
            return null;
        }

        List<SpecCondition> conjuncts =
            where is Junction { IsAnd: true } j ? [.. j.Operands] : [where];
        int index = conjuncts.FindIndex(c => c is InSubquery
        {
            Negated: false,
            Operand: ColumnRef operand,
            Query: { Items: [ColumnRef], Tail.Count: 0, Limit: null, GroupBy.Count: 0 } sub,
        } && !spec.Unaliased.Contains(operand.Table) && !spec.Unaliased.Contains(sub.From));
        if (index < 0)
        {
            return null;
        }

        InSubquery inSubquery = (InSubquery)conjuncts[index];
        QueryBlock query = inSubquery.Query;
        SpecCondition link = new Comparison("==", query.Items[0], inSubquery.Operand);
        conjuncts[index] = new ExistsTest(
            query with
            {
                Where = query.Where is null ? link : new Junction(true, [query.Where, link]),
            },
            false);
        SpecCondition rewritten =
            conjuncts.Count == 1 ? conjuncts[0] : new Junction(true, [.. conjuncts]);
        return spec.Kind == DmlKind.Insert
            ? spec with
            {
                Insert = spec.Insert! with { Feed = spec.Insert.Feed! with { Where = rewritten } },
            }
            : spec with { Where = rewritten };
    }
}

/// <summary>One DML statement's run inside its own rolled-back transaction: what it returned,
/// and both tables as it left them, read before the rollback.</summary>
internal sealed record DmlOutcome(Outcome Statement, List<string>? Users, List<string>? Depts)
{
    public bool ChangedRows => Users is not null
        && (!Same(Users, DmlReference.SeedUsers) || !Same(Depts!, DmlReference.SeedDepts));

    public static bool Same(IEnumerable<string> a, IEnumerable<string> b) =>
        a.Order(StringComparer.Ordinal).SequenceEqual(b.Order(StringComparer.Ordinal));

    public string Show(string label) => Statement.Show(label)
        + (Users is null
            ? ""
            : $"  USERS({Users.Count}), rows not in the seed: "
                + string.Join(" ; ", Users.Except(DmlReference.SeedUsers)) + "\n"
                + $"  DEPTS({Depts!.Count}), rows not in the seed: "
                + string.Join(" ; ", Depts.Except(DmlReference.SeedDepts)) + "\n");
}

/// <summary>
/// Runs one generated DML statement and the relations that must hold between it, its rewrites,
/// the SELECT of the rows it reads, its output and the state it leaves. Each statement runs in
/// a transaction of its own that is rolled back, so every one starts from the seed.
/// </summary>
internal sealed class DmlChecker
{
    private static readonly RandomUsersTable s_users = new("");
    private static readonly RandomDeptsTable s_depts = new("");

    private static readonly PlannedStatement s_readUsers = new(
        "READ USERS",
        "Select(u.Id, u.Name, u.Age, u.DeptId).From(u)",
        () => Select(s_users.Id, s_users.Name, s_users.Age, s_users.DeptId).From(s_users));

    private static readonly PlannedStatement s_readDepts = new(
        "READ DEPTS",
        "Select(d.Id, d.Name).From(d)",
        () => Select(s_depts.Id, s_depts.Name).From(s_depts));

    private readonly StatementRunner _runner;

    public DmlChecker(IDbConnection connection, Dbms dbms)
    {
        _runner = new StatementRunner(connection, dbms);
    }

    public DmlOutcome Run(PlannedStatement statement) =>
        _runner.RunInRolledBackTransaction(transaction =>
        {
            Outcome outcome = _runner.Run(statement, transaction);
            return outcome.Error is not null
                ? new DmlOutcome(outcome, null, null)
                : new DmlOutcome(
                    outcome,
                    _runner.Run(s_readUsers, transaction).Rows,
                    _runner.Run(s_readDepts, transaction).Rows);
        });

    public DmlOutcome Run(DmlSpec spec) => Run(spec.Planned(""));

    /// <summary>Throws <see cref="RandomizedCaseException"/> naming every relation that does
    /// not hold.</summary>
    public void Check(DmlSpec spec)
    {
        DmlOutcome baseline = Run(spec);
        if (baseline.Users is null)
        {
            throw new RandomizedCaseException("the generated statement failed\n"
                + baseline.Show("BASE"));
        }

        List<string> failures = [];
        DmlPlan plan = DmlPlanner.Plan(spec);
        foreach ((string name, DmlSpec variant) in plan.Rewrites)
        {
            DmlOutcome result = Run(variant);
            if (result.Users is null || !SameEffect(baseline, result))
            {
                failures.Add($"{name} does not hold\n" + baseline.Show("BASE")
                    + result.Show("VARIANT"));
            }
        }

        CheckReference(spec, baseline, failures);
        CheckMatch(spec, baseline, failures);
        CheckOutputAgainstState(spec, baseline, failures);
        if (failures.Count > 0)
        {
            throw new RandomizedCaseException(string.Join("\n", failures));
        }
    }

    private static bool SameEffect(DmlOutcome a, DmlOutcome b) =>
        DmlOutcome.Same(a.Users!, b.Users!) && DmlOutcome.Same(a.Depts!, b.Depts!)
        && DmlOutcome.Same(a.Statement.Rows!, b.Statement.Rows!);

    private static void CheckReference(DmlSpec spec, DmlOutcome actual, List<string> failures)
    {
        DmlExpectation expected;
        try
        {
            expected = DmlReference.Evaluate(spec);
        }
        catch (ReferenceUnavailableException)
        {
            return;
        }

        foreach ((string what, List<string> want, List<string> got) in new[]
        {
            ("users", expected.Users, actual.Users!),
            ("depts", expected.Depts, actual.Depts!),
            ("output", expected.Output, actual.Statement.Rows!),
        })
        {
            if (!DmlOutcome.Same(want, got))
            {
                failures.Add($"R1 {what} differ from the reference evaluation\n"
                    + $"EXPECTED-ONLY: {string.Join(" ; ", want.Except(got))}\n"
                    + $"ACTUAL-ONLY: {string.Join(" ; ", got.Except(want))}\n"
                    + actual.Show("ACTUAL"));
            }
        }
    }

    // R2: the rows the statement read are the rows a SELECT with its join and WHERE returns.
    private void CheckMatch(DmlSpec spec, DmlOutcome actual, List<string> failures)
    {
        if (spec.PlannedMatch("MATCH") is not { } planned)
        {
            return;
        }

        Outcome match = _runner.RunInRolledBackTransaction(t => _runner.Run(planned, t));
        if (match.Error is not null)
        {
            failures.Add("R2 the match SELECT failed\n" + match.Show("MATCH"));
            return;
        }

        List<string> written = [.. actual.Users!.Except(DmlReference.SeedUsers)];
        string relation;
        List<string> expected = match.Rows!;
        List<string> got;
        if (spec.Kind == DmlKind.Delete)
        {
            relation = "the deleted ids are the matched ids";
            got = [.. DmlReference.SeedUsers.Except(actual.Users!).Select(Id)];
        }
        else if (spec.Kind == DmlKind.Insert)
        {
            relation = "the inserted rows are the feeding SELECT's";
            got = [.. written.Select(r => Project(r, spec.Insert!))];
        }
        else
        {
            // A matched row can keep its values, so an UPDATE's changed ids are a subset.
            relation = "every changed id is a matched id";
            expected = [.. written.Select(Id)];
            got = [.. expected.Intersect(match.Rows!)];
        }

        if (!DmlOutcome.Same(expected, got))
        {
            failures.Add($"R2 {relation}\nEXPECTED({expected.Count}): "
                + string.Join(" ; ", expected) + "\n" + match.Show("MATCH") + actual.Show("BASE"));
        }

        if (spec.Output is DmlOutputLevel.Returning or DmlOutputLevel.Output
            && spec.Kind != DmlKind.Insert)
        {
            List<string> returned = [.. actual.Statement.Rows!.Select(r => r.Split('|')[0])
                .Where(id => id != "NULL")];
            if (!DmlOutcome.Same(returned, match.Rows!))
            {
                failures.Add("R2 the output ids are the matched ids\n" + match.Show("MATCH")
                    + actual.Show("BASE"));
            }
        }
    }

    // R3: each output row holds what its row reads back as: the target as the statement left
    // it, or (a DELETE's, and SQL Server's DELETED) as the seed held it.
    private static void CheckOutputAgainstState(
        DmlSpec spec,
        DmlOutcome actual,
        List<string> failures)
    {
        if (spec.Output == DmlOutputLevel.None)
        {
            return;
        }

        Dictionary<long, UserRow> after = actual.Users!.Select(DmlReference.ParseUser)
            .ToDictionary(u => u.Id);
        Dictionary<long, UserRow> before = RandomSchema.Users.ToDictionary(u => u.Id);
        IEnumerable<string> outputs = spec.Output == DmlOutputLevel.OutputInto
            ? actual.Depts!.Except(DmlReference.SeedDepts)
            : actual.Statement.Rows!;
        long offset = spec.Output == DmlOutputLevel.OutputInto ? DmlSpec.DestinationIdOffset : 0;
        List<string> expected = [];
        List<string> got = [];
        try
        {
            foreach (string row in outputs)
            {
                if (DmlReference.Cell(row.Split('|')[0]) is not long id)
                {
                    continue;
                }

                UserRow? old = before.GetValueOrDefault(id - offset);
                UserRow? updated = spec.Kind == DmlKind.Delete
                    ? null
                    : after.GetValueOrDefault(id - offset);
                expected.Add(DmlReference.OutputRow(spec, old, updated));
                got.Add(row);
            }
        }
        catch (ReferenceUnavailableException)
        {
            return;
        }

        if (!DmlOutcome.Same(expected, got))
        {
            failures.Add("R3 the output does not read back from the state\n"
                + $"EXPECTED: {string.Join(" ; ", expected)}\n" + actual.Show("BASE"));
        }
    }

    private static string Id(string row) => row.Split('|')[0];

    private static string Project(string row, InsertShape shape)
    {
        string[] cells = row.Split('|');
        string[] order = ["id", "name", "age", "dept_id"];
        return string.Join("|", shape.Columns.Select(c => cells[System.Array.IndexOf(order, c)]));
    }
}
