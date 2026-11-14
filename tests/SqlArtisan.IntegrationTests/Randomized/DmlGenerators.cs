using CsCheck;
using static SqlArtisan.IntegrationTests.Randomized.QueryGenerators;

namespace SqlArtisan.IntegrationTests.Randomized;

/// <summary>
/// CsCheck generators for a <see cref="DmlSpec"/> that fits one <see cref="DmlCaseRow"/>. Each
/// shape an engine rejects, or runs with another reading than <see cref="DmlReference"/>'s,
/// is left out at the site that would build it.
/// </summary>
internal static class DmlGenerators
{
    // Seed ids are 1..28: an inserted VALUES row takes 500 and up, a SELECT-fed one its source
    // id plus 200 (users) or 300 (depts), so no key collides and none needs IDENTITY.
    private const int ValuesIdBase = 500;
    private const int UsersFeedOffset = 200;
    private const int DeptsFeedOffset = 300;

    private const int Attempts = 8;

    // Every order of the columns a statement may write; the key is never one (a row-by-row
    // unique check on PostgreSQL, MySQL and SQLite would see transient duplicates).
    private static readonly string[][] s_settableOrders =
    [
        ["name", "age", "dept_id"], ["name", "dept_id", "age"], ["age", "name", "dept_id"],
        ["age", "dept_id", "name"], ["dept_id", "name", "age"], ["dept_id", "age", "name"],
    ];

    public static Gen<DmlSpec> For(DmlCaseRow row) => new PreferringChange(row.Kind switch
    {
        DmlKind.Insert => InsertSpec(row),
        DmlKind.Update => UpdateSpec(row),
        DmlKind.Upsert => UpsertGenerators.Spec(row),
        DmlKind.Merge => MergeGenerators.Spec(row),
        _ => DeleteSpec(row),
    });

    private static Gen<DmlSpec> UpdateSpec(DmlCaseRow row)
    {
        GenScope scope = TargetScope(row);
        return
            from joined in Join(scope, row)
            from filter in Where(scope, row)
            from set in SetList(scope, row)
            from cte in Cte(row)
            from cteOperand in Gen.OneOf("id", "dept_id")
            from output in OutputItems(row)
            from unaliased in Unaliased(row)
            select new DmlSpec
            {
                Kind = DmlKind.Update,
                Dbms = row.Dbms,
                Unaliased = unaliased,
                Cte = cte,
                Join = joined,
                Set = set,
                Where = WithCte(filter, cte, new ColumnRef("u", cteOperand)),
                Output = row.Output,
                OutputItems = output,
            };
    }

    private static Gen<DmlSpec> DeleteSpec(DmlCaseRow row)
    {
        GenScope scope = TargetScope(row);
        return
            from joined in Join(scope, row)
            from filter in Where(scope, row)
            from cte in Cte(row)
            from cteOperand in Gen.OneOf("id", "dept_id")
            from output in OutputItems(row)
            from unaliased in Unaliased(row)
            select new DmlSpec
            {
                Kind = DmlKind.Delete,
                Dbms = row.Dbms,
                Unaliased = unaliased,
                Cte = cte,
                Join = joined,
                Where = WithCte(filter, cte, new ColumnRef("u", cteOperand)),
                Output = row.Output,
                OutputItems = output,
            };
    }

    // MySQL rejects a subquery over the UPDATE or DELETE target in SET or WHERE at any depth
    // (ER_UPDATE_TABLE_USED, 1093, 8.0.46), aliased or not, so its subqueries read rt_depts.
    private static GenScope TargetScope(DmlCaseRow row)
    {
        string[] tables = row.Join == DmlJoinLevel.None ? ["u"] : ["u", "d"];
        return new GenScope(
            row.Dbms,
            tables,
            tables,
            1,
            AllowsSubquery: false,
            row.Correlated ? Correlation.Required : Correlation.Never,
            ReadsUsers: row.Dbms != Dbms.MySql,
            TextScalarsByRow: true);
    }

    private static SpecCondition? WithCte(SpecCondition? where, CteSpec? cte, ColumnRef operand)
    {
        if (cte is null)
        {
            return where;
        }

        SpecCondition member = new InCte(operand, cte);
        return where is null ? member : new Junction(true, [member, where]);
    }

