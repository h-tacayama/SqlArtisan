namespace SqlArtisan.IntegrationTests.Randomized;

/// <summary>
/// The upsert and MERGE rewrites that keep a statement's meaning only under conditions
/// <see cref="DmlGenerators"/> holds: every proposed key is unique in its batch, and a proposed
/// row meets at most one existing row, on keys that include the arbiter.
/// </summary>
internal static class UpsertRewrites
{
    private static readonly Literal s_one = new(1);

    /// <summary>A named arbiter dropped: with each conflict on one row and on the named key,
    /// any unique key arbitrates the same rows. PostgreSQL's DO UPDATE needs the target.</summary>
    public static DmlSpec? Targetless(DmlSpec spec)
    {
        if (spec.Upsert is not { Arbiter: not ArbiterLevel.Targetless } upsert
            || (upsert.Updates && spec.Dbms == Dbms.PostgreSql))
        {
            return null;
        }

        return spec with { Upsert = upsert with { Arbiter = ArbiterLevel.Targetless } };
    }

    /// <summary><c>DO NOTHING</c> or <c>INSERT IGNORE</c> as one <c>INSERT ... SELECT</c> branch
    /// per row under a <c>NOT EXISTS</c> over its keys: equal only while the batch's keys are
    /// unique and IGNORE coerces no value. No RETURNING follows a SELECT.</summary>
    public static DmlSpec? NotExistsInsert(DmlSpec spec)
    {
        if (spec.Upsert is not { Updates: false } upsert || spec.Output != DmlOutputLevel.None)
        {
            return null;
        }

        InsertShape shape = spec.Insert!;

        // PostgreSQL 16.15 resolves UNION ALL pairwise from the left: NULL beside NULL is text,
        // which an integer branch after it refuses (UnionAll_PostgreSql_NullPairThenInteger_...).
        if (shape.Rows.Count > 1 && Enumerable.Range(0, shape.Columns.Count)
            .Any(i => shape.Rows[0][i] is NullValue && shape.Rows[1][i] is NullValue))
        {
            return null;
        }

        IReadOnlyList<(Literal Id, Literal? Code)> keys = spec.ProposedKeys();
        QueryBlock[] branches = [.. shape.Rows.Select((row, i) => new QueryBlock
        {
            From = "d6",
            Items = row,
            Where = new Junction(true,
            [
                new Comparison("==", new ColumnRef("d6", "id"), s_one),
                new ExistsTest(
                    new QueryBlock
                    {
                        From = "u6",
                        Items = [new ColumnRef("u6", "id")],
                        Where = Meets(upsert.Arbiter, keys[i]),
                    },
                    true),
            ]),
        })];
        QueryBlock feed = branches[0] with
        {
            Tail = [.. branches.Skip(1).Select(b => new SetBranch(SetOperatorLevel.UnionAll, b))],
        };
        return spec with
        {
            Kind = DmlKind.Insert,
            Upsert = null,
            Insert = new InsertShape(InsertSourceLevel.Select, true, shape.Columns, [], feed),
        };
    }

    // A key whose code is NULL meets no row through it.
    private static SpecCondition Meets(ArbiterLevel arbiter, (Literal Id, Literal? Code) key)
    {
        SpecCondition id = new Comparison("==", new ColumnRef("u6", "id"), key.Id);
        if (arbiter == ArbiterLevel.Id || key.Code is null)
        {
            return arbiter == ArbiterLevel.Code
                ? new Comparison("!=", new ColumnRef("u6", "id"), new ColumnRef("u6", "id"))
                : id;
        }

        SpecCondition code = new Comparison("==", new ColumnRef("u6", "code"), key.Code);
        return arbiter == ArbiterLevel.Code ? code : new Junction(false, [id, code]);
    }

