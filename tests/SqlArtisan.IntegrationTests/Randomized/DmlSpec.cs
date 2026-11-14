using System.Data;
using System.Globalization;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Randomized;

internal enum DmlKind { Insert, Update, Delete, Upsert, Merge }

/// <summary>How a joined UPDATE or DELETE reaches its source table: MySQL's direct join, a
/// FROM that leaves the target out (PostgreSQL, SQLite), one that re-lists it (SQL Server's
/// UPDATE, MySQL's and SQL Server's DELETE), or PostgreSQL's <c>DELETE ... USING</c>.</summary>
internal enum DmlJoinForm { Direct, From, FromRelisted, Using }

/// <summary>The joined source, always the aliased <c>rt_depts</c> <c>d</c>: its <c>id</c> is
/// unique, so <c>On</c>, which equates it to the target's <c>dept_id</c>, matches one source
/// row at most per target row.</summary>
internal sealed record DmlJoin(DmlJoinForm Form, bool IsInner, SpecCondition On);

internal sealed record Assignment(string Column, SpecExpression Value);

/// <summary>A CTE of one integer column <c>c0</c>: a query over <c>u4</c>/<c>d4</c>, or the
/// recursive series <paramref name="Start"/>, <c>+ Step</c>, ... that stops at the first value
/// not below <paramref name="Below"/>.</summary>
internal sealed record CteSpec(bool Recursive, QueryBlock? Body, int Start, int Step, int Below)
{
    public CommonTableExpression Define(BuildContext context)
    {
        Cte c = context.Cte();
        if (!Recursive)
        {
            return c.As(Body!.Build(context));
        }

        DbColumn c0 = context.CteColumn();
        return c.As(Select(Bind(Start).As(BuildContext.CteColumnName))
            .UnionAll
            .Select((c0 + Step).As(BuildContext.CteColumnName))
            .From(c)
            .Where(c0 < Below));
    }

    public string ToCSharp() => Recursive
        ? $"c.As(Select(Bind({Start}).As(\"c0\")).UnionAll.Select((c.Column(\"c0\") + {Step})"
            + $".As(\"c0\")).From(c).Where(c.Column(\"c0\") < {Below}))"
        : $"c.As({Body!.ToCSharp()})";
}

/// <summary>The SQL keyword <c>NULL</c> as a value.</summary>
internal sealed record NullValue(SpecType ValueType) : SpecExpression
{
    public override SpecType Type => ValueType;

    public override object Build(BuildContext context) => Null;

    public override string ToCSharp() => "Null";
}

/// <summary>SQL Server's <c>INSERTED.col</c> / <c>DELETED.col</c> of the target.</summary>
internal sealed record PseudoColumn(bool IsInserted, string Column) : SpecExpression
{
    public const string InsertedKey = "inserted";
    public const string DeletedKey = "deleted";

    public string Key => IsInserted ? InsertedKey : DeletedKey;

    public override SpecType Type => Column == "name" ? SpecType.Text : SpecType.Int;

    public override object Build(BuildContext context) => IsInserted
        ? Inserted(context.Column("u", Column))
        : Deleted(context.Column("u", Column));

    public override string ToCSharp() =>
        $"{(IsInserted ? "Inserted" : "Deleted")}({new ColumnRef("u", Column).ToCSharp()})";
}

/// <summary><c>MAX(c0)</c> over the statement's CTE.</summary>
internal sealed record CteMax(CteSpec Cte) : SpecExpression
{
    public override SpecType Type => SpecType.Int;

    public override object Build(BuildContext context) =>
        Select(Max(context.CteColumn())).From(context.Cte());

    public override string ToCSharp() => "Select(Max(c.Column(\"c0\"))).From(c)";
}

/// <summary><c>operand IN (SELECT c0 FROM c)</c> over the statement's CTE.</summary>
internal sealed record InCte(SpecExpression Operand, CteSpec Cte) : SpecCondition
{
    public override SpecCondition Rewrite(SpecRewrite rewrite) =>
        rewrite.OnCondition(this with { Operand = Operand.Rewrite(rewrite) });

    public override SqlCondition Build(BuildContext context) => Operand.BuildOperand(context)
        .In(Select(context.CteColumn()).From(context.Cte()));

    public override string ToCSharp() =>
        $"{Operand.ToCSharpOperand()}.In(Select(c.Column(\"c0\")).From(c))";
}

