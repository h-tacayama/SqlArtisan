using CsCheck;

namespace SqlArtisan.IntegrationTests.Randomized;

internal enum Correlation { Never, Optional, Required }

/// <summary>What a generator may reference and nest at one query block.</summary>
/// <param name="Tables">The table keys this block reads.</param>
/// <param name="Outer">The top-level table keys a subquery may correlate to.</param>
/// <param name="Level">1 at the top, 2 and 3 inside subqueries.</param>
internal sealed record GenScope(
    Dbms Target,
    IReadOnlyList<string> Tables,
    IReadOnlyList<string> Outer,
    int Level,
    bool AllowsSubquery,
    Correlation Correlation);

/// <summary>CsCheck generators for a <see cref="QuerySpec"/> that fits one
/// <see cref="CaseRow"/>.</summary>
internal static class QueryGenerators
{
    private static readonly int[] s_ints = [-5, 0, 1, 2, 3, 5, 10, 20, 25, 30, 40, 99];

    // Every literal stays apart from the data ignoring case, as the data does (RandomSchema).
    private static readonly string[] s_texts =
        ["Alice", "Bob", "Eve", "Zed", "Ivy", "Bo", "A_b", "50%", "Sales", "Eng", "Xu"];

    private static readonly string[] s_patterns =
        ["A%", "%b%", "%e", "_o%", "%", "B__", "%_%", "Bob", "%l%", "a%"];

    private static readonly string[] s_intOperators = ["==", "!=", "<", ">", "<=", ">="];

    // Text compares by equality only: an ordering would read each engine's collation.
    private static readonly string[] s_textOperators = ["==", "!="];

    /// <summary>The queries that fit <paramref name="row"/>, sized by their C# text so a
    /// shrink lands on the shortest failing chain.</summary>
    public static Gen<QuerySpec> For(CaseRow row) => new SizedByText(Spec(row));

    private static Gen<QuerySpec> Spec(CaseRow row)
    {
        bool usersOnly = row.OrderBy is OrderByLevel.NullsFirst or OrderByLevel.NullsLast;
        return
            from fromDepts in usersOnly ? Gen.Const(false) : Chance(20)
            let source = fromDepts ? "d" : "u"
            let tables = row.Join == JoinLevel.None
                ? new[] { source }
                : [source, fromDepts ? "u" : "d"]
            let scope = new GenScope(
                row.Dbms,
                tables,
                tables,
                1,
                AllowsSubquery: false,
                row.Correlated ? Correlation.Required : Correlation.Never)
            from joinSpec in JoinCondition(scope, row.Join)
            from filter in TopWhere(scope, row)
            from body in Body(scope, row)
            from order in Order(scope, row, body)
            from limit in Limit(row.Paging)
            from tailWhere in Condition(scope, 1)
            from innerUnaliased in Chance(30).Array[4]
            select Assemble(
                row,
                scope,
                joinSpec,
                filter,
                body,
                order,
                limit,
                tailWhere,
                innerUnaliased);
    }

    /// <summary>A condition over <paramref name="tables"/> with no subquery, for the relations
    /// that add one.</summary>
    public static Gen<SpecCondition> PlainCondition(Dbms dbms, IReadOnlyList<string> tables) =>
        Condition(new GenScope(dbms, tables, tables, 1, false, Correlation.Never), 2);

    private static QuerySpec Assemble(
        CaseRow row,
        GenScope scope,
        JoinSpec? join,
        SpecCondition? where,
        SelectBody body,
        IReadOnlyList<SpecExpression> order,
        RowLimit? limit,
        SpecCondition tailWhere,
        bool[] innerUnaliased)
    {
        List<SpecExpression> items = [.. body.Items];
        if (row.Distinct && row.SetOperator == SetOperatorLevel.None)
        {
            // DISTINCT takes only the sort keys its select list holds.
            foreach (SpecExpression key in order.Select(k => k is SortKey s ? s.Key : k))
            {
                if (!items.Any(i => i.ToCSharp() == key.ToCSharp()))
                {
                    items.Add(key);
                }
            }
        }

        QueryBlock head = new()
        {
            IsDistinct = row.Distinct,
            Items = items,
            From = scope.Tables[0],
            Join = join,
            Where = where,
            GroupBy = body.GroupBy,
            Having = body.Having,
            AliasesItems = true,
        };

        if (row.SetOperator != SetOperatorLevel.None)
        {
            QueryBlock branch = head with { Where = tailWhere, AliasesItems = false };
            head = head with { Tail = [new SetBranch(row.SetOperator, branch)] };
            order = row.OrderBy == OrderByLevel.None
                ? []
                : [.. Enumerable.Range(1, items.Count).Select(p => new OrdinalKey(p))];
        }

        head = head with { OrderBy = order, Limit = limit };

        HashSet<string> unaliased = [];
        if (row.TargetAlias == TargetAliasLevel.Unaliased)
        {
            // An inner block stays aliased, so the outer table's name still binds to it.
            unaliased.UnionWith(scope.Tables);
        }
        else
        {
            string[] inner = ["u2", "d2", "u3", "d3"];
            unaliased.UnionWith(inner.Where((_, i) => innerUnaliased[i]));
        }

        return new QuerySpec(head, unaliased);
    }

