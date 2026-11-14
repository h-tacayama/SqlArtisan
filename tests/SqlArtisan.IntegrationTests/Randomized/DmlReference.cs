namespace SqlArtisan.IntegrationTests.Randomized;

/// <summary>What a DML statement leaves and returns, each row in the engine's normalized
/// form.</summary>
/// <param name="Matched">The target ids an UPDATE or DELETE reads, or an INSERT writes.</param>
internal sealed record DmlExpectation(
    List<string> Users,
    List<string> Depts,
    List<string> Output,
    IReadOnlyList<long> Matched);

/// <summary>
/// Applies a <see cref="DmlSpec"/> to <see cref="RandomSchema"/>'s seed in C#: SET, WHERE and
/// subqueries read the pre-statement rows, the standard's reading the generators keep each engine
/// to (<see cref="DmlGenerators"/>); an output item reads the row it returns.
/// </summary>
internal static class DmlReference
{
    private static readonly Dictionary<string, object?> s_noRows = [];

    public static List<string> SeedUsers { get; } = [.. RandomSchema.Users.Select(Text)];

    public static List<string> SeedDepts { get; } = [.. RandomSchema.Depts.Select(Text)];

    /// <param name="mutated">Leaves the lowest matched id as the seed holds it, the
    /// deliberate error the mutation control must see.</param>
    public static DmlExpectation Evaluate(DmlSpec spec, bool mutated = false)
    {
        if (spec.Kind is DmlKind.Upsert or DmlKind.Merge)
        {
            return Reverted(
                spec.Kind == DmlKind.Upsert ? EvaluateUpsert(spec) : EvaluateMerge(spec),
                mutated);
        }

        List<UserRow> users = [.. RandomSchema.Users];
        List<DeptRow> depts = [.. RandomSchema.Depts];
        List<(UserRow? Old, UserRow? New)> affected = [];
        if (spec.Kind == DmlKind.Insert)
        {
            foreach (UserRow row in Inserted(spec))
            {
                affected.Add((null, row));
            }

            users.AddRange(affected.Select(a => a.New!).Skip(mutated ? 1 : 0));
        }
        else
        {
            foreach (IReadOnlyDictionary<string, object?> env in Matches(spec))
            {
                UserRow old = (UserRow)env["u"]!;
                affected.Add((old, spec.Kind == DmlKind.Update ? Updated(spec, old, env) : null));
            }

            long? kept = mutated && affected.Count > 0 ? affected.Min(a => a.Old!.Id) : null;
            foreach ((UserRow? old, UserRow? updated) in affected.Where(a => a.Old!.Id != kept))
            {
                int index = users.FindIndex(u => u.Id == old!.Id);
                if (updated is null)
                {
                    users.RemoveAt(index);
                }
                else
                {
                    users[index] = updated;
                }
            }
        }

        List<string> output = Output(spec, affected, depts);
        return new DmlExpectation(
            [.. users.Select(Text)],
            [.. depts.Select(Text)],
            output,
            [.. affected.Select(a => (a.New ?? a.Old)!.Id)]);
    }

    /// <summary>The target ids the statement's WHERE and join keep, or <see langword="null"/>
    /// where the reference does not model them.</summary>
    public static IReadOnlyList<long>? MatchedIds(DmlSpec spec)
    {
        try
        {
            return [.. Matches(spec).Select(env => ((UserRow)env["u"]!).Id)];
        }
        catch (ReferenceUnavailableException)
        {
            return null;
        }
    }

    /// <summary>Whether the statement leaves both tables as the seed holds them; unknown counts
    /// as changing.</summary>
    public static bool ChangesNothing(DmlSpec spec)
    {
        try
        {
            DmlExpectation expected = Evaluate(spec);
            return expected.Users.SequenceEqual(SeedUsers)
                && expected.Depts.SequenceEqual(SeedDepts);
        }
        catch (ReferenceUnavailableException)
        {
            return false;
        }
    }