    // Joining dept_id to the depts key matches one source row at most per target row: with
    // two, which one an UPDATE ... FROM applies is unspecified.
    private static Gen<DmlJoin?> Join(GenScope scope, DmlCaseRow row)
    {
        DmlJoinForm? form = row.Join switch
        {
            DmlJoinLevel.Direct => DmlJoinForm.Direct,
            DmlJoinLevel.From => DmlJoinForm.From,
            DmlJoinLevel.FromRelisted => DmlJoinForm.FromRelisted,
            DmlJoinLevel.Using => DmlJoinForm.Using,
            _ => null,
        };
        if (form is not { } f)
        {
            return Nothing<DmlJoin>();
        }

        SpecCondition key =
            new Comparison("==", new ColumnRef("u", "dept_id"), new ColumnRef("d", "id"));
        bool canLeftJoin = f is DmlJoinForm.Direct or DmlJoinForm.FromRelisted;
        return
            from left in canLeftJoin ? Chance(30) : Gen.Const(false)
            from extra in Chance(25)
            from more in Condition(scope with { Correlation = Correlation.Never }, 1)
            select (DmlJoin?)new DmlJoin(f, !left, extra ? new Junction(true, [key, more]) : key);
    }

    private static Gen<SpecCondition?> Where(GenScope scope, DmlCaseRow row)
    {
        Gen<SpecCondition?> shaped = row.Where == WhereLevel.None
            ? Nothing<SpecCondition>()
            : Shaped(scope, row.Where).Select(c => (SpecCondition?)c);
        SubqueryLevel? level = row.Subquery switch
        {
            DmlSubqueryLevel.In => SubqueryLevel.In,
            DmlSubqueryLevel.NotIn => SubqueryLevel.NotIn,
            DmlSubqueryLevel.Exists => SubqueryLevel.Exists,
            DmlSubqueryLevel.NotExists => SubqueryLevel.NotExists,
            DmlSubqueryLevel.ScalarInWhere => SubqueryLevel.ScalarInWhere,
            _ => null,
        };
        if (level is not { } subquery)
        {
            return shaped;
        }

        return
            from basic in shaped
            from predicate in SubqueryPredicate(scope, subquery)
            from isOr in Chance(50)
            select basic is null
                ? predicate
                : (SpecCondition?)new Junction(!isOr, [basic, predicate]);
    }

    private static Gen<IReadOnlyList<Assignment>> SetList(GenScope scope, DmlCaseRow row)
    {
        // SQLite evaluates a FROM-less UPDATE's correlated SET subquery over the target against
        // the rows already updated (dbms-differences.md); with a FROM it reads the starting rows.
        GenScope subqueryScope = row.Dbms == Dbms.Sqlite && row.Correlated
            && row.Join == DmlJoinLevel.None
            ? scope with { ReadsUsers = false }
            : scope;
        return
            from columns in SettableOrder()
            from count in Gen.Int[1, 3]
            from name in SetValue(scope, row.Value, SpecType.Text)
            from age in SetValue(scope, row.Value, SpecType.Int)
            from dept in SetValue(scope, row.Value, SpecType.Int)
            from subquery in row.Subquery == DmlSubqueryLevel.ScalarInSet
                ? ScalarSubqueryValue(subqueryScope, columns[0] == "name")
                    .Select(e => (SpecExpression?)e)
                : Nothing<SpecExpression>()
            select OrderedForMySql(row, [.. columns.Take(count).Select((c, i) => new Assignment(
                c,
                i == 0 && subquery is not null
                    ? subquery
                    : c switch
                    {
                        "name" => name,
                        "age" => age,
                        _ => dept,
                    }))]);
    }

    // MySQL evaluates SET left to right, each right side reading the values earlier
    // assignments wrote (dbms-differences.md); a joined UPDATE's order is unspecified.
    internal static IReadOnlyList<Assignment> OrderedForMySql(
        DmlCaseRow row,
        IReadOnlyList<Assignment> set)
    {
        if (row.Dbms != Dbms.MySql)
        {
            return set;
        }

        return [.. set.Select((a, i) => set
            .Where((other, j) => row.Join == DmlJoinLevel.Direct ? j != i : j < i)
            .Any(other => Reads(a.Value, other.Column))
            ? a with { Value = a.Column == "name" ? new Literal("Xu") : new Literal(7) }
            : a)];
    }

    private static bool Reads(SpecExpression expression, string column)
    {
        bool found = false;
        expression.Rewrite(SpecRewrite.Expressions(e =>
        {
            found |= e is ColumnRef { Table: "u" } c && c.Column == column;
            return e;
        }));
        return found;
    }