    private sealed record SelectBody(
        IReadOnlyList<SpecExpression> Items,
        IReadOnlyList<SpecExpression> GroupBy,
        SpecCondition? Having);

    private static Gen<JoinSpec?> JoinCondition(GenScope scope, JoinLevel level)
    {
        if (level == JoinLevel.None)
        {
            return Nothing<JoinSpec>();
        }

        SpecCondition key =
            new Comparison("==", new ColumnRef("u", "dept_id"), new ColumnRef("d", "id"));
        return
            from extra in Chance(25)
            from more in Condition(scope, 1)
            select (JoinSpec?)new JoinSpec(
                level == JoinLevel.Inner,
                scope.Tables[1],
                extra ? new Junction(true, [key, more]) : key);
    }

    private static Gen<SpecCondition?> TopWhere(GenScope scope, CaseRow row)
    {
        Gen<SpecCondition?> shaped = row.Where == WhereLevel.None
            ? Nothing<SpecCondition>()
            : Shaped(scope, row.Where).Select(c => (SpecCondition?)c);
        if (row.Subquery is SubqueryLevel.None or SubqueryLevel.ScalarInSelect)
        {
            return shaped;
        }

        return
            from basic in shaped
            from predicate in SubqueryPredicate(scope, row.Subquery)
            from isOr in Chance(50)
            select basic is null
                ? predicate
                : (SpecCondition?)new Junction(!isOr, [basic, predicate]);
    }

    private static Gen<SpecCondition> Shaped(GenScope scope, WhereLevel level) => level switch
    {
        WhereLevel.Compare => Compare(scope, 2),
        WhereLevel.NullTest => NullCheck(scope, 2),
        WhereLevel.InList => InValues(scope, 2),
        WhereLevel.Like => Like(scope, 2),
        WhereLevel.Between => Between(scope, 2),
        WhereLevel.AndOrNest =>
            from inner in Junction(scope, 1, allowsConditionIf: false)
            from other in Condition(scope, 1)
            from isAnd in Gen.Bool
            select (SpecCondition)new Junction(isAnd, [inner, other]),
        WhereLevel.Not => Condition(scope, 1).Select(c => (SpecCondition)new Negation(c)),
        WhereLevel.ConditionIfMix =>
            from operands in Condition(scope, 1).Array[1, 2]
            from include in Gen.Bool
            from optional in Condition(scope, 0)
            from position in Gen.Int[0, operands.Length]
            from isAnd in Gen.Bool
            select (SpecCondition)new Junction(
                isAnd,
                [.. operands.Take(position), new ConditionIfTest(include, optional),
                    .. operands.Skip(position)]),
        _ => throw new InvalidOperationException(level.ToString()),
    };

    private static Gen<SpecCondition> SubqueryPredicate(GenScope scope, SubqueryLevel level)
    {
        Correlation correlation = scope.Correlation;
        return level switch
        {
            SubqueryLevel.In or SubqueryLevel.NotIn =>
                from text in Chance(30)
                from operand in text ? TextColumn(scope) : IntColumn(scope)
                from query in Subquery(scope, operand.Type, false, correlation, quantified: true)
                select (SpecCondition)new InSubquery(operand, query, level == SubqueryLevel.NotIn),
            SubqueryLevel.Exists or SubqueryLevel.NotExists =>
                from text in Chance(50)
                from query in Subquery(
                    scope,
                    text ? SpecType.Text : SpecType.Int,
                    false,
                    correlation,
                    quantified: false)
                select (SpecCondition)new ExistsTest(query, level == SubqueryLevel.NotExists),
            _ => ScalarComparison(scope, correlation),
        };
    }

