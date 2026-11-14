using System.Globalization;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Randomized;

internal enum SpecType { Int, Text }

/// <summary>
/// The table instances of one build. A key is <c>u</c>/<c>d</c> at the top level and
/// <c>u2</c>/<c>d2</c>/<c>u3</c>/<c>d3</c> inside subqueries; it is also the alias, unless the
/// spec leaves the key unaliased.
/// </summary>
internal sealed class BuildContext
{
    private readonly Dictionary<string, DbTableBase> _tables = [];
    private readonly IReadOnlySet<string> _unaliased;
    private Cte? _cte;

    public BuildContext(IReadOnlySet<string> unaliased)
    {
        _unaliased = unaliased;
    }

    public DbTableBase Table(string key)
    {
        if (!_tables.TryGetValue(key, out DbTableBase? table))
        {
            string alias = _unaliased.Contains(key) ? "" : key;
            table = key[0] == 'u' ? new RandomUsersTable(alias) : new RandomDeptsTable(alias);
            _tables[key] = table;
        }

        return table;
    }

    public const string CteColumnName = "c0";

    /// <summary>The one CTE a DML statement defines, shared by its WITH and its
    /// references.</summary>
    public Cte Cte() => _cte ??= new Cte("c");

    // Through an alias, so the reference is quoted as the definition's As("c0") is: Oracle
    // folds a bare c0 to C0, which a quoted "c0" no longer matches.
    public DbColumn CteColumn() => Cte().Column(Null.As(CteColumnName));

    /// <summary>A MERGE source's columns by name, which <see cref="Column"/> serves for
    /// <see cref="SourceKey"/>.</summary>
    public Func<string, DbColumn>? Source { get; set; }

    /// <summary>The key of a MERGE source's row.</summary>
    public const string SourceKey = "s";

    /// <summary>The key of the row an upsert proposes, which <c>Excluded(...)</c> reads.</summary>
    public const string ExcludedKey = "x";

    public DbColumn Column(string key, string column) => key == SourceKey && Source is { } source
        ? source(column)
        : Table(key) switch
        {
            RandomUsersTable u => column switch
            {
                "id" => u.Id,
                "name" => u.Name,
                "age" => u.Age,
                "code" => u.Code,
                _ => u.DeptId,
            },
            RandomDeptsTable d => column == "id" ? d.Id : d.Name,
            _ => throw new InvalidOperationException(key),
        };
}

/// <summary>Rewrites a spec bottom-up: each node is rebuilt from its rewritten children, then
/// handed to the matching callback.</summary>
internal sealed record SpecRewrite(
    Func<SpecExpression, SpecExpression> OnExpression,
    Func<SpecCondition, SpecCondition> OnCondition)
{
    public static SpecRewrite Conditions(Func<SpecCondition, SpecCondition> onCondition) =>
        new(e => e, onCondition);

    public static SpecRewrite Expressions(Func<SpecExpression, SpecExpression> onExpression) =>
        new(onExpression, c => c);
}

internal abstract record SpecExpression
{
    public abstract SpecType Type { get; }

    public virtual bool HasAggregate => false;

    public abstract object Build(BuildContext context);

    public abstract string ToCSharp();

    public virtual SpecExpression Rewrite(SpecRewrite rewrite) => rewrite.OnExpression(this);

    // A bare value on the left of an operator would bind C#'s, not SqlArtisan's.
    public SqlExpression BuildOperand(BuildContext context) => Build(context) switch
    {
        SqlExpression expression => expression,
        object value => Bind(value),
    };

    public string ToCSharpOperand() =>
        this is Literal { IsBound: false } literal ? $"Bind({literal.Text})" : ToCSharp();
}

internal sealed record ColumnRef(string Table, string Column) : SpecExpression
{
    public override SpecType Type => Column == "name" ? SpecType.Text : SpecType.Int;

