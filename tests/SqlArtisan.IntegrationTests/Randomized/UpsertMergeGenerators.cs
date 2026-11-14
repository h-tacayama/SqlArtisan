using CsCheck;
using static SqlArtisan.IntegrationTests.Randomized.QueryGenerators;

namespace SqlArtisan.IntegrationTests.Randomized;

/// <summary>
/// CsCheck generators for an upsert that fits one <c>upsert.cases.csv</c> row. A proposed row
/// either meets exactly one seed row, on keys that include the arbiter, or takes keys no row
/// holds; no two rows of a batch share a key or meet the same seed row.
/// </summary>
internal static class UpsertGenerators
{
    // Fresh ids and codes sit apart from the seed's (ids 1..28, codes 101..127) and from the
    // codes an update writes (3000 + id).
    private const int FreshIdBase = 500;
    private const int FreshCodeBase = 900;
    private const int WrittenCodeOffset = 3000;

    public static Gen<DmlSpec> Spec(DmlCaseRow row)
    {
        bool multi = row.Rows is UpsertRowsLevel.Chained or UpsertRowsLevel.Array;
        return
            from others in DmlGenerators.SettableOrder()
            from take in Gen.Int[0, 3]
            from idAt in Gen.Int[0, 3]
            from codeAt in Gen.Int[0, 4]
            from count in multi ? Gen.Int[2, 3] : Gen.Const(1)
            from subset in Gen.Int[1, 6]
            from picks in Gen.Int[0, 1000].Array[3]
            from kinds in Gen.Int[0, 2].Array[3]
            from freshBase in Gen.Int[0, 40]
            from nullCodes in Chance(20).Array[3]
            from texts in Value(row, SpecType.Text).Array[3]
            from ints in Value(row, SpecType.Int).Array[6]
            from set in Assignments(row, multi)
            from filter in row.Action == UpsertActionLevel.DoUpdateWhere
                ? Condition(Scope(row, multi), 2).Select(c => (SpecCondition?)c)
                : Nothing<SpecCondition>()
            from output in DmlGenerators.OutputItems(row)
            from unaliased in DmlGenerators.Unaliased(row)
            let columns = Columns(others, take, idAt, codeAt)
            let keys = Keys(row, count, subset, picks, kinds, freshBase, nullCodes)
            select new DmlSpec
            {
                Kind = DmlKind.Upsert,
                Dbms = row.Dbms,
                Unaliased = unaliased,
                Insert = new InsertShape(
                    row.Rows == UpsertRowsLevel.SetForm
                        ? InsertSourceLevel.SetForm
                        : multi ? InsertSourceLevel.MultiValues : InsertSourceLevel.Values,
                    row.Rows != UpsertRowsLevel.SetForm,
                    columns,
                    [.. keys.Select((k, r) => (IReadOnlyList<SpecExpression>)[.. columns.Select(
                        (c, i) => c switch
                        {
                            "id" => new Literal((int)k.Id),
                            "code" => k.Code is { } code
                                ? new Literal((int)code)
                                : new NullValue(SpecType.Int),
                            "name" => texts[r],
                            _ => ints[(r * 2) + (i % 2)],
                        })])],
                    null),
                Upsert = new UpsertShape(
                    row.Arbiter,
                    row.Action,
                    row.Rows == UpsertRowsLevel.Array,
                    ListedOnly(row, set, columns),
                    filter),
                Output = row.Output,
                OutputItems = output,
            };
    }

    // MySQL 8.0.46's row alias holds the listed columns alone, so new.age of an INSERT that lists
    // no age is unknown (1054); SqlArtisan renders the target's own column bare, which it reads
    // as ambiguous beside a listed one (1052; a candidate defect, reported with this PR).
    private static IReadOnlyList<Assignment> ListedOnly(
        DmlCaseRow row,
        IReadOnlyList<Assignment> set,
        string[] columns)
    {
        if (row.Dbms != Dbms.MySql)
        {
            return set;
        }

        SpecRewrite listed = SpecRewrite.Expressions(e => e is ColumnRef { Table: "u" or "x" } c
            && (c.Table == BuildContext.ExcludedKey) != columns.Contains(c.Column)
            ? c.Type == SpecType.Text ? new Literal("Xu") : new Literal(7)
            : e);
        return [.. set.Select(a => a with { Value = a.Value.Rewrite(listed) })];
    }