/// <summary>The rows an INSERT writes: literal rows, or the SELECT that feeds it.</summary>
/// <param name="ListsColumns">Whether the statement names <paramref name="Columns"/>;
/// otherwise its rows are positional, in table order, or <c>Set(...)</c> pairs.</param>
internal sealed record InsertShape(
    InsertSourceLevel Source,
    bool ListsColumns,
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<SpecExpression>> Rows,
    QueryBlock? Feed);

/// <summary>
/// One generated INSERT, UPDATE, DELETE, upsert or MERGE on <c>rt_users</c> (key <c>u</c>). A
/// table key is unaliased when <see cref="Unaliased"/> holds it, as in <see cref="QuerySpec"/>.
/// </summary>
internal sealed record DmlSpec
{
    /// <summary>The key-equality conjunct Oracle's <c>RETURNING ... INTO</c> takes, outside
    /// <see cref="Where"/> so a rewrite or the negated WHERE keeps it.</summary>
    public long? PinnedId { get; init; }

    public DmlKind Kind { get; init; }

    public Dbms Dbms { get; init; }

    public IReadOnlySet<string> Unaliased { get; init; } = new HashSet<string>();

    public CteSpec? Cte { get; init; }

    /// <summary>Whether the CTE sits between <c>INSERT INTO</c> and its SELECT rather than
    /// leading the statement.</summary>
    public bool CteInSelect { get; init; }

    public DmlJoin? Join { get; init; }

    public IReadOnlyList<Assignment> Set { get; init; } = [];

    public SpecCondition? Where { get; init; }

    public InsertShape? Insert { get; init; }

    public DmlOutputLevel Output { get; init; }

    public IReadOnlyList<SpecExpression> OutputItems { get; init; } = [];

    /// <summary>An upsert's conflict clause, over the rows <see cref="Insert"/> proposes.</summary>
    public UpsertShape? Upsert { get; init; }

    public MergeShape? Merge { get; init; }

    /// <summary>A statement run after this one in the same transaction, which a rewrite into
    /// two statements compares as one.</summary>
    public DmlSpec? Then { get; init; }

    public bool TargetAliased => !Unaliased.Contains("u");

    /// <summary>The WHERE the statement renders: the pin, then <see cref="Where"/>, then a
    /// join condition the FROM and USING forms carry there.</summary>
    public SpecCondition? EffectiveWhere
    {
        get
        {
            List<SpecCondition> conjuncts = [];
            if (PinnedId is { } id)
            {
                conjuncts.Add(new Comparison("==", new ColumnRef("u", "id"), new Literal((int)id)));
            }

            if (Join is { Form: DmlJoinForm.From or DmlJoinForm.Using } join)
            {
                conjuncts.Add(join.On);
            }

            if (Where is not null)
            {
                conjuncts.Add(Where);
            }

            return conjuncts.Count switch
            {
                0 => null,
                1 => conjuncts[0],
                _ => new Junction(true, [.. conjuncts]),
            };
        }
    }

    /// <summary>The condition the planner negates and rewrites: an UPDATE's or DELETE's WHERE,
    /// an INSERT's feeding WHERE, an upsert's DO UPDATE WHERE, or a MERGE source's WHERE, else
    /// its first branch's condition.</summary>
    public SpecCondition? Filter => Kind switch
    {
        DmlKind.Insert => Insert!.Feed?.Where,
        DmlKind.Upsert => Upsert!.Where,
        DmlKind.Merge => Merge!.Source.Body?.Where
            ?? Merge.Branches
                .Select(b => b.When ?? b.ActionWhere)
                .FirstOrDefault(c => c is not null),
        _ => Where,
    };

    public DmlSpec WithFilter(SpecCondition filter) => Kind switch
    {
        DmlKind.Insert => this with
        {
            Insert = Insert! with { Feed = Insert.Feed! with { Where = filter } },
        },
        DmlKind.Upsert => this with { Upsert = Upsert! with { Where = filter } },
        DmlKind.Merge when Merge!.Source.Body is { Where: not null } body => this with
        {
            Merge = Merge with
            {
                Source = Merge.Source with { Body = body with { Where = filter } },
            },
        },
        DmlKind.Merge => this with { Merge = Merge with { Branches = WithBranchFilter(filter) } },
        _ => this with { Where = filter },
    };

    private MergeBranch[] WithBranchFilter(SpecCondition filter)
    {
        int index = Merge!.Branches.ToList().FindIndex(b => b.When is not null
            || b.ActionWhere is not null);
        return [.. Merge.Branches.Select((b, i) => i != index
            ? b
            : b.When is not null ? b with { When = filter } : b with { ActionWhere = filter })];
    }