    // The proposed row's key reads through Excluded(...) on the target's column.
    public override object Build(BuildContext context) => Table == BuildContext.ExcludedKey
        ? Excluded(context.Column("u", Column))
        : context.Column(Table, Column);

    public override string ToCSharp()
    {
        string member = Column switch
        {
            "id" => "Id",
            "name" => "Name",
            "age" => "Age",
            "code" => "Code",
            _ => "DeptId",
        };
        return Table == BuildContext.ExcludedKey ? $"Excluded(u.{member})" : Table + "." + member;
    }
}

internal sealed record Literal(object Value, bool IsBound = false) : SpecExpression
{
    public override SpecType Type => Value is string ? SpecType.Text : SpecType.Int;

    public string Text => Value is string s
        ? "\"" + s + "\""
        : Convert.ToString(Value, CultureInfo.InvariantCulture)!;

    public override object Build(BuildContext context) => IsBound ? Bind(Value) : Value;

    public override string ToCSharp() => IsBound ? $"Bind({Text})" : Text;
}

internal sealed record FunctionCall(string Name, SpecType ResultType, SpecExpression[] Arguments)
    : SpecExpression
{
    public override SpecType Type => ResultType;

    public override bool HasAggregate => Arguments.Any(a => a.HasAggregate);

    public override SpecExpression Rewrite(SpecRewrite rewrite) => rewrite.OnExpression(
        this with { Arguments = [.. Arguments.Select(a => a.Rewrite(rewrite))] });

    public override object Build(BuildContext context)
    {
        object[] a = [.. Arguments.Select(x => x.Build(context))];
        return Name switch
        {
            "Coalesce" => Coalesce(a[0], a[1]),
            "Abs" => Abs(a[0]),
            "Upper" => Upper(a[0]),
            "Lower" => Lower(a[0]),
            "Trim" => Trim(a[0]),
            "Length" => Length(a[0]),
            "Substr" => Substr(a[0], a[1], a[2]),
            "Substring" => Substring(a[0], a[1], a[2]),
            "Replace" => Replace(a[0], a[1], a[2]),
            "Nullif" => Nullif(a[0], a[1]),
            "+" => Arguments[0].BuildOperand(context) + a[1],
            "-" => Arguments[0].BuildOperand(context) - a[1],
            "*" => Arguments[0].BuildOperand(context) * a[1],
            _ => throw new InvalidOperationException(Name),
        };
    }

    public override string ToCSharp() => Name is "+" or "-" or "*"
        ? $"({Arguments[0].ToCSharpOperand()} {Name} {Arguments[1].ToCSharp()})"
        : $"{Name}({string.Join(", ", Arguments.Select(x => x.ToCSharp()))})";
}

internal sealed record AggregateCall(string Kind, SpecExpression? Argument, bool IsDistinct)
    : SpecExpression
{
    public override SpecType Type =>
        Kind is "Max" or "Min" && Argument!.Type == SpecType.Text ? SpecType.Text : SpecType.Int;

    public override bool HasAggregate => true;

    public override SpecExpression Rewrite(SpecRewrite rewrite) =>
        rewrite.OnExpression(this with { Argument = Argument?.Rewrite(rewrite) });

    public override object Build(BuildContext context)
    {
        if (Argument is null)
        {
            return Count(Asterisk);
        }

        object a = Argument.Build(context);
        return (Kind, IsDistinct) switch
        {
            ("Count", false) => Count(a),
            ("Count", true) => Count(Distinct, a),
            ("Sum", false) => Sum(a),
            ("Sum", true) => Sum(Distinct, a),
            ("Max", _) => Max(a),
            ("Min", _) => Min(a),
            _ => throw new InvalidOperationException(Kind),
        };
    }

    public override string ToCSharp() => Argument is null
        ? "Count(Asterisk)"
        : IsDistinct
            ? $"{Kind}(Distinct, {Argument.ToCSharp()})"
            : $"{Kind}({Argument.ToCSharp()})";
}

