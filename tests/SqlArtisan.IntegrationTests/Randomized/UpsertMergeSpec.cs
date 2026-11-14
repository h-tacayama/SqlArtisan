using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Randomized;

/// <summary>An upsert's conflict clause over the rows its <see cref="InsertShape"/> proposes. An
/// update's <c>SET</c> and <c>WHERE</c> read the existing row as <c>u</c> and the proposed one
/// as <see cref="BuildContext.ExcludedKey"/>, which builds as <c>Excluded(...)</c>.</summary>
/// <param name="ArrayForm">Whether several rows go in one <c>Values(object?[][])</c>
/// call.</param>
internal sealed record UpsertShape(
    ArbiterLevel Arbiter,
    UpsertActionLevel Action,
    bool ArrayForm,
    IReadOnlyList<Assignment> Set,
    SpecCondition? Where)
{
    public bool Updates => Action is UpsertActionLevel.DoUpdate or UpsertActionLevel.DoUpdateWhere
        or UpsertActionLevel.Odku;

    /// <summary>Whether a key column of <paramref name="row"/> meets the existing row's under
    /// the arbiter: the named key, or either one where none is named.</summary>
    public bool Conflicts(UserRow existing, UserRow row) => Arbiter switch
    {
        ArbiterLevel.Id => existing.Id == row.Id,
        ArbiterLevel.Code => row.Code is not null && existing.Code == row.Code,
        _ => existing.Id == row.Id || (row.Code is not null && existing.Code == row.Code),
    };

    public UpsertShape Rewrite(SpecRewrite rewrite) => this with
    {
        Set = [.. Set.Select(a => a with { Value = a.Value.Rewrite(rewrite) })],
        Where = Where?.Rewrite(rewrite),
    };

    public ISqlBuilder Build(InsertShape shape, BuildContext context, Func<object[]>? returning)
    {
        DbTableBase t = context.Table("u");
        DbColumn[] columns = [.. shape.Columns.Select(c => context.Column("u", c))];
        object?[][] rows = [.. shape.Rows.Select(r => r.Select(e => e.Build(context)).ToArray())];
        EqualityCondition[] pairs =
            [.. columns.Zip(rows[0]).Select(p => p.First == p.Second!)];
        if (Action == UpsertActionLevel.Ignore)
        {
            if (shape.Source == InsertSourceLevel.SetForm)
            {
                return InsertIgnoreInto(t).Set(pairs);
            }

            IInsertIgnoreBuilderColumns ignore = InsertIgnoreInto(t, columns);
            if (ArrayForm)
            {
                return ignore.Values(rows);
            }

            IInsertIgnoreBuilderValues chained = ignore.Values(rows[0]);
            foreach (object?[] row in rows.Skip(1))
            {
                chained = chained.Values(row);
            }

            return chained;
        }

        IUpsert stage;
        if (shape.Source == InsertSourceLevel.SetForm)
        {
            stage = InsertInto(t).Set(pairs);
        }
        else if (ArrayForm)
        {
            stage = InsertInto(t, columns).Values(rows);
        }
        else
        {
            IInsertBuilderValues values = InsertInto(t, columns).Values(rows[0]);
            foreach (object?[] row in rows.Skip(1))
            {
                values = values.Values(row);
            }

            stage = values;
        }

        EqualityCondition[] set =
            [.. Set.Select(a => context.Column("u", a.Column) == a.Value.Build(context))];
        if (Action == UpsertActionLevel.Odku)
        {
            return stage.OnDuplicateKeyUpdate(set);
        }

        IInsertBuilderOnConflict conflict = Arbiter switch
        {
            ArbiterLevel.Id => stage.OnConflict(context.Column("u", "id")),
            ArbiterLevel.Code => stage.OnConflict(context.Column("u", "code")),
            _ => stage.OnConflict(),
        };
        IInsertBuilderConflictAction action = Action switch
        {
            UpsertActionLevel.DoNothing => conflict.DoNothing(),
            _ when Where is not null => conflict.DoUpdateSet(set).Where(Where.Build(context)),
            _ => conflict.DoUpdateSet(set),
        };
        return returning is null ? action : action.Returning(returning());
    }

    public string ToCSharp(InsertShape shape)
    {
        string head = Action == UpsertActionLevel.Ignore ? "InsertIgnoreInto" : "InsertInto";
        string Row(IReadOnlyList<SpecExpression> row) =>
            string.Join(", ", row.Select(e => e.ToCSharp()));
        string body;
        if (shape.Source == InsertSourceLevel.SetForm)
        {
            body = $"{head}(u).Set({string.Join(", ", shape.Columns.Zip(shape.Rows[0]).Select(p =>
                $"{new ColumnRef("u", p.First).ToCSharp()} == {p.Second.ToCSharp()}"))})";
        }
        else
        {
            string columns = string.Join(", ", shape.Columns.Select(c =>
                new ColumnRef("u", c).ToCSharp()));
            body = $"{head}(u, {columns})" + (ArrayForm
                ? $".Values(new object?[][] {{ {string.Join(", ", shape.Rows.Select(r =>
                    $"[{Row(r)}]"))} }})"
                : string.Concat(shape.Rows.Select(r => $".Values({Row(r)})")));
        }

        string set = string.Join(", ", Set.Select(a =>
            $"{new ColumnRef("u", a.Column).ToCSharp()} == {a.Value.ToCSharp()}"));
        return body + Action switch
        {
            UpsertActionLevel.Ignore => "",
            UpsertActionLevel.Odku => $".OnDuplicateKeyUpdate({set})",
            _ => Arbiter switch
            {
                ArbiterLevel.Id => ".OnConflict(u.Id)",
                ArbiterLevel.Code => ".OnConflict(u.Code)",
                _ => ".OnConflict()",
            } + (Action == UpsertActionLevel.DoNothing
                ? ".DoNothing()"
                : $".DoUpdateSet({set})" + (Where is null ? "" : $".Where({Where.ToCSharp()})")),
        };
    }
}