    // The id and code are always listed: every key a conflict can meet is one the row names.
    private static string[] Columns(string[] others, int take, int idAt, int codeAt)
    {
        List<string> columns = [.. others.Take(take)];
        columns.Insert(Math.Min(idAt, columns.Count), "id");
        columns.Insert(Math.Min(codeAt, columns.Count), "code");
        return [.. columns];
    }

    private static List<(long Id, long? Code)> Keys(
        DmlCaseRow row,
        int count,
        int some,
        int[] picks,
        int[] kinds,
        int freshBase,
        bool[] nullCodes)
    {
        bool[] conflicting = [.. Enumerable.Range(0, count).Select(r => row.Conflict switch
        {
            ConflictLevel.All => true,
            ConflictLevel.None => false,

            // A non-empty proper subset of the rows, read from the bits of `some`.
            _ => ((some % ((1 << count) - 2)) + 1 & (1 << r)) != 0,
        })];

        // A code-only conflict needs the seed row's code; a code arbiter needs one throughout.
        List<UserRow> candidates = [.. RandomSchema.Users];
        List<(long Id, long? Code)> keys = [];
        for (int r = 0; r < count; r++)
        {
            long freshId = FreshIdBase + (freshBase * 10) + r;
            long? freshCode = nullCodes[r] ? null : FreshCodeBase + (freshBase * 10) + r;
            if (!conflicting[r])
            {
                keys.Add((freshId, freshCode));
                continue;
            }

            int kind = row.Arbiter switch
            {
                ArbiterLevel.Id => kinds[r] % 2,
                ArbiterLevel.Code => kinds[r] == 1 ? 2 : kinds[r],
                _ => kinds[r],
            };
            List<UserRow> usable = kind == 1 && row.Arbiter != ArbiterLevel.Code
                ? candidates
                : [.. candidates.Where(u => u.Code is not null)];
            UserRow seed = usable[picks[r] % usable.Count];
            candidates.Remove(seed);
            keys.Add(kind switch
            {
                0 => (seed.Id, seed.Code),
                1 => (seed.Id, freshCode),
                _ => (freshId, seed.Code),
            });
        }

        return keys;
    }

    private static Gen<SpecExpression> Value(DmlCaseRow row, SpecType type) =>
        from expression in Chance(20)
        from value in DmlGenerators.InsertValue(
            new GenScope(row.Dbms, ["u4"], ["u4"], 1, false, Correlation.Never),
            expression ? DmlValueLevel.Expression : DmlValueLevel.Constant,
            type)
        select value;

    /// <summary>
    /// What a SET and a DO UPDATE WHERE read: the existing row <c>u</c> and the proposed one.
    /// MySQL and SQLite apply a batch row by row, so a later row's subquery over the target
    /// would read an earlier row's write; theirs read <c>rt_depts</c> in a batch.
    /// </summary>
    private static GenScope Scope(DmlCaseRow row, bool multi)
    {
        string[] outer = row.TargetAlias == TargetAliasLevel.Aliased
            ? ["u", BuildContext.ExcludedKey]
            : [BuildContext.ExcludedKey];
        return new GenScope(
            row.Dbms,
            ["u", BuildContext.ExcludedKey],
            outer,
            1,
            AllowsSubquery: true,
            Correlation.Optional,
            ReadsUsers: !(multi && row.Dbms is Dbms.MySql or Dbms.Sqlite),
            TextScalarsByRow: true);
    }

    private static Gen<IReadOnlyList<Assignment>> Assignments(DmlCaseRow row, bool multi)
    {
        if (row.UpdateValue == UpdateValueLevel.None)
        {
            return Gen.Const<IReadOnlyList<Assignment>>([]);
        }

        GenScope scope = Scope(row, multi);
        return
            from columns in DmlGenerators.SettableOrder()
            from count in Gen.Int[1, 3]
            from name in SetValue(scope, row.UpdateValue, SpecType.Text)
            from age in SetValue(scope, row.UpdateValue, SpecType.Int)
            from dept in SetValue(scope, row.UpdateValue, SpecType.Int)
            from writesCode in Chance(row.Dbms == Dbms.MySql ? 0 : 15)
            let set = (IReadOnlyList<Assignment>)[.. columns.Take(count).Select(c => new Assignment(
                c,
                c switch
                {
                    "name" => name,
                    "age" => age,
                    _ => dept,
                })),
                .. writesCode ? [WrittenCode()] : Array.Empty<Assignment>()]
            select DmlGenerators.OrderedForMySql(row, set);
    }