internal sealed record CaseExpression(SpecCondition When, SpecExpression Then, SpecExpression Else)
    : SpecExpression
{
    public override SpecType Type => Then.Type;

    public override bool HasAggregate => Then.HasAggregate || Else.HasAggregate;

    public override SpecExpression Rewrite(SpecRewrite rewrite) => rewrite.OnExpression(
        new CaseExpression(When.Rewrite(rewrite), Then.Rewrite(rewrite), Else.Rewrite(rewrite)));

    public override object Build(BuildContext context) => Case(
        Sql.When(When.Build(context)).Then(Then.Build(context)),
        Sql.Else(Else.Build(context)));

    public override string ToCSharp() =>
        $"Case(When({When.ToCSharp()}).Then({Then.ToCSharp()}), Else({Else.ToCSharp()}))";
}

internal sealed record ScalarSubquery(QueryBlock Query) : SpecExpression
{
    public override SpecType Type => Query.Items[0].Type;

    public override SpecExpression Rewrite(SpecRewrite rewrite) =>
        rewrite.OnExpression(new ScalarSubquery(Query.Rewrite(rewrite)));

    public override object Build(BuildContext context) => Query.Build(context);

    public override string ToCSharp() => Query.ToCSharp();
}

/// <summary>An ORDER BY key with an explicit direction or NULL placement.</summary>
internal sealed record SortKey(SpecExpression Key, bool Descending, NullsPlacement Nulls)
    : SpecExpression
{
    public override SpecType Type => Key.Type;

    public override SpecExpression Rewrite(SpecRewrite rewrite) =>
        rewrite.OnExpression(this with { Key = Key.Rewrite(rewrite) });

    public override object Build(BuildContext context)
    {
        SqlExpression key = Key.BuildOperand(context);
        SortOrder order = Descending ? key.Desc : key.Asc;
        return Nulls switch
        {
            NullsPlacement.First => Descending ? order.NullsFirst : key.NullsFirst,
            NullsPlacement.Last => Descending ? order.NullsLast : key.NullsLast,
            _ => order,
        };
    }

    public override string ToCSharp() => Key.ToCSharp()
        + (Descending ? ".Desc" : Nulls == NullsPlacement.Default ? ".Asc" : "")
        + Nulls switch
        {
            NullsPlacement.First => ".NullsFirst",
            NullsPlacement.Last => ".NullsLast",
            _ => "",
        };
}

internal enum NullsPlacement { Default, First, Last }

/// <summary>A select-list position, the one sort key a set operator's ORDER BY takes on every
/// engine.</summary>
internal sealed record OrdinalKey(int Position) : SpecExpression
{
    public override SpecType Type => SpecType.Int;

    public override object Build(BuildContext context) => Position;

    public override string ToCSharp() => Position.ToString(CultureInfo.InvariantCulture);
}

internal abstract record SpecCondition
{
    public virtual bool HasAggregate => false;

    public abstract SqlCondition Build(BuildContext context);

    public abstract string ToCSharp();

    public abstract SpecCondition Rewrite(SpecRewrite rewrite);

    /// <summary>Whether <c>ConditionIf(false, ...)</c> drops this whole operand.</summary>
    public bool IsExcluded => this switch
    {
        ConditionIfTest { Include: false } => true,
        ConditionIfTest t => t.Operand.IsExcluded,
        Junction j => j.Operands.All(o => o.IsExcluded),
        Negation n => n.Operand.IsExcluded,
        _ => false,
    };
}

internal sealed record Comparison(string Operator, SpecExpression Left, SpecExpression Right)
    : SpecCondition
{
    public override bool HasAggregate => Left.HasAggregate || Right.HasAggregate;

    public override SpecCondition Rewrite(SpecRewrite rewrite) => rewrite.OnCondition(
        new Comparison(Operator, Left.Rewrite(rewrite), Right.Rewrite(rewrite)));

    public override SqlCondition Build(BuildContext context)
    {
        SqlExpression l = Left.BuildOperand(context);
        object r = Right.Build(context);
        return Operator switch
        {
            "==" => l == r,
            "!=" => l != r,
            "<" => l < r,
            ">" => l > r,
            "<=" => l <= r,
            ">=" => l >= r,
            _ => throw new InvalidOperationException(Operator),
        };
    }

    public override string ToCSharp() =>
        $"{Left.ToCSharpOperand()} {Operator} {Right.ToCSharp()}";
}