    private static Gen<SpecExpression> SetValue(GenScope scope, DmlValueLevel level, SpecType type)
    {
        bool text = type == SpecType.Text;
        return level switch
        {
            DmlValueLevel.Column => text
                ? TextColumn(scope).Select(c => (SpecExpression)c)
                : IntColumn(scope).Select(c => (SpecExpression)c),
            DmlValueLevel.Expression => text
                ? TextExpr(scope with { AllowsSubquery = false }, 1, caseMapping: true)
                : IntExpr(scope with { AllowsSubquery = false }, 1),
            _ => Constant(type),
        };
    }

    internal static Gen<SpecExpression> Constant(SpecType type) =>
        from isNull in Chance(20)
        from value in type == SpecType.Text ? TextLiteral() : IntLiteral()
        select isNull ? new NullValue(type) : value;

    internal static Gen<SpecExpression> ScalarSubqueryValue(GenScope scope, bool text) =>
        from query in Subquery(
            scope,
            text ? SpecType.Text : SpecType.Int,
            true,
            scope.Correlation,
            quantified: false)
        from wrap in Chance(50)
        from fallback in text ? TextLiteral() : IntLiteral()
        let sub = (SpecExpression)new ScalarSubquery(query)
        select wrap ? new FunctionCall("Coalesce", sub.Type, [sub, fallback]) : sub;

    private static Gen<DmlSpec> InsertSpec(DmlCaseRow row)
    {
        bool listed = row.ColumnList == ColumnListLevel.Listed;
        return
            from others in SettableOrder()
            from count in Gen.Int[0, 3]
            from idAt in Gen.Int[0, 3]
            from cte in Cte(row)
            let columns = Columns(row, listed, others, count, idAt, cte)
            from rows in row.Source == InsertSourceLevel.Select
                ? Gen.Const<IReadOnlyList<IReadOnlyList<SpecExpression>>>([])
                : InsertRows(row, columns, cte)
            from feed in row.Source == InsertSourceLevel.Select
                ? Feed(row, columns, cte).Select(f => (QueryBlock?)f)
                : Nothing<QueryBlock>()
            from output in OutputItems(row)
            from unaliased in Unaliased(row)
            select new DmlSpec
            {
                Kind = DmlKind.Insert,
                Dbms = row.Dbms,
                Unaliased = unaliased,
                Cte = cte,
                CteInSelect = row.Cte == DmlCteLevel.InSelect,
                Insert = new InsertShape(row.Source, listed, columns, rows, feed),
                Output = row.Output,
                OutputItems = output,
            };
    }

    // A positional row fills every column in table order, the code key NULL; a leading CTE's
    // MAX needs an integer column besides the key to land in.
    private static string[] Columns(
        DmlCaseRow row,
        bool listed,
        string[] others,
        int count,
        int idAt,
        CteSpec? cte)
    {
        if (!listed && row.Source != InsertSourceLevel.SetForm)
        {
            return ["id", "name", "age", "dept_id", "code"];
        }

        List<string> columns = [.. others.Take(count)];
        if (cte is not null && row.Source != InsertSourceLevel.Select && !columns.Contains("age"))
        {
            columns.Add("age");
        }

        columns.Insert(Math.Min(idAt, columns.Count), "id");
        return [.. columns];
    }

    private static Gen<IReadOnlyList<IReadOnlyList<SpecExpression>>> InsertRows(
        DmlCaseRow row,
        string[] columns,
        CteSpec? cte)
    {
        // A VALUES subquery correlates to nothing; the outer table only feeds the unused link.
        // MySQL's later VALUES rows read the rows inserted before them (dbms-differences.md).
        bool multi = row.Source == InsertSourceLevel.MultiValues;
        GenScope scope = new(
            row.Dbms,
            ["u4"],
            ["u4"],
            1,
            AllowsSubquery: true,
            Correlation.Never,
            ReadsUsers: !(multi && row.Dbms == Dbms.MySql),
            TextScalarsByRow: true);
        int rows = multi ? 3 : 1;
        return
            from idBase in Gen.Int[0, 40]
            from rowCount in Gen.Int[multi ? 2 : 1, rows]
            from values in InsertValue(scope, row.Value, SpecType.Text).Array[rows * 4]
            from ints in InsertValue(scope, row.Value, SpecType.Int).Array[rows * 4]
            select (IReadOnlyList<IReadOnlyList<SpecExpression>>)[.. Enumerable.Range(0, rowCount)
                .Select(r => (IReadOnlyList<SpecExpression>)[.. columns.Select((c, i) => c switch
                {
                    "id" => new Literal(ValuesIdBase + (idBase * 10) + r),
                    "age" when cte is not null && r == 0 => new CteMax(cte),
                    "code" => new NullValue(SpecType.Int),
                    "name" => values[(r * 4) + i],
                    _ => ints[(r * 4) + i],
                })])];
    }