    private static Gen<SpecCondition> ScalarComparison(GenScope scope, Correlation correlation) =>
        from text in Chance(30)
        from column in text ? TextColumn(scope) : IntColumn(scope)
        from op in Gen.OneOf(text ? s_textOperators : ["==", "<", ">", "!="])
        from query in Subquery(scope, column.Type, true, correlation, quantified: false)
        select (SpecCondition)new Comparison(op, column, new ScalarSubquery(query));

    private static Gen<SelectBody> Body(GenScope scope, CaseRow row)
    {
        if (row.Aggregate == AggregateLevel.WholeTable)
        {
            return
                from first in FunctionAggregate(scope, row.SelectFunction)
                from more in Aggregate(scope, intOnly: false).Array[0, 2]
                select new SelectBody([first, .. more], [], null);
        }

        if (row.Aggregate != AggregateLevel.None)
        {
            return Grouped(scope, row);
        }

        Gen<SpecExpression[]> extras = row.SelectFunction == SelectFunctionLevel.None
            ? AnyColumn(scope).Array[1, 3]
            : ScalarExpr(scope, 1).Array[0, 2];
        Gen<SpecExpression?> subquery = row.Subquery == SubqueryLevel.ScalarInSelect
            ? SelectSubquery(scope).Select(e => (SpecExpression?)e)
            : Nothing<SpecExpression>();
        return
            from function in FunctionItem(scope, row.SelectFunction)
            from more in extras
            from sub in subquery
            select new SelectBody(
                [.. new[] { function }.OfType<SpecExpression>(), .. more,
                    .. new[] { sub }.OfType<SpecExpression>()],
                [],
                null);
    }

    private static Gen<SpecExpression> SelectSubquery(GenScope scope) =>
        from text in Chance(40)
        from query in Subquery(
            scope,
            text ? SpecType.Text : SpecType.Int,
            true,
            scope.Correlation,
            quantified: false)
        from wrap in Chance(50)
        from fallback in text ? TextLiteral() : IntLiteral()
        let sub = (SpecExpression)new ScalarSubquery(query)
        select wrap
            ? new FunctionCall("Coalesce", sub.Type, [sub, fallback])
            : sub;

    private static Gen<SelectBody> Grouped(GenScope scope, CaseRow row)
    {
        bool ordered = row.OrderBy != OrderByLevel.None;
        bool nullsFirstKey = row.OrderBy is OrderByLevel.NullsFirst or OrderByLevel.NullsLast;
        Gen<ColumnRef> key = ordered
            ? IntColumn(scope)
            : AnyColumn(scope).Select(e => (ColumnRef)e);
        Gen<ColumnRef> lead = nullsFirstKey ? NullableIntColumn() : key;
        Gen<SpecCondition?> having = row.Aggregate == AggregateLevel.GroupByHaving
            ? Having(scope).Select(h => (SpecCondition?)h)
            : Nothing<SpecCondition>();
        return
            from first in lead
            from second in key.Array[0, 1]
            from function in FunctionAggregate(scope, row.SelectFunction)
            from more in Aggregate(scope, intOnly: false).Array[0, 1]
            from h in having
            let keys = new[] { first }.Concat(second).DistinctBy(k => k.ToCSharp())
                .Cast<SpecExpression>().ToArray()
            select new SelectBody([.. keys, function, .. more], keys, h);
    }

    private static Gen<SpecCondition> Having(GenScope scope) =>
        from op in Gen.OneOf(">", "<", ">=", "==")
        from aggregate in Aggregate(scope, intOnly: true)
        from value in IntLiteral()
        from withNull in Chance(30)
        from other in Aggregate(scope, intOnly: false)
        from negated in Gen.Bool
        from isAnd in Gen.Bool
        let compare = (SpecCondition)new Comparison(op, aggregate, value)
        select withNull ? new Junction(isAnd, [compare, new NullTest(other, negated)]) : compare;