internal sealed record NullTest(SpecExpression Operand, bool Negated) : SpecCondition
{
    public override bool HasAggregate => Operand.HasAggregate;

    public override SpecCondition Rewrite(SpecRewrite rewrite) =>
        rewrite.OnCondition(this with { Operand = Operand.Rewrite(rewrite) });

    public override SqlCondition Build(BuildContext context) => Negated
        ? Operand.BuildOperand(context).IsNotNull
        : Operand.BuildOperand(context).IsNull;

    public override string ToCSharp() =>
        $"{Operand.ToCSharpOperand()}.{(Negated ? "IsNotNull" : "IsNull")}";
}

internal sealed record InList(SpecExpression Operand, Literal[] Values, bool Negated)
    : SpecCondition
{
    public override SpecCondition Rewrite(SpecRewrite rewrite) => rewrite.OnCondition(
        new InList(
            Operand.Rewrite(rewrite),
            [.. Values.Select(v => (Literal)v.Rewrite(rewrite))],
            Negated));

    public override SqlCondition Build(BuildContext context)
    {
        object[] values = [.. Values.Select(v => v.Build(context))];
        return Negated
            ? Operand.BuildOperand(context).NotIn(values)
            : Operand.BuildOperand(context).In(values);
    }

    public override string ToCSharp() => $"{Operand.ToCSharpOperand()}."
        + $"{(Negated ? "NotIn" : "In")}({string.Join(", ", Values.Select(v => v.ToCSharp()))})";
}

internal sealed record InSubquery(SpecExpression Operand, QueryBlock Query, bool Negated)
    : SpecCondition
{
    public override SpecCondition Rewrite(SpecRewrite rewrite) => rewrite.OnCondition(
        new InSubquery(Operand.Rewrite(rewrite), Query.Rewrite(rewrite), Negated));

    public override SqlCondition Build(BuildContext context) => Negated
        ? Operand.BuildOperand(context).NotIn(Query.Build(context))
        : Operand.BuildOperand(context).In(Query.Build(context));

    public override string ToCSharp() =>
        $"{Operand.ToCSharpOperand()}.{(Negated ? "NotIn" : "In")}({Query.ToCSharp()})";
}

internal sealed record LikeTest(SpecExpression Operand, Literal Pattern, bool Negated)
    : SpecCondition
{
    public override SpecCondition Rewrite(SpecRewrite rewrite) => rewrite.OnCondition(
        new LikeTest(Operand.Rewrite(rewrite), (Literal)Pattern.Rewrite(rewrite), Negated));

    public override SqlCondition Build(BuildContext context) => Negated
        ? Operand.BuildOperand(context).NotLike(Pattern.Build(context))
        : Operand.BuildOperand(context).Like(Pattern.Build(context));

    public override string ToCSharp() =>
        $"{Operand.ToCSharpOperand()}.{(Negated ? "NotLike" : "Like")}({Pattern.ToCSharp()})";
}

internal sealed record BetweenTest(
    SpecExpression Operand,
    SpecExpression Low,
    SpecExpression High,
    bool Negated) : SpecCondition
{
    public override SpecCondition Rewrite(SpecRewrite rewrite) => rewrite.OnCondition(
        new BetweenTest(
            Operand.Rewrite(rewrite),
            Low.Rewrite(rewrite),
            High.Rewrite(rewrite),
            Negated));

    public override SqlCondition Build(BuildContext context) => Negated
        ? Operand.BuildOperand(context).NotBetween(Low.Build(context), High.Build(context))
        : Operand.BuildOperand(context).Between(Low.Build(context), High.Build(context));

    public override string ToCSharp() => $"{Operand.ToCSharpOperand()}."
        + $"{(Negated ? "NotBetween" : "Between")}({Low.ToCSharp()}, {High.ToCSharp()})";
}