    // MySQL rejects a VALUES subquery that names its INSERT target bare (1093, 8.0.46) and runs
    // one over an aliased rt_users; Unaliased keeps MySQL's inner tables aliased.
    internal static Gen<SpecExpression> InsertValue(
        GenScope scope,
        DmlValueLevel level,
        SpecType type)
    {
        bool text = type == SpecType.Text;
        return level switch
        {
            DmlValueLevel.Expression => text
                ? from v in TextLiteral()
                  from f in Gen.OneOf("Upper", "Lower", "Trim", "Coalesce")
                  select (SpecExpression)(f == "Coalesce"
                      ? new FunctionCall(f, SpecType.Text, [new NullValue(SpecType.Text), v])
                      : new FunctionCall(f, SpecType.Text, [v]))
                : from a in IntLiteral()
                  from b in IntLiteral()
                  from f in Gen.OneOf("Abs", "+", "*", "Coalesce")
                  select (SpecExpression)(f switch
                  {
                      "Abs" => new FunctionCall(f, SpecType.Int, [a]),
                      "Coalesce" => new FunctionCall(f, SpecType.Int, [new NullValue(type), a]),
                      _ => new FunctionCall(f, SpecType.Int, [a, b]),
                  }),
            DmlValueLevel.Subquery => ScalarSubqueryValue(scope, text),
            _ => Constant(type),
        };
    }

    private static Gen<QueryBlock> Feed(DmlCaseRow row, string[] columns, CteSpec? cte) =>
        from users in Chance(75)
        let table = users ? "u2" : "d2"
        let scope = new GenScope(
            row.Dbms,
            [table],
            [table],
            2,
            AllowsSubquery: true,
            Correlation.Optional,
            TextScalarsByRow: true)
        from items in FeedItem(scope, row.Value, SpecType.Text).Array[4]
        from ints in FeedItem(scope, row.Value, SpecType.Int).Array[4]
        from filtered in Chance(80)
        from filter in Condition(scope, 1)
        let id = new FunctionCall(
            "+",
            SpecType.Int,
            [new ColumnRef(table, "id"), new Literal(users ? UsersFeedOffset : DeptsFeedOffset)])
        select new QueryBlock
        {
            From = table,
            Items = [.. columns.Select((c, i) => c switch
            {
                "id" => id,
                "name" => items[i],
                _ => ints[i],
            })],
            Where = WithCte(filtered ? filter : null, cte, new ColumnRef(table, "id")),
        };

    private static Gen<SpecExpression> FeedItem(GenScope scope, DmlValueLevel level, SpecType type)
    {
        bool text = type == SpecType.Text;
        return level switch
        {
            DmlValueLevel.Expression => text
                ? TextExpr(scope with { AllowsSubquery = false }, 1, caseMapping: true)
                : IntExpr(scope with { AllowsSubquery = false }, 1),
            DmlValueLevel.Subquery => ScalarSubqueryValue(scope, text),
            _ => Constant(type),
        };
    }

    private static Gen<CteSpec?> Cte(DmlCaseRow row)
    {
        if (row.Cte == DmlCteLevel.None)
        {
            return Nothing<CteSpec>();
        }

        if (row.Recursive)
        {
            return
                from start in Gen.Int[1, 6]
                from step in Gen.Int[1, 4]
                from span in Gen.Int[3, 14]
                select (CteSpec?)new CteSpec(true, null, start, step, start + span);
        }

        return
            from users in Chance(70)
            let table = users ? "u4" : "d4"
            let scope = new GenScope(row.Dbms, [table], [table], 3, false, Correlation.Never)
            from column in users ? Gen.OneOf("id", "dept_id") : Gen.Const("id")
            from filtered in Chance(70)
            from filter in Condition(scope, 1)
            select (CteSpec?)new CteSpec(
                false,
                new QueryBlock
                {
                    From = table,
                    Items = [new ColumnRef(table, column)],
                    Where = filtered ? filter : null,
                    AliasesItems = true,
                },
                0,
                0,
                0);
    }