    // A key column set into a range no row holds: a row-by-row unique check sees no
    // transient duplicate.
    private static Assignment WrittenCode() => new(
        "code",
        new FunctionCall(
            "+",
            SpecType.Int,
            [new ColumnRef("u", "id"), new Literal(WrittenCodeOffset)]));

    private static Gen<SpecExpression> SetValue(
        GenScope scope,
        UpdateValueLevel level,
        SpecType type)
    {
        bool text = type == SpecType.Text;
        string column = text ? "name" : "age";
        GenScope plain = scope with { AllowsSubquery = false };
        GenScope existing = plain with { Tables = ["u"] };
        return level switch
        {
            UpdateValueLevel.Excluded =>
                from column2 in text ? Gen.Const("name") : Gen.OneOf("age", "dept_id")
                from fallback in Chance(30)
                let excluded = new ColumnRef(BuildContext.ExcludedKey, column2)
                select fallback
                    ? (SpecExpression)new FunctionCall(
                        "Coalesce",
                        type,
                        [excluded, new ColumnRef("u", column2)])
                    : excluded,
            UpdateValueLevel.Existing => text
                ? TextExpr(existing, 1, caseMapping: true)
                : IntExpr(existing, 1),
            UpdateValueLevel.Expression => text
                ? TextExpr(plain, 2, caseMapping: true)
                : IntExpr(plain, 2),
            _ => DmlGenerators.ScalarSubqueryValue(scope, text),
        };
    }
}

/// <summary>
/// CsCheck generators for a MERGE that fits one <c>merge.cases.csv</c> row. The source's ids
/// are unique and the ON equates the target's id to them, so a target row meets one source row
/// at most; no branch sets the id the ON reads.
/// </summary>
internal static class MergeGenerators
{
    private static readonly int[] s_offsets = [0, 3, 10, 20, 25];

    private static readonly string[] s_ordinalNames = ["c0", "c1", "c2", "c3"];

    // An inserted code lands apart from the seed's codes and the upsert's.
    private const int InsertedCodeOffset = 2000;

    public static Gen<DmlSpec> Spec(DmlCaseRow row) =>
        from sameNames in Chance(50)
        from source in Source(row, sameNames ? MergeSource.Columns : s_ordinalNames)
        from branches in Branches(row, source)
        from unaliased in DmlGenerators.Unaliased(row)
        select new DmlSpec
        {
            Kind = DmlKind.Merge,
            Dbms = row.Dbms,
            Unaliased = unaliased,
            Merge = new MergeShape(
                source,
                new Comparison("==", new ColumnRef("u", "id"), Key(source)),
                branches),
        };

    private static SpecExpression Key(MergeSource source) => source.Kind == MergeSourceLevel.Table
        ? new FunctionCall(
            "+",
            SpecType.Int,
            [new ColumnRef(BuildContext.SourceKey, "id"), new Literal(source.Offset)])
        : new ColumnRef(BuildContext.SourceKey, "id");

