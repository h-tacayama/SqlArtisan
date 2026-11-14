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
/// Applies a <see cref="DmlSpec"/> to <see cref="RandomSchema"/>'s seed in C#: every SET right
/// side, WHERE and output item reads the rows as they stood before the statement, the
/// standard's reading, which the generators keep each engine to (<see cref="DmlGenerators"/>).
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
        ReferenceEvaluator.Normalize(row.DeptId));

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
            (long?)Cell(cells[3]));
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
        _ => row with { DeptId = (long?)value },
    };

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