    public DmlSpec Rewrite(SpecRewrite rewrite) => this with
    {
        Join = Join is null ? null : Join with { On = Join.On.Rewrite(rewrite) },
        Set = [.. Set.Select(a => a with { Value = a.Value.Rewrite(rewrite) })],
        Where = Where?.Rewrite(rewrite),
        Insert = Insert is null ? null : Insert with
        {
            Rows = [.. Insert.Rows.Select(r => (IReadOnlyList<SpecExpression>)
                [.. r.Select(e => e.Rewrite(rewrite))])],
            Feed = Insert.Feed?.Rewrite(rewrite),
        },
        OutputItems = [.. OutputItems.Select(e => e.Rewrite(rewrite))],
        Upsert = Upsert?.Rewrite(rewrite),
        Merge = Merge?.Rewrite(rewrite),
        Then = Then?.Rewrite(rewrite),
    };

    /// <summary>Every condition of the statement and of the blocks nested in it, in visit
    /// order.</summary>
    public List<SpecCondition> AllConditions()
    {
        List<SpecCondition> found = [];
        Rewrite(SpecRewrite.Conditions(c =>
        {
            found.Add(c);
            return c;
        }));
        return found;
    }

    /// <summary>The SELECT of the rows the statement reads: an UPDATE's or DELETE's target
    /// ids through the same join and WHERE, or an INSERT's feeding query.</summary>
    public QueryBlock? MatchQuery() => Kind switch
    {
        DmlKind.Insert => Insert!.Feed,
        DmlKind.Upsert => ConflictQuery(),
        DmlKind.Merge => null,
        _ => new QueryBlock
        {
            Items = [new ColumnRef("u", "id")],
            From = "u",
            Join = Join is null
                ? null
                : new JoinSpec(Join.IsInner || Join.Form is DmlJoinForm.From or DmlJoinForm.Using,
                    "d",
                    Join.On),
            Where = Join is { Form: DmlJoinForm.From or DmlJoinForm.Using }
                ? (this with { Join = null }).EffectiveWhere
                : EffectiveWhere,
        },
    };

    public PlannedStatement Planned(string label) => new(label, ToCSharp(), Build);

    /// <summary>The proposed rows' key literals: each row's id, then its code.</summary>
    public IReadOnlyList<(Literal Id, Literal? Code)> ProposedKeys() => [.. Insert!.Rows.Select(r =>
        ((Literal)r[IndexOf("id")], r[IndexOf("code")] as Literal))];

    private int IndexOf(string column) =>
        Insert!.Columns.ToList().IndexOf(column);

    // The existing rows an upsert's proposed keys meet under its arbiter, with their codes.
    private QueryBlock? ConflictQuery()
    {
        IReadOnlyList<(Literal Id, Literal? Code)> keys = ProposedKeys();
        List<SpecCondition> tests = [];
        if (Upsert!.Arbiter != ArbiterLevel.Code)
        {
            tests.Add(new InList(new ColumnRef("u", "id"), [.. keys.Select(k => k.Id)], false));
        }

        Literal[] codes = [.. keys.Select(k => k.Code).OfType<Literal>()];
        if (Upsert.Arbiter != ArbiterLevel.Id && codes.Length > 0)
        {
            tests.Add(new InList(new ColumnRef("u", "code"), codes, false));
        }

        return tests.Count == 0 ? null : new QueryBlock
        {
            Items = [new ColumnRef("u", "id"), new ColumnRef("u", "code")],
            From = "u",
            Where = tests.Count == 1 ? tests[0] : new Junction(false, [.. tests]),
        };
    }

    /// <summary>The SELECT of <see cref="MatchQuery"/>, under the statement's WITH.</summary>
    public PlannedStatement? PlannedMatch(string label)
    {
        if (Merge is { } merge)
        {
            return new PlannedStatement(
                label,
                Declarations() + "\n" + merge.MatchCSharp(),
                () => merge.BuildMatch(new BuildContext(Unaliased)));
        }

        if (MatchQuery() is not { } query)
        {
            return null;
        }

        string with = Cte is null ? "" : $"{(Cte.Recursive ? "WithRecursive" : "With")}("
            + Cte.ToCSharp() + ").";
        return new PlannedStatement(label, Declarations() + "\n" + with + query.ToCSharp(), () =>
        {
            BuildContext context = new(Unaliased);
            return Lead(context, inSelectToo: true) is ISelectBuilder head
                ? query.BuildStatement(context, head)
                : query.BuildStatement(context, out _);
        });
    }