    public static string Text(UserRow row) => string.Join(
        "|",
        ReferenceEvaluator.Normalize(row.Id),
        ReferenceEvaluator.Normalize(row.Name),
        ReferenceEvaluator.Normalize(row.Age),
        ReferenceEvaluator.Normalize(row.DeptId),
        ReferenceEvaluator.Normalize(row.Code));

    public static string Text(DeptRow row) =>
        ReferenceEvaluator.Normalize(row.Id) + "|" + ReferenceEvaluator.Normalize(row.Name);

    /// <summary>Reads back a row <see cref="Text(UserRow)"/> wrote, or an engine's own.</summary>
    public static UserRow ParseUser(string text)
    {
        string[] cells = text.Split('|');
        return new UserRow(
            (long)Cell(cells[0])!,
            (string?)Cell(cells[1]),
            (long?)Cell(cells[2]),
            (long?)Cell(cells[3]),
            (long?)Cell(cells[4]));
    }

    public static object? Cell(string cell) => cell switch
    {
        "NULL" => null,
        ['\'', .., '\''] => cell[1..^1],
        _ => long.Parse(cell, System.Globalization.CultureInfo.InvariantCulture),
    };

    /// <summary>The value of an output item over the row it reads: the target as the
    /// statement left it, as it stood before (a DELETE's), or SQL Server's pseudo-tables.</summary>
    public static string OutputRow(DmlSpec spec, UserRow? old, UserRow? updated)
    {
        Dictionary<string, object?> env = new()
        {
            ["u"] = updated ?? old,
            [PseudoColumn.InsertedKey] = updated,
            [PseudoColumn.DeletedKey] = old,
        };
        return string.Join("|", spec.OutputItems.Select(e =>
            ReferenceEvaluator.Normalize(ReferenceEvaluator.ValueOf(e, env, spec.Dbms))));
    }

    private static List<IReadOnlyDictionary<string, object?>> Matches(DmlSpec spec)
    {
        List<IReadOnlyDictionary<string, object?>> matches =
            ReferenceEvaluator.Matches(spec.MatchQuery()!, spec.Dbms);
        if (matches.Select(env => ((UserRow)env["u"]!).Id).Distinct().Count() != matches.Count)
        {
            throw new InvalidOperationException(
                "A target row matched two source rows, which DmlGenerators never builds.");
        }

        return matches;
    }

    private static UserRow Updated(
        DmlSpec spec,
        UserRow old,
        IReadOnlyDictionary<string, object?> env)
    {
        UserRow row = old;
        foreach (Assignment assignment in spec.Set)
        {
            object? value = ReferenceEvaluator.ValueOf(assignment.Value, env, spec.Dbms);
            row = With(row, assignment.Column, value);
        }

        return row;
    }

    private static IEnumerable<UserRow> Inserted(DmlSpec spec)
    {
        InsertShape shape = spec.Insert!;
        IEnumerable<object?[]> rows = shape.Feed is { } feed
            ? ReferenceEvaluator.RawRows(feed, spec.Dbms)
            : shape.Rows.Select(r => r
                .Select(e => ReferenceEvaluator.ValueOf(e, s_noRows, spec.Dbms))
                .ToArray());
        foreach (object?[] values in rows)
        {
            UserRow row = new(0, null, null, null);
            for (int i = 0; i < values.Length; i++)
            {
                row = With(row, shape.Columns[i], values[i]);
            }

            yield return row;
        }
    }

    private static UserRow With(UserRow row, string column, object? value) => column switch
    {
        "id" => row with { Id = (long)value! },
        "name" => row with { Name = (string?)value },
        "age" => row with { Age = (long?)value },
        "code" => row with { Code = (long?)value },
        _ => row with { DeptId = (long?)value },
    };