internal sealed record Junction(bool IsAnd, SpecCondition[] Operands) : SpecCondition
{
    public override bool HasAggregate => Operands.Any(o => o.HasAggregate);

    public override SpecCondition Rewrite(SpecRewrite rewrite) => rewrite.OnCondition(
        new Junction(IsAnd, [.. Operands.Select(o => o.Rewrite(rewrite))]));

    public override SqlCondition Build(BuildContext context)
    {
        SqlCondition result = Operands[0].Build(context);
        for (int i = 1; i < Operands.Length; i++)
        {
            SqlCondition next = Operands[i].Build(context);
            result = IsAnd ? result & next : result | next;
        }

        return result;
    }

    public override string ToCSharp() => "("
        + string.Join(IsAnd ? " & " : " | ", Operands.Select(o => "(" + o.ToCSharp() + ")"))
        + ")";
}

internal sealed record Negation(SpecCondition Operand) : SpecCondition
{
    public override bool HasAggregate => Operand.HasAggregate;

    public override SpecCondition Rewrite(SpecRewrite rewrite) =>
        rewrite.OnCondition(new Negation(Operand.Rewrite(rewrite)));

    public override SqlCondition Build(BuildContext context) => Not(Operand.Build(context));

    public override string ToCSharp() => $"Not({Operand.ToCSharp()})";
}

internal sealed record ConditionIfTest(bool Include, SpecCondition Operand) : SpecCondition
{
    public override bool HasAggregate => Operand.HasAggregate;

    public override SpecCondition Rewrite(SpecRewrite rewrite) =>
        rewrite.OnCondition(this with { Operand = Operand.Rewrite(rewrite) });

    public override SqlCondition Build(BuildContext context) =>
        ConditionIf(Include, Operand.Build(context));

    public override string ToCSharp() =>
        $"ConditionIf({(Include ? "true" : "false")}, {Operand.ToCSharp()})";
}

internal sealed record ExistsTest(QueryBlock Query, bool Negated) : SpecCondition
{
    public override SpecCondition Rewrite(SpecRewrite rewrite) =>
        rewrite.OnCondition(this with { Query = Query.Rewrite(rewrite) });

    public override SqlCondition Build(BuildContext context) => Negated
        ? NotExists(Query.Build(context))
        : Exists(Query.Build(context));

    public override string ToCSharp() =>
        $"{(Negated ? "NotExists" : "Exists")}({Query.ToCSharp()})";
}

internal sealed record JoinSpec(bool IsInner, string Table, SpecCondition On);

internal sealed record SetBranch(SetOperatorLevel Operator, QueryBlock Query);

internal sealed record RowLimit(PagingLevel Form, int Count, int Offset);

/// <summary>One query block: a top-level statement, a subquery, or a set operator's
/// branch.</summary>
internal sealed record QueryBlock
{
    public bool IsDistinct { get; init; }

    public IReadOnlyList<SpecExpression> Items { get; init; } = [];

    public string From { get; init; } = "u";

    public JoinSpec? Join { get; init; }

    public SpecCondition? Where { get; init; }

    public IReadOnlyList<SpecExpression> GroupBy { get; init; } = [];

    public SpecCondition? Having { get; init; }

    public IReadOnlyList<SetBranch> Tail { get; init; } = [];

    public IReadOnlyList<SpecExpression> OrderBy { get; init; } = [];

    public RowLimit? Limit { get; init; }

    /// <summary>Whether each select item takes a <c>c0</c>, <c>c1</c>, ... alias, which a
    /// derived table or CTE wrapping the block reads its columns by.</summary>
    public bool AliasesItems { get; init; }