    private static Gen<IReadOnlyList<SpecExpression>> Order(
        GenScope scope,
        CaseRow row,
        SelectBody body)
    {
        if (row.OrderBy == OrderByLevel.None || row.SetOperator != SetOperatorLevel.None)
        {
            return Gen.Const<IReadOnlyList<SpecExpression>>([]);
        }

        bool isDescending = row.OrderBy == OrderByLevel.UniqueDesc;
        NullsPlacement nulls = row.OrderBy switch
        {
            OrderByLevel.NullsFirst => NullsPlacement.First,
            OrderByLevel.NullsLast => NullsPlacement.Last,
            _ => NullsPlacement.Default,
        };
        if (body.GroupBy.Count > 0)
        {
            return
                from leadDescending in Gen.Bool
                select (IReadOnlyList<SpecExpression>)[.. body.GroupBy.Select((k, i) => i == 0
                    ? Sorted(k, nulls == NullsPlacement.Default ? isDescending : leadDescending,
                        nulls)
                    : Sorted(k, isDescending, NullsPlacement.Default))];
        }

        // Each result row has its own value of these keys, so the order and any row limit
        // over it are deterministic.
        SpecExpression[] unique = scope.Tables[0] == "u"
            ? [new ColumnRef("u", "id")]
            : scope.Tables.Count == 1
                ? [new ColumnRef("d", "id")]
                : [new ColumnRef("d", "id"), new ColumnRef("u", "id")];
        if (nulls != NullsPlacement.Default)
        {
            return
                from lead in NullableIntColumn()
                from leadDescending in Gen.Bool
                select (IReadOnlyList<SpecExpression>)
                    [Sorted(lead, leadDescending, nulls), .. unique];
        }

        // A DISTINCT sort key must match a select item, which a bound value never does.
        Gen<SpecExpression> extraKey = row.Distinct
            ? IntColumn(scope).Select(c => (SpecExpression)c)
            : IntExpr(scope, 1);
        return
            from withExtra in Chance(40)
            from extra in extraKey
            from explicitAscending in Gen.Bool
            let keys = withExtra ? [extra, .. unique] : unique
            select (IReadOnlyList<SpecExpression>)[.. keys.Select(k =>
                isDescending || explicitAscending
                    ? Sorted(k, isDescending, NullsPlacement.Default)
                    : k)];
    }

    private static SpecExpression Sorted(
        SpecExpression key,
        bool isDescending,
        NullsPlacement nulls) =>
        new SortKey(key, isDescending, nulls);

    private static Gen<RowLimit?> Limit(PagingLevel paging) => paging switch
    {
        PagingLevel.None => Nothing<RowLimit>(),
        PagingLevel.Limit or PagingLevel.Top =>
            Gen.Int[0, 8].Select(n => (RowLimit?)new RowLimit(paging, n, 0)),
        PagingLevel.FetchFirst => Gen.Int[1, 8].Select(n => (RowLimit?)new RowLimit(paging, n, 0)),

        // T-SQL documents a FETCH count of one or more, so every FETCH form takes one.
        _ =>
            from count in paging == PagingLevel.LimitOffset ? Gen.Int[0, 8] : Gen.Int[1, 8]
            from offset in Gen.Int[0, 5]
            select (RowLimit?)new RowLimit(paging, count, offset),
    };

    private static Gen<SpecExpression?> FunctionItem(GenScope scope, SelectFunctionLevel level) =>
        level == SelectFunctionLevel.None
            ? Nothing<SpecExpression>()
            : FunctionExpression(scope, level).Select(e => (SpecExpression?)e);

    private static Gen<SpecExpression> FunctionExpression(
        GenScope scope,
        SelectFunctionLevel level) =>
        level switch
        {
            SelectFunctionLevel.Coalesce => Gen.OneOf<SpecExpression>(
                from c in IntColumn(scope)
                from v in IntLiteral()
                select (SpecExpression)new FunctionCall("Coalesce", SpecType.Int, [c, v]),
                from c in TextColumn(scope)
                from v in TextLiteral()
                select (SpecExpression)new FunctionCall("Coalesce", SpecType.Text, [c, v])),
            SelectFunctionLevel.Text => TextFunction(scope, 1),
            SelectFunctionLevel.Arithmetic =>
                from left in IntExpr(scope, 1)
                from op in Gen.OneOf("+", "-", "*")
                from right in IntLiteral()
                from abs in Chance(30)
                let arithmetic = (SpecExpression)new FunctionCall(op, SpecType.Int, [left, right])
                select abs ? new FunctionCall("Abs", SpecType.Int, [arithmetic]) : arithmetic,
            _ =>
                from text in Chance(40)
                from when in Condition(scope, 1)
                from then in text ? TextExpr(scope, 1, caseMapping: false) : IntExpr(scope, 1)
                from otherwise in text ? TextLiteral() : IntLiteral()
                select (SpecExpression)new CaseExpression(when, then, otherwise),
        };