internal enum MergeClause { Matched, NotMatched, BySource }

internal enum MergeAction { Update, Delete, Insert }

/// <summary>One <c>WHEN</c> branch of a MERGE. A matched branch reads the target as <c>u</c>
/// and the source row as <c>s</c>, a not-matched one the source alone, a BY SOURCE one the
/// target alone.</summary>
/// <param name="When">The branch's <c>AND</c> condition (PostgreSQL, SQL Server).</param>
internal sealed record MergeBranch(MergeClause Clause, MergeAction Action, SpecCondition? When)
{
    public IReadOnlyList<Assignment> Set { get; init; } = [];

    public IReadOnlyList<string> InsertColumns { get; init; } = [];

    public IReadOnlyList<SpecExpression> InsertValues { get; init; } = [];

    /// <summary>Oracle's action filter: <c>UPDATE SET ... WHERE</c> or
    /// <c>INSERT ... VALUES ... WHERE</c>.</summary>
    public SpecCondition? ActionWhere { get; init; }

    /// <summary>Oracle's <c>DELETE WHERE</c>, which reads the row its UPDATE wrote.</summary>
    public SpecCondition? DeleteWhere { get; init; }

    public MergeBranch Rewrite(SpecRewrite rewrite) => this with
    {
        When = When?.Rewrite(rewrite),
        Set = [.. Set.Select(a => a with { Value = a.Value.Rewrite(rewrite) })],
        InsertValues = [.. InsertValues.Select(e => e.Rewrite(rewrite))],
        ActionWhere = ActionWhere?.Rewrite(rewrite),
        DeleteWhere = DeleteWhere?.Rewrite(rewrite),
    };