    // The mutation control's error for the statements that both write and keep rows: the
    // lowest changed id is left as the seed holds it, or not inserted.
    private static DmlExpectation Reverted(DmlExpectation expected, bool mutated)
    {
        if (!mutated)
        {
            return expected;
        }

        List<string> changed = [.. expected.Users.Except(SeedUsers)
            .Concat(SeedUsers.Except(expected.Users))];
        if (changed.Count == 0)
        {
            return expected;
        }

        long lowest = changed.Select(r => ParseUser(r).Id).Min();
        List<string> users = [.. expected.Users.Where(r => ParseUser(r).Id != lowest)];
        users.AddRange(SeedUsers.Where(r => ParseUser(r).Id == lowest));
        return expected with { Users = users };
    }

    /// <summary>Applies each proposed row in VALUES order: a row no existing one meets under the
    /// arbiter is inserted; otherwise the action skips it or updates the row it meets, the
    /// <c>SET</c> and <c>WHERE</c> reading that row as it stood.</summary>
    private static DmlExpectation EvaluateUpsert(DmlSpec spec)
    {
        UpsertShape upsert = spec.Upsert!;
        List<UserRow> users = [.. RandomSchema.Users];
        List<(UserRow? Old, UserRow? New)> affected = [];
        foreach (UserRow proposed in Inserted(spec))
        {
            int index = users.FindIndex(u => upsert.Conflicts(u, proposed));
            if (index < 0)
            {
                users.Add(proposed);
                affected.Add((null, proposed));
                continue;
            }

            UserRow existing = users[index];
            Dictionary<string, object?> env = new()
            {
                ["u"] = existing,
                [BuildContext.ExcludedKey] = proposed,
            };
            if (!upsert.Updates
                || (upsert.Where is { } where
                    && ReferenceEvaluator.TruthOf(where, env, spec.Dbms) != true))
            {
                continue;
            }

            UserRow updated = Assigned(spec, upsert.Set, existing, env);
            users[index] = updated;
            affected.Add((existing, updated));
        }

        List<DeptRow> depts = [.. RandomSchema.Depts];
        return new DmlExpectation(
            [.. users.Select(Text)],
            [.. depts.Select(Text)],
            Output(spec, affected, depts),
            [.. affected.Select(a => a.New!.Id)]);
    }

    /// <summary>Joins each source row to the starting target; each row takes the first branch of
    /// its clause whose condition holds. Oracle's DELETE WHERE reads the row its UPDATE wrote,
    /// and only a row the UPDATE wrote.</summary>
    private static DmlExpectation EvaluateMerge(DmlSpec spec)
    {
        MergeShape merge = spec.Merge!;
        Dictionary<long, UserRow?> state = RandomSchema.Users.ToDictionary(u => u.Id, u => u)!;
        List<long> order = [.. RandomSchema.Users.Select(u => u.Id)];
        HashSet<long> matched = [];
        List<long> affected = [];
        foreach (UserRow source in SourceRows(spec))
        {
            Dictionary<string, object?> env = new() { [BuildContext.SourceKey] = source };
            UserRow? target = RandomSchema.Users.FirstOrDefault(u =>
            {
                env["u"] = u;
                return ReferenceEvaluator.TruthOf(merge.On, env, spec.Dbms) == true;
            });
            env["u"] = target;
            if (target is not null)
            {
                matched.Add(target.Id);
            }

            MergeClause clause = target is null ? MergeClause.NotMatched : MergeClause.Matched;
            MergeBranch? branch = merge.Branches.FirstOrDefault(b => b.Clause == clause
                && (b.When is null || ReferenceEvaluator.TruthOf(b.When, env, spec.Dbms) == true));
            if (branch is null || (branch.ActionWhere is { } filter
                && ReferenceEvaluator.TruthOf(filter, env, spec.Dbms) != true))
            {
                continue;
            }

            if (target is null)
            {
                UserRow row = new(0, null, null, null);
                foreach ((string column, SpecExpression value) in
                    branch.InsertColumns.Zip(branch.InsertValues))
                {
                    row = With(row, column, ReferenceEvaluator.ValueOf(value, env, spec.Dbms));
                }

                state[row.Id] = row;
                order.Add(row.Id);
                affected.Add(row.Id);
                continue;
            }

            affected.Add(target.Id);
            if (branch.Action == MergeAction.Delete)
            {
                state[target.Id] = null;
                continue;
            }

            UserRow updated = Assigned(spec, branch.Set, target, env);
            env["u"] = updated;
            state[target.Id] = branch.DeleteWhere is { } delete
                && ReferenceEvaluator.TruthOf(delete, env, spec.Dbms) == true
                ? null
                : updated;
        }

        foreach (UserRow target in RandomSchema.Users.Where(u => !matched.Contains(u.Id)))
        {
            Dictionary<string, object?> env = new() { ["u"] = target };
            if (merge.Branches.FirstOrDefault(b => b.Clause == MergeClause.BySource
                && (b.When is null || ReferenceEvaluator.TruthOf(b.When, env, spec.Dbms) == true))
                is not { } branch)
            {
                continue;
            }

            affected.Add(target.Id);
            state[target.Id] = branch.Action == MergeAction.Delete
                ? null
                : Assigned(spec, branch.Set, target, env);
        }

        return new DmlExpectation(
            [.. order.Select(id => state[id]).OfType<UserRow>().Select(Text)],
            SeedDepts,
            [],
            affected);
    }