    private static Gen<MergeSource> Source(DmlCaseRow row, string[] names)
    {
        switch (row.MergeSource)
        {
            case MergeSourceLevel.Table:
                return Gen.OneOf(s_offsets.Skip(1).ToArray())
                    .Select(k => new MergeSource(row.MergeSource, MergeSource.Columns, k));
            case MergeSourceLevel.Values:
                return
                    from count in Gen.Int[1, 4]
                    from ids in Gen.Int[1, 40].Array[4]
                    from texts in TextLiteral().Array[4]
                    from ages in IntLiteral().Array[4]
                    from depts in IntLiteral().Array[4]
                    from nulls in Chance(25).Array[12]
                    select new MergeSource(row.MergeSource, names, 0)
                    {
                        Rows = [.. ids.Distinct().Take(count).Select((id, r) =>
                            (IReadOnlyList<SpecExpression>)
                            [
                                new Literal(id),
                                Nullable(texts[r], r, nulls[r * 3], SpecType.Text),
                                Nullable(ages[r], r, nulls[(r * 3) + 1], SpecType.Int),
                                Nullable(depts[r], r, nulls[(r * 3) + 2], SpecType.Int),
                            ])],
                    };
            default:
                return
                    from users in Chance(70)
                    let table = users ? "u5" : "d5"
                    from offset in Gen.OneOf(s_offsets)
                    from filtered in Chance(80)
                    from filter in Condition(
                        new GenScope(row.Dbms, [table], [table], 2, false, Correlation.Never),
                        1)
                    select new MergeSource(row.MergeSource, names, offset)
                    {
                        Body = new QueryBlock
                        {
                            From = table,
                            Items =
                            [
                                new FunctionCall(
                                    "+",
                                    SpecType.Int,
                                    [new ColumnRef(table, "id"), new Literal(offset)]),
                                new ColumnRef(table, "name"),
                                new ColumnRef(table, users ? "age" : "id"),
                                new ColumnRef(table, users ? "dept_id" : "id"),
                            ],
                            Where = filtered ? filter : null,
                            AliasesItems = true,
                            ItemNames = names,
                        },
                    };
        }
    }

    // PostgreSQL types a VALUES column NULL in every row as text and SQL Server as int, which
    // a text or integer target column then refuses; the first row is never NULL.
    private static SpecExpression Nullable(
        SpecExpression value,
        int row,
        bool isNull,
        SpecType type) => row > 0 && isNull ? new NullValue(type) : value;

    private static Gen<IReadOnlyList<MergeBranch>> Branches(DmlCaseRow row, MergeSource source)
    {
        GenScope both = Scope(row, ["u", BuildContext.SourceKey]);
        GenScope sourceOnly = Scope(row, [BuildContext.SourceKey]);
        GenScope targetOnly = Scope(row, ["u"]);
        return
            from matched in Matched(row, both)
            from notMatched in row.NotMatched
                ? Insert(row, sourceOnly, source).Select(b => (MergeBranch?)b)
                : Nothing<MergeBranch>()
            from bySource in BySource(row, targetOnly)
            from firstNotMatched in Chance(row.Dbms == Dbms.Oracle ? 0 : 40)
            from lastBySource in Chance(50)
            select Ordered(matched, notMatched, bySource, firstNotMatched, lastBySource);
    }

    // Oracle's documented grammar puts the update clause before the insert clause; each clause
    // keeps its own branches' order, which decides the first that applies.
    private static IReadOnlyList<MergeBranch> Ordered(
        IReadOnlyList<MergeBranch> matched,
        MergeBranch? notMatched,
        MergeBranch? bySource,
        bool firstNotMatched,
        bool lastBySource)
    {
        List<MergeBranch> branches = [.. matched];
        if (notMatched is not null)
        {
            branches.Insert(firstNotMatched ? 0 : branches.Count, notMatched);
        }

        if (bySource is not null)
        {
            branches.Insert(lastBySource ? branches.Count : 0, bySource);
        }

        return branches;
    }

    /// <summary>A branch's scope: the subquery level decides whether a value or condition holds
    /// a subquery and whether it correlates to the rows the branch reads.</summary>
    private static GenScope Scope(DmlCaseRow row, string[] tables) => new(
        row.Dbms,
        tables,
        tables,
        1,
        AllowsSubquery: row.MergeSubquery != MergeSubqueryLevel.None,
        row.MergeSubquery == MergeSubqueryLevel.Correlated
            ? Correlation.Required
            : Correlation.Never,
        TextScalarsByRow: true);