    private static Gen<SpecExpression> FunctionAggregate(
        GenScope scope,
        SelectFunctionLevel level)
    {
        if (level == SelectFunctionLevel.None)
        {
            return Aggregate(scope, intOnly: false);
        }

        return
            from argument in FunctionExpression(scope, level)
            from kind in argument.Type == SpecType.Text
                ? Gen.OneOf("Max", "Min", "Count")
                : Gen.OneOf("Sum", "Max", "Min")
            from distinct in Chance(kind is "Count" or "Sum" ? 30 : 0)
            select (SpecExpression)new AggregateCall(kind, argument, distinct);
    }

    private static Gen<SpecExpression> Aggregate(GenScope scope, bool intOnly) =>
        Gen.OneOf<SpecExpression>(
            Gen.Const<SpecExpression>(new AggregateCall("Count", null, false)),
            from c in AnyColumn(scope)
            from d in Chance(30)
            select (SpecExpression)new AggregateCall("Count", c, d),
            from c in IntColumn(scope)
            from d in Chance(20)
            select (SpecExpression)new AggregateCall("Sum", c, d),
            from kind in Gen.OneOf("Max", "Min")
            from c in intOnly ? IntColumn(scope).Select(x => (SpecExpression)x) : AnyColumn(scope)
            select (SpecExpression)new AggregateCall(kind, c, false),
            from c in IntColumn(scope)
            from v in IntLiteral()
            select (SpecExpression)new AggregateCall(
                "Sum",
                new FunctionCall("Coalesce", SpecType.Int, [c, v]),
                false));

    private static Gen<QueryBlock> Subquery(
        GenScope outer,
        SpecType want,
        bool scalar,
        Correlation correlation,
        bool quantified)
    {
        int level = outer.Level + 1;
        GenScope Inner(string table) => outer with
        {
            Tables = [table],
            Level = level,
            AllowsSubquery = level < 3,
            Correlation = correlation == Correlation.Never
                ? Correlation.Never
                : Correlation.Optional,
        };

        return
            from users in want == SpecType.Int ? Chance(75) : Chance(50)
            let table = (users ? "u" : "d") + level
            let inner = Inner(table)
            from shape in Gen.Int[0, 9]
            from item in want == SpecType.Text
                ? TextColumn(inner).Select(c => (SpecExpression)c)
                : IntColumn(inner).Select(c => (SpecExpression)c)
            from aggregateKind in Gen.OneOf("Max", "Min", "Count", "CountAll")
            from filtered in Chance(60)
            from filter in Condition(inner, 1)
            from correlate in correlation switch
            {
                Correlation.Required => Gen.Const(true),
                Correlation.Optional => Chance(25),
                _ => Gen.Const(false),
            }
            from link in CorrelationPredicate(inner, outer.Outer)
            from limit in SubqueryLimit(outer.Target, scalar ? 1 : 5, quantified)
            from having in Gen.OneOf(">", ">=", "==")
            from havingCount in Gen.Int[1, 3]
            from branchUsers in Chance(50)
            from branchItemTable in Gen.Const((branchUsers ? "u" : "d") + level)
            from branchItem in want == SpecType.Text
                ? TextColumn(Inner(branchItemTable)).Select(c => (SpecExpression)c)
                : IntColumn(Inner(branchItemTable)).Select(c => (SpecExpression)c)
            from branchFilter in Condition(
                Inner(branchItemTable) with { AllowsSubquery = false },
                0)
            from branchFiltered in Gen.Bool
            from setOperator in Gen.OneOf(
                SetOperatorLevel.Union,
                SetOperatorLevel.UnionAll,
                SetOperatorLevel.Intersect,
                SetOperatorLevel.Except)
            select SubqueryBlock(
                table,
                want,
                scalar,
                shape,
                item,
                aggregateKind,
                filtered ? filter : null,
                correlate ? link : null,
                limit,
                new Comparison(
                    having,
                    new AggregateCall("Count", null, false),
                    new Literal(havingCount)),
                new SetBranch(setOperator, new QueryBlock
                {
                    From = branchItemTable,
                    Items = [branchItem],
                    Where = branchFiltered ? branchFilter : null,
                }));
    }