    // Every output row leads with the target's id, which the output-to-state relation reads
    // it back by.
    internal static Gen<IReadOnlyList<SpecExpression>> OutputItems(DmlCaseRow row)
    {
        bool inserted = row.Kind != DmlKind.Delete;
        bool deleted = row.Kind != DmlKind.Insert;
        switch (row.Output)
        {
            case DmlOutputLevel.None:
                return Gen.Const<IReadOnlyList<SpecExpression>>([]);
            case DmlOutputLevel.OutputInto:
                return
                    from side in Side(inserted, deleted)
                    from nameSide in Side(inserted, deleted)
                    select (IReadOnlyList<SpecExpression>)
                    [
                        new FunctionCall("+", SpecType.Int, [
                            new PseudoColumn(side, "id"),
                            new Literal(DmlSpec.DestinationIdOffset),
                        ]),
                        new PseudoColumn(nameSide, "name"),
                    ];
            case DmlOutputLevel.Output:
                return
                    from side in Side(inserted, deleted)
                    from columns in SettableOrder()
                    from count in Gen.Int[0, 2]
                    from sides in Side(inserted, deleted).Array[2]
                    select (IReadOnlyList<SpecExpression>)
                    [
                        new PseudoColumn(side, "id"),
                        .. columns.Take(count).Select((c, i) => new PseudoColumn(sides[i], c)),
                    ];
            default:
                return
                    from columns in SettableOrder()
                    from count in Gen.Int[0, 2]
                    from arithmetic in Chance(25)
                    from lookup in Chance(row.Dbms == Dbms.Oracle
                        || row.TargetAlias == TargetAliasLevel.Unaliased ? 0 : 25)
                    select (IReadOnlyList<SpecExpression>)
                    [
                        new ColumnRef("u", "id"),
                        .. columns.Take(count).Select(c => new ColumnRef("u", c)),
                        .. arithmetic
                            ? new[]
                            {
                                (SpecExpression)new FunctionCall(
                                    "+",
                                    SpecType.Int,
                                    [new ColumnRef("u", "age"), new Literal(1)]),
                            }
                            : [],
                        .. lookup ? new[] { DeptName() } : [],
                    ];
        }
    }

    // A RETURNING subquery reads rt_depts only: SQLite evaluates one over the target against
    // the rows the statement has already changed (dbms-differences.md). Oracle's RETURNING
    // INTO takes plain expressions only.
    private static SpecExpression DeptName() => new ScalarSubquery(new QueryBlock
    {
        From = "d2",
        Items = [new ColumnRef("d2", "name")],
        Where = new Comparison("==", new ColumnRef("d2", "id"), new ColumnRef("u", "dept_id")),
    });

    internal static Gen<string[]> SettableOrder() => Gen.OneOf(s_settableOrders);

    private static Gen<bool> Side(bool inserted, bool deleted) =>
        inserted && deleted ? Gen.Bool : Gen.Const(inserted);

    internal static Gen<IReadOnlySet<string>> Unaliased(DmlCaseRow row)
    {
        if (row.TargetAlias == TargetAliasLevel.Unaliased)
        {
            // An inner block stays aliased, so the target's bare name still binds to it.
            return Gen.Const<IReadOnlySet<string>>(new HashSet<string> { "u" });
        }

        // An INSERT's feeding table is what its subqueries correlate to, so it stays aliased.
        string[] inner = row.Kind == DmlKind.Insert ? ["u3", "d3"] : ["u2", "d2", "u3", "d3"];
        bool mySqlInsert = row.Dbms == Dbms.MySql && row.Kind == DmlKind.Insert;
        return Chance(mySqlInsert ? 0 : 30).Array[inner.Length].Select(flags =>
            (IReadOnlySet<string>)new HashSet<string>(inner.Where((_, i) => flags[i])));
    }

    /// <summary>
    /// Retries a statement the reference says changes no row, which every relation would pass
    /// unseen, then pins Oracle's <c>RETURNING ... INTO</c> to one target row: its output
    /// parameters hold one row's values.
    /// </summary>
    private sealed class PreferringChange(Gen<DmlSpec> inner) : Gen<DmlSpec>
    {
        public override DmlSpec Generate(PCG pcg, Size? min, out Size size)
        {
            DmlSpec spec = inner.Generate(pcg, null!, out _);
            for (int i = 1; i < Attempts && DmlReference.ChangesNothing(spec); i++)
            {
                spec = inner.Generate(pcg, null!, out _);
            }

            if (spec is { Dbms: Dbms.Oracle, Output: DmlOutputLevel.Returning }
                && spec.Kind != DmlKind.Insert)
            {
                IReadOnlyList<long> ids = DmlReference.MatchedIds(spec) is { Count: > 0 } matched
                    ? matched
                    : [.. RandomSchema.Users.Select(u => u.Id)];
                spec = spec with { PinnedId = ids[(int)(pcg.Next() % (uint)ids.Count)] };
            }

            size = new Size((ulong)spec.ToCSharp().Length);
            return spec;
        }
    }
}