    public IMergeBuilderWhen Build(IMergeBuilderOn on, BuildContext context)
    {
        EqualityCondition[] set =
            [.. Set.Select(a => context.Column("u", a.Column) == a.Value.Build(context))];
        switch (Clause)
        {
            case MergeClause.Matched when When is not null:
                IMergeBuilderWhenMatchedAnd and = on.WhenMatched(When.Build(context));
                return Action == MergeAction.Delete ? and.ThenDelete() : and.ThenUpdateSet(set);
            case MergeClause.Matched:
                IMergeBuilderWhenMatched matched = on.WhenMatched();
                if (Action == MergeAction.Delete)
                {
                    return matched.ThenDelete();
                }

                IMergeBuilderThenUpdateSet update = matched.ThenUpdateSet(set);
                if (ActionWhere is not null)
                {
                    IMergeBuilderWhere filtered = update.Where(ActionWhere.Build(context));
                    return DeleteWhere is null
                        ? filtered
                        : filtered.DeleteWhere(DeleteWhere.Build(context));
                }

                return DeleteWhere is null
                    ? update
                    : update.DeleteWhere(DeleteWhere.Build(context));
            case MergeClause.NotMatched:
                DbColumn[] columns = [.. InsertColumns.Select(c => context.Column("u", c))];
                object[] values = [.. InsertValues.Select(e => e.Build(context))];
                if (When is not null)
                {
                    return on.WhenNotMatched(When.Build(context))
                        .ThenInsert(columns)
                        .Values(values);
                }

                IMergeBuilderValues inserted =
                    on.WhenNotMatched().ThenInsert(columns).Values(values);
                return ActionWhere is null ? inserted : inserted.Where(ActionWhere.Build(context));
            default:
                IMergeBuilderWhenNotMatchedBySource bySource = When is null
                    ? on.WhenNotMatchedBySource()
                    : on.WhenNotMatchedBySource(When.Build(context));
                return Action == MergeAction.Delete
                    ? bySource.ThenDelete()
                    : bySource.ThenUpdateSet(set);
        }
    }

    public string ToCSharp()
    {
        string when = When is null ? "" : When.ToCSharp();
        string head = Clause switch
        {
            MergeClause.Matched => $".WhenMatched({when})",
            MergeClause.NotMatched => $".WhenNotMatched({when})",
            _ => $".WhenNotMatchedBySource({when})",
        };
        string action = Action switch
        {
            MergeAction.Delete => ".ThenDelete()",
            MergeAction.Update => $".ThenUpdateSet({string.Join(", ", Set.Select(a =>
                $"{new ColumnRef("u", a.Column).ToCSharp()} == {a.Value.ToCSharp()}"))})",
            _ => $".ThenInsert({string.Join(", ", InsertColumns.Select(c =>
                new ColumnRef("u", c).ToCSharp()))})"
                + $".Values({string.Join(", ", InsertValues.Select(e => e.ToCSharp()))})",
        };
        return head + action
            + (ActionWhere is null ? "" : $".Where({ActionWhere.ToCSharp()})")
            + (DeleteWhere is null ? "" : $".DeleteWhere({DeleteWhere.ToCSharp()})");
    }
}

/// <summary>A MERGE source of the users columns, keyed by a unique id: <c>rt_users</c> (key
/// <c>s.id + Offset</c>), a query over <c>u5</c>/<c>d5</c> as a derived table or leading CTE,
/// or literal rows; <paramref name="Names"/> names the others' columns.</summary>
internal sealed record MergeSource(MergeSourceLevel Kind, IReadOnlyList<string> Names, int Offset)
{
    public static readonly string[] Columns = ["id", "name", "age", "dept_id"];

    public QueryBlock? Body { get; init; }

    public IReadOnlyList<IReadOnlyList<SpecExpression>> Rows { get; init; } = [];

    public MergeSource Rewrite(SpecRewrite rewrite) => this with
    {
        Body = Body?.Rewrite(rewrite),
        Rows = [.. Rows.Select(r => (IReadOnlyList<SpecExpression>)
            [.. r.Select(e => e.Rewrite(rewrite))])],
    };

    public string NameOf(string column) => Names[System.Array.IndexOf(Columns, column)];