    public ISqlBuilder Build()
    {
        BuildContext context = new(Unaliased);
        return Kind switch
        {
            DmlKind.Insert => BuildInsert(context),
            DmlKind.Update => BuildUpdate(context),
            DmlKind.Upsert => Upsert!.Build(
                Insert!,
                context,
                Output == DmlOutputLevel.Returning ? () => OutputObjects(context) : null),
            DmlKind.Merge => Merge!.Build(context),
            _ => BuildDelete(context),
        };
    }

    private object? Lead(BuildContext context, bool inSelectToo = false)
    {
        if (Cte is null || (CteInSelect && !inSelectToo))
        {
            return null;
        }

        return Cte.Recursive ? WithRecursive(Cte.Define(context)) : With(Cte.Define(context));
    }

    private ISqlBuilder BuildUpdate(BuildContext context)
    {
        DbTableBase t = context.Table("u");
        IUpdateBuilderUpdate update = Lead(context) is IUpdateBuilder w ? w.Update(t) : Update(t);
        EqualityCondition[] set = [.. Set.Select(a =>
            context.Column("u", a.Column) == a.Value.Build(context))];
        SqlCondition? where = EffectiveWhere?.Build(context);
        if (Join is { Form: DmlJoinForm.Direct } direct)
        {
            DbTableBase d = context.Table("d");
            IUpdateBuilderJoinOn on = direct.IsInner ? update.InnerJoin(d) : update.LeftJoin(d);
            IUpdateBuilderJoinedSet joined = on
                .On(direct.On.Build(context))
                .Set(set);
            return where is null ? joined : joined.Where(where);
        }

        IUpdateBuilderSetOutput setStage = update.Set(set);
        IUpdateBuilderSet afterOutput = Output switch
        {
            DmlOutputLevel.Output => setStage.Output(OutputObjects(context)),
            DmlOutputLevel.OutputInto => setStage.Output(OutputObjects(context))
                .Into(Destination, Destination.Id, Destination.Name),
            _ => setStage,
        };
        IReturning stage = Join switch
        {
            { Form: DmlJoinForm.From } => afterOutput.From(context.Table("d")).Where(where!),
            { Form: DmlJoinForm.FromRelisted } relisted => Relisted(
                afterOutput.From(t),
                relisted,
                context,
                where),
            _ => where is null ? afterOutput : afterOutput.Where(where),
        };
        return Returning(stage, context);
    }

    private static IReturning Relisted(
        IUpdateBuilderFrom from,
        DmlJoin join,
        BuildContext context,
        SqlCondition? where)
    {
        DbTableBase d = context.Table("d");
        IUpdateBuilderFrom joined = (join.IsInner ? from.InnerJoin(d) : from.LeftJoin(d))
            .On(join.On.Build(context));
        return where is null ? joined : joined.Where(where);
    }

    private ISqlBuilder BuildDelete(BuildContext context)
    {
        DbTableBase t = context.Table("u");
        IDeleteBuilderDeleteOutput delete =
            Lead(context) is IDeleteBuilder w ? w.DeleteFrom(t) : DeleteFrom(t);
        IDeleteBuilderDelete afterOutput = Output switch
        {
            DmlOutputLevel.Output => delete.Output(OutputObjects(context)),
            DmlOutputLevel.OutputInto => delete.Output(OutputObjects(context))
                .Into(Destination, Destination.Id, Destination.Name),
            _ => delete,
        };
        SqlCondition? where = EffectiveWhere?.Build(context);
        if (Join is { Form: DmlJoinForm.FromRelisted } relisted)
        {
            DbTableBase d = context.Table("d");
            IDeleteBuilderFrom joined = (relisted.IsInner
                    ? afterOutput.From(t).InnerJoin(d)
                    : afterOutput.From(t).LeftJoin(d))
                .On(relisted.On.Build(context));
            return where is null ? joined : joined.Where(where);
        }

        IReturning stage = Join is { Form: DmlJoinForm.Using }
            ? afterOutput.Using(context.Table("d")).Where(where!)
            : where is null ? afterOutput : afterOutput.Where(where);
        return Returning(stage, context);
    }