    /// <summary>A MERGE source's rows, as users rows of its id, name, age and dept_id.</summary>
    public static IEnumerable<UserRow> SourceRows(DmlSpec spec)
    {
        MergeSource source = spec.Merge!.Source;
        IEnumerable<object?[]> rows = source.Kind switch
        {
            MergeSourceLevel.Table => RandomSchema.Users
                .Select(u => new object?[] { u.Id, u.Name, u.Age, u.DeptId, u.Code }),
            MergeSourceLevel.Values => source.Rows.Select(r => r
                .Select(e => ReferenceEvaluator.ValueOf(e, s_noRows, spec.Dbms))
                .ToArray()),
            _ => ReferenceEvaluator.RawRows(source.Body!, spec.Dbms),
        };
        return rows.Select(r => new UserRow(
            (long)r[0]!,
            (string?)r[1],
            (long?)r[2],
            (long?)r[3],
            r.Length > 4 ? (long?)r[4] : null));
    }

    // Every right side reads the row as it stood before the assignments.
    private static UserRow Assigned(
        DmlSpec spec,
        IReadOnlyList<Assignment> set,
        UserRow row,
        IReadOnlyDictionary<string, object?> env)
    {
        UserRow result = row;
        foreach (Assignment assignment in set)
        {
            object? value = ReferenceEvaluator.ValueOf(assignment.Value, env, spec.Dbms);
            result = With(result, assignment.Column, value);
        }

        return result;
    }

    private static List<string> Output(
        DmlSpec spec,
        List<(UserRow? Old, UserRow? New)> affected,
        List<DeptRow> depts)
    {
        switch (spec.Output)
        {
            case DmlOutputLevel.None:
                return [];
            case DmlOutputLevel.OutputInto:
                foreach ((UserRow? old, UserRow? updated) in affected)
                {
                    string[] cells = OutputRow(spec, old, updated).Split('|');
                    depts.Add(new DeptRow((long)Cell(cells[0])!, (string?)Cell(cells[1])));
                }

                return [];
            default:
                List<string> rows = [.. affected.Select(a => OutputRow(spec, a.Old, a.New))];

                // Oracle's INTO reads one row's values into the parameters, NULL with none.
                return spec.Dbms == Dbms.Oracle && rows.Count == 0
                    ? [string.Join("|", spec.OutputItems.Select(_ => "NULL"))]
                    : rows;
        }
    }
}