    /// <summary>The source, with its columns registered on <paramref name="context"/>; a CTE
    /// comes back as the definition the statement's WITH leads with.</summary>
    public TableReference Build(BuildContext context, out CommonTableExpression? cte)
    {
        cte = null;
        switch (Kind)
        {
            case MergeSourceLevel.Table:
                RandomUsersTable table = new(BuildContext.SourceKey);
                context.Source = c => UsersColumn(table, c);
                return table;
            case MergeSourceLevel.Derived:
                SubqueryDerivedTable derived = Body!.Build(context).AsTable(BuildContext.SourceKey);

                // Through an alias, as the body's items are named: Oracle folds a bare id.
                context.Source = c => derived.Column(Null.As(NameOf(c)));
                return derived;
            case MergeSourceLevel.Cte:
                Cte named = new(BuildContext.SourceKey);
                cte = named.As(Body!.Build(context));
                context.Source = c => named.Column(Null.As(NameOf(c)));
                return named;
            default:
                ValuesDerivedTable values = Values(
                    BuildContext.SourceKey,
                    [.. Names],
                    [.. Rows.Select(r => r.Select(e => e.Build(context)).ToArray())]);
                context.Source = c => values.Column(NameOf(c));
                return values;
        }
    }

    public string Declaration() => Kind switch
    {
        MergeSourceLevel.Table => "RandomUsersTable s = new(\"s\");",
        MergeSourceLevel.Derived => $"SubqueryDerivedTable s = {Body!.ToCSharp()}.AsTable(\"s\");",
        MergeSourceLevel.Cte => "Cte s = new(\"s\");",
        _ => $"ValuesDerivedTable s = Values(\"s\", [{string.Join(", ", Names.Select(n =>
            $"\"{n}\""))}], new object?[][] {{ {string.Join(", ", Rows.Select(r =>
            $"[{string.Join(", ", r.Select(e => e.ToCSharp()))}]"))} }});",
    } + (Kind == MergeSourceLevel.Table
        ? ""
        : " // s.Id, s.Name, s.Age, s.DeptId are "
            + string.Join(", ", Names.Select(n => $"s.Column(\"{n}\")")));

    public string LeadCSharp() =>
        Kind == MergeSourceLevel.Cte ? $"With(s.As({Body!.ToCSharp()}))." : "";

    private static DbColumn UsersColumn(RandomUsersTable table, string column) => column switch
    {
        "id" => table.Id,
        "name" => table.Name,
        "age" => table.Age,
        "code" => table.Code,
        _ => table.DeptId,
    };
}

/// <summary>A MERGE into <c>rt_users</c> (always aliased <c>u</c>: <c>MergeInto</c> rejects a
/// bare target, #820), its source, <c>ON</c> condition and branches in order.</summary>
internal sealed record MergeShape(
    MergeSource Source,
    SpecCondition On,
    IReadOnlyList<MergeBranch> Branches)
{
    public MergeShape Rewrite(SpecRewrite rewrite) => this with
    {
        Source = Source.Rewrite(rewrite),
        On = On.Rewrite(rewrite),
        Branches = [.. Branches.Select(b => b.Rewrite(rewrite))],
    };

    public ISqlBuilder Build(BuildContext context)
    {
        TableReference source = Source.Build(context, out CommonTableExpression? cte);
        DbTableBase t = context.Table("u");
        IMergeBuilderTarget target = cte is null ? MergeInto(t) : With(cte).MergeInto(t);
        IMergeBuilderOn on = target.Using(source).On(On.Build(context));
        IMergeBuilderWhen? last = null;
        foreach (MergeBranch branch in Branches)
        {
            last = branch.Build(last ?? on, context);
        }

        return last!;
    }

    /// <summary>The ids of the target rows the source matches, by the MERGE's own source and
    /// <c>ON</c>.</summary>
    public ISqlBuilder BuildMatch(BuildContext context)
    {
        TableReference source = Source.Build(context, out CommonTableExpression? cte);
        DbTableBase t = context.Table("u");
        DbColumn id = context.Column("u", "id");
        ISelectBuilderSelect select = cte is null ? Select(id) : With(cte).Select(id);
        return select.From(t).InnerJoin(source).On(On.Build(context));
    }

    public string ToCSharp() => Source.LeadCSharp() + $"MergeInto(u).Using(s).On({On.ToCSharp()})"
        + string.Concat(Branches.Select(b => b.ToCSharp()));

    public string MatchCSharp() => Source.LeadCSharp()
        + $"Select(u.Id).From(u).InnerJoin(s).On({On.ToCSharp()})";
}