    /// <summary>The aliases <see cref="AliasesItems"/> gives in place of <c>c0</c>,
    /// <c>c1</c>, ...</summary>
    public IReadOnlyList<string>? ItemNames { get; init; }

    private string ItemName(int i) =>
        ItemNames?[i] ?? "c" + i.ToString(CultureInfo.InvariantCulture);

    public IReadOnlyList<string> Tables => Join is null ? [From] : [From, Join.Table];

    public QueryBlock Rewrite(SpecRewrite rewrite) => this with
    {
        Items = [.. Items.Select(e => e.Rewrite(rewrite))],
        Join = Join is null ? null : Join with { On = Join.On.Rewrite(rewrite) },
        Where = Where?.Rewrite(rewrite),
        GroupBy = [.. GroupBy.Select(e => e.Rewrite(rewrite))],
        Having = Having?.Rewrite(rewrite),
        OrderBy = [.. OrderBy.Select(e => e.Rewrite(rewrite))],
        Tail = [.. Tail.Select(t => t with { Query = t.Query.Rewrite(rewrite) })],
    };

    /// <summary>Every condition of the block and of the blocks nested in it, in visit
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

    public ISubquery Build(BuildContext context) => (ISubquery)BuildStage(context, null, out _);

    public ISqlBuilder BuildStatement(BuildContext context, out ExpressionAlias[] aliases) =>
        (ISqlBuilder)BuildStage(context, null, out aliases);

    /// <summary>The block as the SELECT that <paramref name="head"/> leads: a WITH, or an
    /// <c>INSERT</c> it feeds.</summary>
    public ISqlBuilder BuildStatement(BuildContext context, ISelectBuilder head) =>
        (ISqlBuilder)BuildStage(context, head, out _);

    private object BuildStage(
        BuildContext context,
        ISelectBuilder? head,
        out ExpressionAlias[] aliases)
    {
        aliases = new ExpressionAlias[Items.Count];
        object[] items = new object[Items.Count];
        for (int i = 0; i < Items.Count; i++)
        {
            object item = Items[i].Build(context);
            if (AliasesItems)
            {
                string name = ItemName(i);
                aliases[i] = item switch
                {
                    ISubquery s => s.As(name),
                    SqlExpression x => x.As(name),
                    _ => Bind(item).As(name),
                };
                item = aliases[i];
            }

            items[i] = item;
        }

        ISelectBuilderSelect select = SelectHead(head, items);
        ISelectBuilderFrom from = select.From(context.Table(From));
        if (Join is not null)
        {
            ISelectBuilderJoin join = Join.IsInner
                ? from.InnerJoin(context.Table(Join.Table))
                : from.LeftJoin(context.Table(Join.Table));
            from = join.On(Join.On.Build(context));
        }

        object stage = Where is null ? from : from.Where(Where.Build(context));
        if (GroupBy.Count > 0)
        {
            object[] keys = [.. GroupBy.Select(e => e.Build(context))];
            ISelectBuilderGroupBy grouped = stage is ISelectBuilderWhere w
                ? w.GroupBy(keys)
                : from.GroupBy(keys);
            stage = Having is null ? grouped : grouped.Having(Having.Build(context));
        }

        foreach (SetBranch branch in Tail)
        {
            ISetOperator left = (ISetOperator)stage;
            ISelectBuilderSetOperator right = branch.Operator switch
            {
                SetOperatorLevel.Union => left.Union,
                SetOperatorLevel.UnionAll => left.UnionAll,
                SetOperatorLevel.Intersect => left.Intersect,
                SetOperatorLevel.Except => left.Except,
                _ => throw new InvalidOperationException(branch.Operator.ToString()),
            };
            stage = branch.Query.BuildStage(context, right, out _);
        }

        if (OrderBy.Count > 0)
        {
            object[] keys = [.. OrderBy.Select(e => e.Build(context))];
            stage = stage switch
            {
                ISelectBuilderWhere w => w.OrderBy(keys),
                ISelectBuilderHaving h => h.OrderBy(keys),
                ISelectBuilderGroupBy g => g.OrderBy(keys),
                ISelectBuilderFrom f => f.OrderBy(keys),
                _ => throw new InvalidOperationException(stage.GetType().Name),
            };
        }

        return Limit is null ? stage : ApplyLimit((IPagination)stage, Limit);
    }