    private static QueryBlock SubqueryBlock(
        string table,
        SpecType want,
        bool scalar,
        int shape,
        SpecExpression item,
        string aggregateKind,
        SpecCondition? filter,
        SpecCondition? link,
        RowLimit? limit,
        SpecCondition having,
        SetBranch branch)
    {
        SpecCondition[] conditions = [.. new[] { filter, link }.OfType<SpecCondition>()];
        QueryBlock block = new()
        {
            From = table,
            Items = [item],
            Where = conditions.Length switch
            {
                0 => null,
                1 => conditions[0],
                _ => new Junction(true, conditions),
            },
        };
        SpecExpression[] byId = [new ColumnRef(table, "id")];

        if (scalar)
        {
            // One row either way: a unique sort key under a one-row limit, or an aggregate.
            if (shape < 2 && limit is not null)
            {
                return block with { OrderBy = byId, Limit = limit with { Count = 1, Offset = 0 } };
            }

            SpecExpression aggregate = aggregateKind switch
            {
                "CountAll" when want == SpecType.Int => new AggregateCall("Count", null, false),
                "Count" when want == SpecType.Int => new AggregateCall("Count", item, false),
                "Min" => new AggregateCall("Min", item, false),
                _ => new AggregateCall("Max", item, false),
            };
            return block with { Items = [aggregate] };
        }

        return shape switch
        {
            0 => block with { IsDistinct = true },
            1 when limit is not null => block with { OrderBy = byId, Limit = limit },
            2 => block with { GroupBy = [item], Having = having },
            3 => block with { Tail = [branch] },
            _ => block,
        };
    }

    // MySQL rejects LIMIT directly inside IN/NOT IN (ContextRules.CheckLimitInQuantifiedSubquery).
    private static Gen<RowLimit?> SubqueryLimit(Dbms dbms, int maxCount, bool quantified)
    {
        if (quantified && dbms == Dbms.MySql)
        {
            return Nothing<RowLimit>();
        }

        PagingLevel[] forms = [.. new[]
        {
            PagingLevel.Limit,
            PagingLevel.LimitOffset,
            PagingLevel.FetchFirst,
            PagingLevel.OffsetFetch,
            PagingLevel.Top,
        }.Where(f => MatrixLookup.SupportsLevel(dbms, "Paging", f.ToString()))];
        return
            from form in Gen.OneOf(forms)
            from count in Gen.Int[1, maxCount]
            from offset in form is PagingLevel.LimitOffset or PagingLevel.OffsetFetch
                ? Gen.Int[0, 3]
                : Gen.Const(0)
            select (RowLimit?)new RowLimit(form, count, offset);
    }

    private static Gen<SpecCondition> CorrelationPredicate(
        GenScope inner,
        IReadOnlyList<string> outer) =>
        from text in Chance(25)
        from outerTable in Gen.OneOf<string>([.. outer])
        from innerColumn in text ? TextColumn(inner) : IntColumn(inner)
        from outerColumn in text
            ? Gen.Const(new ColumnRef(outerTable, "name"))
            : Gen.OneOf(IntColumnsOf(outerTable)).Select(c => new ColumnRef(outerTable, c))
        from op in Gen.OneOf(text ? s_textOperators : ["==", "==", "<", ">"])
        select (SpecCondition)new Comparison(op, innerColumn, outerColumn);

    private static Gen<SpecCondition> Condition(GenScope scope, int depth)
    {
        List<(int, IGen<SpecCondition>)> options =
        [
            (2, Compare(scope, depth)),
            (1, NullCheck(scope, depth)),
            (1, InValues(scope, depth)),
            (1, Like(scope, depth)),
            (1, Between(scope, depth)),
            (1, ColumnComparison(scope)),
        ];
        if (depth > 0)
        {
            options.Add((2, Junction(scope, depth, allowsConditionIf: true)));
            options.Add((1, Condition(scope, depth - 1)
                .Select(c => (SpecCondition)new Negation(c))));
        }

        if (scope.AllowsSubquery && depth > 0)
        {
            options.Add((1, NestedSubquery(scope)));
        }

        return Gen.Frequency<SpecCondition>([.. options]);
    }

    private static Gen<SpecCondition> NestedSubquery(GenScope scope)
    {
        GenScope inner = scope with { AllowsSubquery = false };
        return Gen.OneOf<SpecCondition>(
            from text in Chance(30)
            from operand in text ? TextColumn(scope) : IntColumn(scope)
            from query in Subquery(inner, operand.Type, false, scope.Correlation, quantified: true)
            from negated in Chance(35)
            select (SpecCondition)new InSubquery(operand, query, negated),
            from text in Chance(50)
            from query in Subquery(
                inner,
                text ? SpecType.Text : SpecType.Int,
                false,
                scope.Correlation,
                quantified: false)
            from negated in Chance(35)
            select (SpecCondition)new ExistsTest(query, negated),
            ScalarComparison(inner, scope.Correlation));
    }

