using System.Data;

namespace SqlArtisan.IntegrationTests.Randomized;

internal sealed class RandomizedCaseException(string message) : Exception(message);

/// <summary>
/// Runs one generated query and the relations that must hold between it and its rewrites. A
/// rewrite compares on the same engine only, so engine-defined behaviour both sides share
/// cancels out.
/// </summary>
internal sealed class CaseChecker
{
    private readonly StatementRunner _runner;
    private readonly CaseRow _row;
    private readonly CasePlanner _planner;

    public CaseChecker(IDbConnection connection, CaseRow row)
    {
        _runner = new StatementRunner(connection, row.Dbms);
        _row = row;
        _planner = new CasePlanner(row);
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

        CasePlan plan = _planner.Plan(spec);
        bool ordered = spec.Query.OrderBy.Count > 0;
        foreach ((string name, QuerySpec variant) in plan.Rewrites)
        {
            Outcome result = Run(variant);
            bool inOrder = ordered && variant.Query.OrderBy.Count > 0 && !name.StartsWith("M9");
            if (result.Error is not null || !Same(baseline.Rows!, result.Rows!, inOrder))
            {
                failures.Add($"{name} does not hold\n" + baseline.Show("BASE")
                    + result.Show("VARIANT"));
            }
        }

        CheckPartition(plan.PartitionParts, failures);
        CheckSlice(spec, plan.Unlimited, baseline, failures);
        CheckWrap(plan.Wrapped, baseline, failures);
        CheckSetOperator(plan.SetOperatorParts, failures);
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
        if (CasePlanner.NegateWhere(spec) is not { } negatedSpec)
        {
            return null;
        }

        Outcome kept = Run(spec);
        Outcome negated = Run(negatedSpec);
        if (kept.Error is not null || negated.Error is not null)
        {
            throw new RandomizedCaseException(kept.Show("KEPT") + negated.Show("NEGATED"));
        }

        return !Same(kept.Rows!, negated.Rows!, inOrder: false);
    }

    public Outcome Run(QuerySpec spec, IDbTransaction? transaction = null) =>
        _runner.Run(PlannedStatement.Of("", spec), transaction);

    private void CheckPartition(QuerySpec[]? parts, List<string> failures)
    {
        if (parts is null)
        {
            return;
        }

        Outcome[] results = [.. parts.Select(p => Run(p))];
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
    private void CheckSlice(
        QuerySpec spec,
        QuerySpec? unlimited,
        Outcome baseline,
        List<string> failures)
    {
        if (spec.Query.Limit is not { } limit || unlimited is null)
        {
            return;
        }

        Outcome full = Run(unlimited);
        List<string>? expected = full.Rows?.Skip(limit.Offset).Take(limit.Count).ToList();
        if (expected is null || !expected.SequenceEqual(baseline.Rows!))
        {
            failures.Add($"M10 row limit is not a slice (expected rows "
                + $"{limit.Offset + 1}..{limit.Offset + limit.Count} of FULL)\n"
                + baseline.Show("BASE") + full.Show("FULL"));
        }
    }

    private void CheckWrap(PlannedStatement? wrapped, Outcome baseline, List<string> failures)
    {
        if (wrapped is null)
        {
            return;
        }

        Outcome result = _runner.Run(wrapped);
        if (result.Error is not null || !Same(baseline.Rows!, result.Rows!, false))
        {
            failures.Add($"M15 {_row.Wrap} wrap does not keep the rows\n"
                + baseline.Show("BASE") + result.Show("WRAPPED"));
        }
    }

    private void CheckSetOperator(
        (QuerySpec Left, QuerySpec Right, QuerySpec Compound)? parts,
        List<string> failures)
    {
        if (parts is not { } p || p.Compound.Query.Tail is not [SetBranch branch])
        {
            return;
        }

        Outcome left = Run(p.Left);
        Outcome right = Run(p.Right);
        Outcome actual = Run(p.Compound);
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

    private static bool Same(List<string> a, List<string> b, bool inOrder) => inOrder
        ? a.SequenceEqual(b)
        : a.Order(StringComparer.Ordinal).SequenceEqual(b.Order(StringComparer.Ordinal));
}