    private ISelectBuilderSelect SelectHead(ISelectBuilder? head, object[] items)
    {
        if (Limit is { Form: PagingLevel.Top } top)
        {
            return IsDistinct
                ? Select(Distinct, Top(top.Count), items)
                : Select(Top(top.Count), items);
        }

        if (head is null)
        {
            return IsDistinct ? Select(Distinct, items) : Select(items);
        }

        return IsDistinct ? head.Select(Distinct, items) : head.Select(items);
    }

    private static object ApplyLimit(IPagination stage, RowLimit limit) => limit.Form switch
    {
        PagingLevel.Limit => stage.Limit(limit.Count),
        PagingLevel.LimitOffset => stage.Limit(limit.Count).Offset(limit.Offset),
        PagingLevel.FetchFirst => stage.FetchFirst(limit.Count),
        PagingLevel.OffsetFetch => stage.OffsetRows(limit.Offset).FetchNext(limit.Count),
        PagingLevel.Top => stage,
        _ => throw new InvalidOperationException(limit.Form.ToString()),
    };

    public string ToCSharp()
    {
        string items = string.Join(", ", Items.Select((e, i) => AliasesItems
            ? $"{(e is Literal l ? $"Bind({l.Text})" : e.ToCSharp())}.As(\"{ItemName(i)}\")"
            : e.ToCSharp()));
        string top = Limit is { Form: PagingLevel.Top } t ? $"Top({t.Count}), " : "";
        string text = IsDistinct ? $"Select(Distinct, {top}{items})" : $"Select({top}{items})";
        text += $".From({From})";
        if (Join is not null)
        {
            text += $".{(Join.IsInner ? "Inner" : "Left")}Join({Join.Table})"
                + $".On({Join.On.ToCSharp()})";
        }

        if (Where is not null)
        {
            text += $".Where({Where.ToCSharp()})";
        }

        if (GroupBy.Count > 0)
        {
            text += $".GroupBy({string.Join(", ", GroupBy.Select(e => e.ToCSharp()))})";
        }

        if (Having is not null)
        {
            text += $".Having({Having.ToCSharp()})";
        }

        text += string.Concat(Tail.Select(b => $".{b.Operator}.{b.Query.ToCSharp()}"));
        if (OrderBy.Count > 0)
        {
            text += $".OrderBy({string.Join(", ", OrderBy.Select(e => e.ToCSharp()))})";
        }

        return text + Limit switch
        {
            { Form: PagingLevel.Limit } l => $".Limit({l.Count})",
            { Form: PagingLevel.LimitOffset } l => $".Limit({l.Count}).Offset({l.Offset})",
            { Form: PagingLevel.FetchFirst } l => $".FetchFirst({l.Count})",
            { Form: PagingLevel.OffsetFetch } l => $".OffsetRows({l.Offset}).FetchNext({l.Count})",
            _ => "",
        };
    }
}

/// <summary>A generated top-level query and the table keys it leaves unaliased.</summary>
internal sealed record QuerySpec(QueryBlock Query, IReadOnlySet<string> Unaliased)
{
    public static readonly string[] TableKeys = ["u", "d", "u2", "d2", "u3", "d3"];

    public QuerySpec With(QueryBlock query) => this with { Query = query };

    public string ToCSharp()
    {
        string tables = string.Join(" ", TableKeys.Select(k =>
            $"{(k[0] == 'u' ? "RandomUsersTable" : "RandomDeptsTable")} {k} = "
                + $"new({(Unaliased.Contains(k) ? "\"\"" : $"\"{k}\"")});"));
        return tables + "\n" + Query.ToCSharp();
    }
}