    private ISqlBuilder BuildInsert(BuildContext context)
    {
        InsertShape shape = Insert!;
        DbTableBase t = context.Table("u");
        DbColumn[] columns = [.. shape.Columns.Select(c => context.Column("u", c))];
        object? lead = Lead(context);
        if (!shape.ListsColumns)
        {
            IInsertBuilderTable table = lead is IInsertBuilder w ? w.InsertInto(t) : InsertInto(t);
            if (shape.Source == InsertSourceLevel.SetForm)
            {
                return Returning(
                    table.Set([.. columns.Zip(shape.Rows[0])
                        .Select(p => p.First == p.Second.Build(context))]),
                    context);
            }

            IInsertBuilderValues values = table.Values(Row(shape.Rows[0], context));
            return Returning(ChainRows(values, context), context);
        }

        IInsertBuilderColumnsOutput listed =
            lead is IInsertBuilder l ? l.InsertInto(t, columns) : InsertInto(t, columns);
        if (Output is DmlOutputLevel.Output or DmlOutputLevel.OutputInto)
        {
            IInsertBuilderOutputInto output = listed.Output(OutputObjects(context));
            IInsertBuilderOutputColumns body = Output == DmlOutputLevel.OutputInto
                ? output.Into(Destination, Destination.Id, Destination.Name)
                : output;
            return shape.Feed is { } feed
                ? feed.BuildStatement(context, body)
                : ChainRows(body.Values(Row(shape.Rows[0], context)), context);
        }

        if (shape.Feed is { } select)
        {
            ISelectBuilder head = !CteInSelect
                ? listed
                : Cte!.Recursive
                    ? listed.WithRecursive(Cte.Define(context))
                    : listed.With(Cte.Define(context));
            return select.BuildStatement(context, head);
        }

        return Returning(ChainRows(listed.Values(Row(shape.Rows[0], context)), context), context);
    }

    private IInsertBuilderValues ChainRows(IInsertBuilderValues first, BuildContext context)
    {
        IInsertBuilderValues values = first;
        foreach (IReadOnlyList<SpecExpression> row in Insert!.Rows.Skip(1))
        {
            values = values.Values(Row(row, context));
        }

        return values;
    }

    private static object?[] Row(IReadOnlyList<SpecExpression> row, BuildContext context) =>
        [.. row.Select(e => e.Build(context))];

    private object[] OutputObjects(BuildContext context) =>
        [.. OutputItems.Select(e => e.Build(context))];

    private ISqlBuilder Returning(IReturning stage, BuildContext context)
    {
        if (Output != DmlOutputLevel.Returning)
        {
            return stage;
        }

        IReturningBuilder returning = stage.Returning(OutputObjects(context));
        return Dbms == Dbms.Oracle ? returning.Into(OutputParameters()) : returning;
    }

    // Oracle's RETURNING takes INTO (ContextRules.CheckReturningRequiresInto), one output
    // parameter per item, so it reads one row.
    private OutputParameter[] OutputParameters() => [.. OutputItems.Select((e, i) => e.Type
        == SpecType.Text
            ? new OutputParameter(OutputName(i), DbType.String, 100)
            : new OutputParameter(OutputName(i), DbType.Int32))];

    private static string OutputName(int i) => "o" + i.ToString(CultureInfo.InvariantCulture);

    /// <summary>The <c>OUTPUT ... INTO</c> destination: <c>rt_depts</c>, unaliased as SQL
    /// Server requires, receiving <c>(id + 1000, name)</c> rows no seed id collides with.</summary>
    public static RandomDeptsTable Destination { get; } = new("");

    public const int DestinationIdOffset = 1000;

    public string ToCSharp() => Declarations() + "\n" + Kind switch
    {
        DmlKind.Insert => InsertCSharp(),
        DmlKind.Update => UpdateCSharp(),
        DmlKind.Upsert => Upsert!.ToCSharp(Insert!),
        DmlKind.Merge => Merge!.ToCSharp(),
        _ => DeleteCSharp(),
    } + ReturningCSharp();

    private string Declarations()
    {
        if (Kind is DmlKind.Upsert or DmlKind.Merge || Then is not null
            || Insert?.Feed?.From is "d6" or "u5" or "d5" or "u7")
        {
            string[] keys =
                ["u", "u2", "d2", "u3", "d3", "u4", "d4", "u5", "d5", "u6", "d6", "u7"];
            return string.Join(" ", keys.Select(k =>
                $"{(k[0] == 'u' ? "RandomUsersTable" : "RandomDeptsTable")} {k} = "
                    + $"new({(Unaliased.Contains(k) ? "\"\"" : $"\"{k}\"")});"))
                + (Merge is null ? "" : "\n" + Merge.Source.Declaration());
        }