    private static Gen<SpecCondition> Compare(GenScope scope, int depth) =>
        from text in Chance(35)
        from op in Gen.OneOf(text ? s_textOperators : s_intOperators)
        from left in text
            ? TextExpr(scope, depth - 1, caseMapping: true)
            : IntExpr(scope, depth - 1)
        from expressionRight in Chance(20)
        from right in expressionRight
            ? (text ? TextColumn(scope).Select(c => (SpecExpression)c) : IntExpr(scope, 0))
            : (text ? TextLiteral() : IntLiteral())
        select (SpecCondition)new Comparison(op, left, right);

    private static Gen<SpecCondition> NullCheck(GenScope scope, int depth) =>
        from operand in ScalarExpr(scope, depth - 1)
        from negated in Gen.Bool
        select (SpecCondition)new NullTest(operand, negated);

    private static Gen<SpecCondition> InValues(GenScope scope, int depth) =>
        from text in Chance(35)
        from operand in text
            ? TextExpr(scope, depth - 1, caseMapping: true)
            : IntExpr(scope, depth - 1)
        from values in (text ? TextLiteral() : IntLiteral()).Array[1, 3]
        from negated in Chance(40)
        select (SpecCondition)new InList(operand, [.. values.Cast<Literal>()], negated);

    private static Gen<SpecCondition> Like(GenScope scope, int depth) =>
        from operand in TextExpr(scope, depth - 1, caseMapping: true)
        from pattern in Gen.OneOf(s_patterns)
        from negated in Chance(30)
        select (SpecCondition)new LikeTest(operand, new Literal(pattern), negated);

    private static Gen<SpecCondition> Between(GenScope scope, int depth) =>
        from operand in IntExpr(scope, depth - 1)
        from low in IntLiteral()
        from high in IntLiteral()
        from negated in Chance(30)
        select (SpecCondition)new BetweenTest(operand, low, high, negated);

    private static Gen<SpecCondition> ColumnComparison(GenScope scope) =>
        from op in Gen.OneOf("==", "!=", "<", ">")
        from left in IntColumn(scope)
        from right in IntColumn(scope)
        select (SpecCondition)new Comparison(op, left, right);

    // At least one operand always survives, so ConditionIf(false, ...) never empties the clause.
    private static Gen<SpecCondition> Junction(GenScope scope, int depth, bool allowsConditionIf) =>
        from operands in Condition(scope, depth - 1).Array[2, 3]
        from withConditionIf in allowsConditionIf ? Chance(20) : Gen.Const(false)
        from include in Gen.Bool
        from optional in Condition(scope, 0)
        from position in Gen.Int[0, operands.Length]
        from isAnd in Chance(45)
        select (SpecCondition)new Junction(
            isAnd,
            withConditionIf
                ? [.. operands.Take(position), new ConditionIfTest(include, optional),
                    .. operands.Skip(position)]
                : operands);

    private static Gen<SpecExpression> ScalarExpr(GenScope scope, int depth) =>
        Gen.Frequency<SpecExpression>(
            (3, IntExpr(scope, depth)),
            (2, TextExpr(scope, depth, caseMapping: true)));

    private static Gen<SpecExpression> IntExpr(GenScope scope, int depth)
    {
        Gen<SpecExpression> column = IntColumn(scope).Select(c => (SpecExpression)c);
        if (depth <= 0)
        {
            return column;
        }

        List<(int, IGen<SpecExpression>)> options =
        [
            (3, column),
            (1,
                from c in IntColumn(scope)
                from v in IntLiteral()
                select (SpecExpression)new FunctionCall("Coalesce", SpecType.Int, [c, v])),
            (1, IntExpr(scope, depth - 1)
                .Select(e => (SpecExpression)new FunctionCall("Abs", SpecType.Int, [e]))),
            (1,
                from left in IntExpr(scope, depth - 1)
                from op in Gen.OneOf("+", "-", "*")
                from right in IntLiteral()
                select (SpecExpression)new FunctionCall(op, SpecType.Int, [left, right])),
            (1,
                from when in Condition(scope with { AllowsSubquery = false }, depth - 1)
                from then in IntExpr(scope, depth - 1)
                from otherwise in IntLiteral()
                select (SpecExpression)new CaseExpression(when, then, otherwise)),
        ];
        if (MatrixLookup.Supports(scope.Target, "Length"))
        {
            options.Add((1, TextExpr(scope, depth - 1, caseMapping: false)
                .Select(e => (SpecExpression)new FunctionCall("Length", SpecType.Int, [e]))));
        }

        return Gen.Frequency<SpecExpression>([.. options]);
    }