    private static Gen<IReadOnlyList<MergeBranch>> Matched(DmlCaseRow row, GenScope scope)
    {
        Gen<SpecCondition> condition = Condition(scope, 1);
        bool filtered = row.MatchedFilter == MergeFilterLevel.WhenAnd;
        MergeBranch Delete(SpecCondition? when) =>
            new(MergeClause.Matched, MergeAction.Delete, when);
        return
            from set in Set(scope, row)
            from first in condition
            from second in condition
            from action in condition
            from deleteFirst in Gen.Bool
            select row.Matched switch
            {
                MergeMatchedLevel.None => (IReadOnlyList<MergeBranch>)[],
                MergeMatchedLevel.Update => [Update(set, filtered ? first : null, row, action)],
                MergeMatchedLevel.Delete => [Delete(filtered ? first : null)],

                // SQL Server and PostgreSQL refuse a branch after an unconditioned one of its
                // clause (ContextRules.CheckRepeatedMergeBranch), so the first is conditioned.
                MergeMatchedLevel.UpdateDelete => deleteFirst
                    ? [Delete(first), Update(set, filtered ? second : null, row, action)]
                    : [Update(set, first, row, action), Delete(filtered ? second : null)],
                _ => [Update(set, null, row, action) with { DeleteWhere = second }],
            };
    }

    private static MergeBranch Update(
        IReadOnlyList<Assignment> set,
        SpecCondition? when,
        DmlCaseRow row,
        SpecCondition action) =>
        new(MergeClause.Matched, MergeAction.Update, when)
        {
            Set = set,
            ActionWhere = row.MatchedFilter == MergeFilterLevel.ActionWhere ? action : null,
        };

    private static Gen<MergeBranch?> BySource(DmlCaseRow row, GenScope scope)
    {
        if (row.BySource == BySourceLevel.None)
        {
            return Nothing<MergeBranch>();
        }

        return
            from conditioned in Chance(50)
            from when in Condition(scope, 1)
            from set in Set(scope, row)
            select (MergeBranch?)new MergeBranch(
                MergeClause.BySource,
                row.BySource == BySourceLevel.Delete ? MergeAction.Delete : MergeAction.Update,
                conditioned ? when : null)
            {
                Set = set,
            };
    }

    // The inserted id is the ON key, which no target row holds when the branch runs.
    private static Gen<MergeBranch> Insert(DmlCaseRow row, GenScope scope, MergeSource source) =>
        from columns in DmlGenerators.SettableOrder()
        from count in Gen.Int[0, 3]
        from withCode in Chance(20)
        from name in Value(scope, SpecType.Text)
        from age in Value(scope, SpecType.Int)
        from dept in Value(scope, SpecType.Int)
        from condition in Condition(scope, 1)
        let listed = columns.Take(count).ToList()
        select new MergeBranch(
            MergeClause.NotMatched,
            MergeAction.Insert,
            row.NotMatchedFilter == MergeFilterLevel.WhenAnd ? condition : null)
        {
            InsertColumns = ["id", .. listed, .. withCode ? ["code"] : Array.Empty<string>()],
            InsertValues =
            [
                Key(source),
                .. listed.Select(c => c switch
                {
                    "name" => name,
                    "age" => age,
                    _ => dept,
                }),
                .. withCode
                    ? [new FunctionCall(
                        "+",
                        SpecType.Int,
                        [Key(source), new Literal(InsertedCodeOffset)])]
                    : Array.Empty<SpecExpression>(),
            ],
            ActionWhere = row.NotMatchedFilter == MergeFilterLevel.ActionWhere ? condition : null,
        };

    private static Gen<IReadOnlyList<Assignment>> Set(GenScope scope, DmlCaseRow row) =>
        from columns in DmlGenerators.SettableOrder()
        from count in Gen.Int[1, 3]
        from name in Value(scope, SpecType.Text)
        from age in Value(scope, SpecType.Int)
        from dept in Value(scope, SpecType.Int)
        select (IReadOnlyList<Assignment>)[.. columns.Take(count).Select(c => new Assignment(
            c,
            c switch
            {
                "name" => name,
                "age" => age,
                _ => dept,
            }))];

    // A plain column or expression over the branch's rows, or a scalar subquery when the row's
    // level asks for one.
    private static Gen<SpecExpression> Value(GenScope scope, SpecType type)
    {
        bool text = type == SpecType.Text;
        GenScope plain = scope with { AllowsSubquery = false };
        Gen<SpecExpression> expression = text
            ? TextExpr(plain, 1, caseMapping: true)
            : IntExpr(plain, 1);
        return scope.AllowsSubquery
            ? Gen.Frequency(
                (1, expression),
                (2, DmlGenerators.ScalarSubqueryValue(scope, text)))
            : expression;
    }
}