        string tables = string.Join(" ", QuerySpec.TableKeys.Concat(["u4", "d4"]).Select(k =>
            $"{(k[0] == 'u' ? "RandomUsersTable" : "RandomDeptsTable")} {k} = "
                + $"new({(Unaliased.Contains(k) ? "\"\"" : $"\"{k}\"")});"));
        return tables + (Cte is null ? "" : " Cte c = new(\"c\");")
            + (Output == DmlOutputLevel.OutputInto ? " RandomDeptsTable dd = new(\"\");" : "");
    }

    private string LeadCSharp(string statement) => Cte is null || CteInSelect
        ? statement
        : $"{(Cte.Recursive ? "WithRecursive" : "With")}({Cte.ToCSharp()}).{statement}";

    private string OutputCSharp() => Output switch
    {
        DmlOutputLevel.Output => $".Output({Items()})",
        DmlOutputLevel.OutputInto => $".Output({Items()}).Into(dd, dd.Id, dd.Name)",
        _ => "",
    };

    private string ReturningCSharp()
    {
        if (Output != DmlOutputLevel.Returning)
        {
            return "";
        }

        string into = Dbms != Dbms.Oracle
            ? ""
            : $".Into({string.Join(", ", OutputItems.Select((e, i) => e.Type == SpecType.Text
                ? $"new(\"{OutputName(i)}\", DbType.String, 100)"
                : $"new(\"{OutputName(i)}\", DbType.Int32)"))})";
        return $".Returning({Items()}){into}";
    }

    private string Items() => string.Join(", ", OutputItems.Select(e => e.ToCSharp()));

    private string WhereCSharp() => EffectiveWhere is { } w ? $".Where({w.ToCSharp()})" : "";

    private string UpdateCSharp()
    {
        string set = $".Set({string.Join(", ", Set.Select(a =>
            $"{new ColumnRef("u", a.Column).ToCSharp()} == {a.Value.ToCSharp()}"))})";
        string join = Join switch
        {
            { Form: DmlJoinForm.Direct } j =>
                $".{(j.IsInner ? "Inner" : "Left")}Join(d).On({j.On.ToCSharp()})" + set,
            { Form: DmlJoinForm.From } => set + OutputCSharp() + ".From(d)",
            { Form: DmlJoinForm.FromRelisted } j => set + OutputCSharp()
                + $".From(u).{(j.IsInner ? "Inner" : "Left")}Join(d).On({j.On.ToCSharp()})",
            _ => set + OutputCSharp(),
        };
        return LeadCSharp("Update(u)" + join + WhereCSharp());
    }

    private string DeleteCSharp()
    {
        string join = Join switch
        {
            { Form: DmlJoinForm.Using } => ".Using(d)",
            { Form: DmlJoinForm.FromRelisted } j =>
                $".From(u).{(j.IsInner ? "Inner" : "Left")}Join(d).On({j.On.ToCSharp()})",
            _ => "",
        };
        return LeadCSharp("DeleteFrom(u)" + OutputCSharp() + join + WhereCSharp());
    }

    private string InsertCSharp()
    {
        InsertShape shape = Insert!;
        string columns = string.Join(", ", shape.Columns.Select(c => new ColumnRef("u", c)
            .ToCSharp()));
        string head = shape.ListsColumns ? $"InsertInto(u, {columns})" : "InsertInto(u)";
        string body;
        if (shape.Source == InsertSourceLevel.SetForm)
        {
            body = $".Set({string.Join(", ", shape.Columns.Zip(shape.Rows[0]).Select(p =>
                $"{new ColumnRef("u", p.First).ToCSharp()} == {p.Second.ToCSharp()}"))})";
        }
        else if (shape.Feed is { } feed)
        {
            string with = CteInSelect
                ? $".{(Cte!.Recursive ? "WithRecursive" : "With")}({Cte.ToCSharp()})"
                : "";
            body = with + "." + feed.ToCSharp();
        }
        else
        {
            body = string.Concat(shape.Rows.Select(r =>
                $".Values({string.Join(", ", r.Select(e => e.ToCSharp()))})"));
        }

        return LeadCSharp(head + OutputCSharp() + body);
    }
}