    /// <summary>
    /// A MERGE as a correlated UPDATE of the matched rows, then an INSERT ... SELECT of the
    /// unmatched source rows. Equal only with the UPDATE first, no SET of the ON column, no
    /// DELETE or BY SOURCE branch, and an INSERT that reads no rt_users row the UPDATE wrote.
    /// </summary>
    /// <remarks>The UPDATE target must be aliased to correlate, which SQL Server's cannot be, and
    /// a VALUES or CTE source is no table a subquery here can name.</remarks>
    public static DmlSpec? UpdateThenInsert(DmlSpec spec)
    {
        if (spec.Merge is not { } merge
            || spec.Dbms is not (Dbms.Oracle or Dbms.PostgreSql)
            || merge.Source.Kind is not (MergeSourceLevel.Table or MergeSourceLevel.Derived)
            || merge.Branches.Any(b => b.Action == MergeAction.Delete || b.DeleteWhere is not null
                || b.Clause == MergeClause.BySource)
            || merge.On is not Comparison { Right: var key })
        {
            return null;
        }

        MergeSource source = merge.Source;
        string from = source.Body?.From ?? "u7";
        // The source's columns read through the table or the body's items they name.
        SpecRewrite inline = SpecRewrite.Expressions(x =>
            x is ColumnRef { Table: BuildContext.SourceKey } c
                ? source.Body is { } body
                    ? body.Items[System.Array.IndexOf(MergeSource.Columns, c.Column)]
                    : c with { Table = "u7" }
                : x);
        SpecExpression Sub(SpecExpression e) => e.Rewrite(inline);
        SpecCondition SubCondition(SpecCondition c) => c.Rewrite(inline);
        SpecExpression sourceKey = Sub(key);
        SpecCondition Joined(SpecCondition link, SpecCondition? extra) => new Junction(
            true,
            [.. new[] { source.Body?.Where, link, extra is null ? null : SubCondition(extra) }
                .OfType<SpecCondition>()]);

        DmlSpec? update = null;
        if (merge.Branches.FirstOrDefault(b => b.Clause == MergeClause.Matched) is { } matched)
        {
            SpecCondition link = new Comparison("==", sourceKey, new ColumnRef("u", "id"));
            update = new DmlSpec
            {
                Kind = DmlKind.Update,
                Dbms = spec.Dbms,
                Unaliased = spec.Unaliased,
                Set = [.. matched.Set.Select(a => a with
                {
                    Value = new ScalarSubquery(new QueryBlock
                    {
                        From = from,
                        Items = [Sub(a.Value)],
                        Where = Joined(link, null),
                    }),
                })],
                Where = new ExistsTest(
                    new QueryBlock
                    {
                        From = from,
                        Items = [new ColumnRef(from, "id")],
                        Where = Joined(link, matched.When ?? matched.ActionWhere),
                    },
                    false),
            };
        }

        DmlSpec? insert = null;
        if (merge.Branches.FirstOrDefault(b => b.Clause == MergeClause.NotMatched) is { } branch)
        {
            SpecCondition absent = new ExistsTest(
                new QueryBlock
                {
                    From = "u6",
                    Items = [new ColumnRef("u6", "id")],
                    Where = new Comparison("==", new ColumnRef("u6", "id"), sourceKey),
                },
                true);
            insert = new DmlSpec
            {
                Kind = DmlKind.Insert,
                Dbms = spec.Dbms,
                Unaliased = spec.Unaliased,
                Insert = new InsertShape(
                    InsertSourceLevel.Select,
                    true,
                    branch.InsertColumns,
                    [],
                    new QueryBlock
                    {
                        From = from,
                        Items = [.. branch.InsertValues.Select(Sub)],
                        Where = Joined(absent, branch.When ?? branch.ActionWhere),
                    }),
            };
        }

        if (update is not null && insert is not null && ReadsUsers(insert.Insert!.Feed!))
        {
            return null;
        }

        return update is null ? insert : update with { Then = insert };
    }

    // Its NOT EXISTS reads only ids, which no MERGE here sets.
    private static bool ReadsUsers(QueryBlock feed)
    {
        static bool Reads(QueryBlock block) =>
            block.From is ['u', not '6'] || block.Tail.Any(b => b.Query.From[0] == 'u');
        bool found = feed.From[0] == 'u';
        feed.Rewrite(new SpecRewrite(
            e =>
            {
                found |= e is ScalarSubquery s && Reads(s.Query);
                return e;
            },
            c =>
            {
                found |= (c is InSubquery i && Reads(i.Query))
                    || (c is ExistsTest x && Reads(x.Query));
                return c;
            }));
        return found;
    }
}