    /// <summary>A text expression. Only the outermost function may map case: under one, a
    /// column would mix cases that a case-insensitive collation folds together.</summary>
    private static Gen<SpecExpression> TextExpr(GenScope scope, int depth, bool caseMapping)
    {
        Gen<SpecExpression> column = TextColumn(scope).Select(c => (SpecExpression)c);
        if (depth <= 0)
        {
            return column;
        }

        List<(int, IGen<SpecExpression>)> options =
        [
            (3, column),
            (1,
                from c in TextColumn(scope)
                from v in TextLiteral()
                select (SpecExpression)new FunctionCall("Coalesce", SpecType.Text, [c, v])),
            (1,
                from c in TextColumn(scope)
                from v in TextLiteral()
                select (SpecExpression)new FunctionCall("Nullif", SpecType.Text, [c, v])),
            (2, TextFunction(scope, depth, caseMapping)),
            (1,
                from when in Condition(scope with { AllowsSubquery = false }, depth - 1)
                from then in TextExpr(scope, depth - 1, caseMapping: false)
                from otherwise in TextLiteral()
                select (SpecExpression)new CaseExpression(when, then, otherwise)),
        ];
        return Gen.Frequency<SpecExpression>([.. options]);
    }

    private static Gen<SpecExpression> TextFunction(
        GenScope scope,
        int depth,
        bool caseMapping = true)
    {
        string substring = MatrixLookup.Supports(scope.Target, "Substr") ? "Substr" : "Substring";
        string[] names = caseMapping
            ? ["Trim", "Replace", substring, "Upper", "Lower"]
            : ["Trim", "Replace", substring];
        return
            from name in Gen.OneOf(names)
            from argument in TextExpr(scope, depth - 1, caseMapping: false)
            select (SpecExpression)new FunctionCall(
                name,
                SpecType.Text,
                name switch
                {
                    "Replace" => [argument, new Literal("o"), new Literal("0")],
                    "Substr" or "Substring" => [argument, new Literal(1), new Literal(2)],
                    _ => [argument],
                });
    }

    private static Gen<ColumnRef> IntColumn(GenScope scope) =>
        from table in Gen.OneOf<string>([.. scope.Tables])
        from column in Gen.OneOf(IntColumnsOf(table))
        select new ColumnRef(table, column);

    private static Gen<ColumnRef> TextColumn(GenScope scope) =>
        Gen.OneOf<string>([.. scope.Tables]).Select(t => new ColumnRef(t, "name"));

    private static Gen<SpecExpression> AnyColumn(GenScope scope) =>
        Gen.Frequency<SpecExpression>(
            (3, IntColumn(scope).Select(c => (SpecExpression)c)),
            (2, TextColumn(scope).Select(c => (SpecExpression)c)));

    private static Gen<ColumnRef> NullableIntColumn() =>
        Gen.OneOf("age", "dept_id").Select(c => new ColumnRef("u", c));

    private static string[] IntColumnsOf(string table) =>
        table[0] == 'u' ? ["id", "age", "dept_id"] : ["id"];

    private static Gen<SpecExpression> IntLiteral() =>
        Gen.OneOf(s_ints).Select(v => (SpecExpression)new Literal(v));

    private static Gen<SpecExpression> TextLiteral() =>
        Gen.OneOf(s_texts).Select(v => (SpecExpression)new Literal(v));

    // Query syntax sizes a value by its earliest choices first, so CsCheck's own measure can
    // shrink to a longer query; the text length is what a reader of the report minds.
    private sealed class SizedByText(Gen<QuerySpec> inner) : Gen<QuerySpec>
    {
        public override QuerySpec Generate(PCG pcg, Size? min, out Size size)
        {
            QuerySpec spec = inner.Generate(pcg, null!, out _);
            size = new Size((ulong)spec.ToCSharp().Length);
            return spec;
        }
    }

    private static Gen<T?> Nothing<T>() where T : class => Gen.Const((T?)null);

    // Shrinks toward false, the simpler spec.
    private static Gen<bool> Chance(int percent) => Gen.Int[1, 100].Select(i => i > 100 - percent);
}
