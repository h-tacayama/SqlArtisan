using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.NameResolution;

public enum NrRun
{
    Query,
    Dml,        // run in a rolled-back transaction, then read nr_w back as id|b
    DmlReturns, // the same, reading the statement's own RETURNING / OUTPUT rows
}

/// <summary>One engine Example. Intended is the result if every reference reads the
/// column its C# names; Other is the result if the bare name lands on the other
/// candidate the key's shadow names (null where there is none to compute).</summary>
public sealed record NrExample(
    string Id,
    NrKey[] Keys,
    Func<Dbms, ISqlBuilder> Make,
    string? Intended,
    string? Other = null,
    NrRun Run = NrRun.Query,
    Func<Dbms, bool>? Supported = null,
    bool Sort = false);

// nr_t / nr_s share id, b, c with b and c permuted between them, so a reference that
// lands on the other table, or on a same-named alias, returns other rows.
public static class NameResolutionExamples
{
    internal static readonly DbTable T = new("nr_t", "t");
    internal static readonly DbTable Tu = new("nr_t");
    internal static readonly DbTable S = new("nr_s", "s");
    internal static readonly DbTable Su = new("nr_s");
    internal static readonly DbTable W = new("nr_w", "w");
    internal static readonly DbTable Wu = new("nr_w");
    private static readonly DbTable Tx = new("nr_t", "x");
    private static readonly DbTable Sx = new("nr_s", "x");

    private const string Rows3 = "1|10,2|20,3|30";

    public static IReadOnlyList<NrExample> All { get; } = Build();

    private static NrKey K(
        NrPosition p, NrRef r, NrOwner o, NrShadow s = NrShadow.None, NrBlock b = NrBlock.Top) =>
        new(p, r, o, s, b);

    // An alias that collides with a bare column: Oracle folds the bare name to upper case
    // and keeps the quoted alias as written, so the colliding alias is upper case there.
    private static string Nm(Dbms d, string name) =>
        d == Dbms.Oracle ? name.ToUpperInvariant() : name;

    private static SqlExpression Agg(Dbms d, DbColumn expr, OrderByClause ob) => d switch
    {
        Dbms.MySql => GroupConcat(expr, ob),
        Dbms.Oracle => Listagg(expr, ",").WithinGroup(ob),
        Dbms.SqlServer => StringAgg(expr, ",").WithinGroup(ob),
        _ => StringAgg(expr, ",", ob),
    };

    private static bool NotSqlServer(Dbms d) => d != Dbms.SqlServer;

    private static bool Lateral(Dbms d) => d is Dbms.PostgreSql or Dbms.MySql or Dbms.Oracle;

    private static bool Upsert(Dbms d) => d is Dbms.PostgreSql or Dbms.Sqlite or Dbms.MySql;

    private static bool OnConflict(Dbms d) => d is Dbms.PostgreSql or Dbms.Sqlite;

    private static bool Merge(Dbms d) => d is Dbms.PostgreSql or Dbms.Oracle or Dbms.SqlServer;

    private static bool ForUpdate(Dbms d) => d is Dbms.PostgreSql or Dbms.MySql or Dbms.Oracle;

    private static ISqlBuilder UpsertOf(Dbms d, Func<EqualityCondition> set) =>
        d == Dbms.MySql
            ? InsertInto(Wu, Wu.Column("id"), Wu.Column("b"), Wu.Column("c"))
                .Values(1, 77, "z").OnDuplicateKeyUpdate(set())
            : InsertInto(Wu, Wu.Column("id"), Wu.Column("b"), Wu.Column("c"))
                .Values(1, 77, "z").OnConflict(Wu.Column("id")).DoUpdateSet(set());

    private static List<NrExample> Build()
    {
        const NrRef Q = NrRef.QualifiedColumn;
        const NrRef B = NrRef.BareColumn;
        const NrRef A = NrRef.SelectAlias;
        const NrRef P = NrRef.PseudoRow;
        const NrOwner Own = NrOwner.Own;
        const NrOwner Outer = NrOwner.OuterListed;
        const NrOwner Unl = NrOwner.Unlisted;
        const NrOwner Other = NrOwner.OtherTable;
        DerivedTable l = new("l");
        Cte c = new("c");

        return
        [
            // --- query blocks, top level -------------------------------------------
            new("SEL-Q", [K(NrPosition.SelectItem, Q, Own), K(NrPosition.OrderBy, Q, Own)],
                _ => Select(T.Column("id"), T.Column("b")).From(T).OrderBy(T.Column("id")),
                Rows3),
            new("SEL-B", [K(NrPosition.SelectItem, B, Own), K(NrPosition.OrderBy, B, Own)],
                _ => Select(Tu.Column("id"), Tu.Column("b")).From(Tu).OrderBy(Tu.Column("id")),
                Rows3),
            new("SEL-B-SIBLING", [K(NrPosition.SelectItem, B, Own, NrShadow.SiblingColumn)],
                _ => Select(Tu.Column("b")).From(Tu).InnerJoin(S)
                    .On(S.Column("id") * 100 == Tu.Column("k")),
                null),
            new("WHERE-B-UNLISTED-SIBLING",
                [K(NrPosition.Where, B, Unl, NrShadow.SiblingColumn)],
                _ => Select(S.Column("id")).From(S).Where(Tu.Column("b") == 10),
                null, Other: "2"),
            new("WHERE-B-UNLISTED", [K(NrPosition.Where, B, Unl)],
                _ => Select(S.Column("id")).From(S).Where(Tu.Column("k") == 100),
                null),
            new("WHERE-Q-UNLISTED", [K(NrPosition.Where, Q, Unl)],
                _ => Select(S.Column("id")).From(S).Where(T.Column("b") == 10),
                null),

            // --- nested blocks -------------------------------------------------------
            new("PRED-B-OUTER", [K(NrPosition.Where, B, Outer, NrShadow.InnerColumn,
                    NrBlock.PredicateSubquery)],
                _ => Select(Tu.Column("id")).From(Tu).Where(Exists(Select(S.Column("id"))
                    .From(S).Where((S.Column("id") == 2) & (S.Column("b") == Tu.Column("b"))))),
                null, Other: "1,2,3"),
            new("PRED-Q-OUTER", [K(NrPosition.Where, Q, Outer, NrShadow.InnerColumn,
                    NrBlock.PredicateSubquery)],
                _ => Select(T.Column("id")).From(T).Where(Exists(Select(S.Column("id"))
                    .From(S).Where((S.Column("id") == 2) & (S.Column("b") == T.Column("b")))))
                    .OrderBy(T.Column("id")),
                "1", Other: "1,2,3"),
            new("PRED-B-OWN", [K(NrPosition.Where, B, Own, b: NrBlock.PredicateSubquery)],
                _ => Select(T.Column("id")).From(T)
                    .Where(T.Column("id") == Select(Su.Column("id")).From(Su)
                        .Where(Su.Column("b") == 10)),
                "2"),
            new("SCALAR-Q-OUTER", [K(NrPosition.Where, Q, Outer, NrShadow.InnerColumn,
                    NrBlock.ScalarSubquery)],
                _ => Select(T.Column("id"), Select(Max(S.Column("id"))).From(S)
                        .Where(S.Column("b") == T.Column("b")))
                    .From(T).OrderBy(T.Column("id")),
                "1|2,2|3,3|1", Other: "1|3,2|3,3|3"),
            new("SCALAR-B-OUTER", [K(NrPosition.Where, B, Outer, NrShadow.InnerColumn,
                    NrBlock.ScalarSubquery)],
                _ => Select(Tu.Column("id"), Select(Max(S.Column("id"))).From(S)
                        .Where(S.Column("b") == Tu.Column("b")))
                    .From(Tu),
                null, Other: "1|3,2|3,3|3"),
            new("DERIVED-B-OWN", [K(NrPosition.SelectItem, B, Own, b: NrBlock.DerivedTable)],
                _ =>
                {
                    SubqueryDerivedTable d = Select(Tu.Column("id"), Tu.Column("b")).From(Tu)
                        .AsTable("d");
                    return Select(d.Column("id"), d.Column("b")).From(d)
                        .OrderBy(d.Column("id"));
                },
                Rows3),
            new("DERIVED-B-UNLISTED", [K(NrPosition.Where, B, Unl, NrShadow.InnerColumn,
                    NrBlock.DerivedTable)],
                _ =>
                {
                    SubqueryDerivedTable d = Select(S.Column("id")).From(S)
                        .Where(Tu.Column("b") == 10).AsTable("d");
                    return Select(d.Column("id")).From(d);
                },
                null, Other: "2"),
            new("CTE-B-OWN", [K(NrPosition.Where, B, Own, b: NrBlock.CteBody)],
                _ => With(c.As(Select(Tu.Column("id")).From(Tu).Where(Tu.Column("b") > 15)))
                    .Select(c.Column("id")).From(c).OrderBy(c.Column("id")),
                "2,3"),
            new("CTE-B-OUTER", [K(NrPosition.Where, B, Outer, NrShadow.InnerColumn,
                    NrBlock.CteBody)],
                _ => With(c.As(Select(Su.Column("id")).From(Su)
                        .Where(Su.Column("b") == Tu.Column("b"))))
                    .Select(Tu.Column("id")).From(Tu, c),
                null),
            new("LATERAL-Q-OUTER", [K(NrPosition.Where, Q, Outer, NrShadow.InnerColumn,
                    NrBlock.Lateral)],
                _ => Select(T.Column("id"), l.Column("x")).From(T)
                    .CrossJoinLateral(Select(Max(S.Column("id")).As(l.Column("x"))).From(S)
                        .Where(S.Column("b") == T.Column("b")), l)
                    .OrderBy(T.Column("id")),
                "1|2,2|3,3|1", Other: "1|3,2|3,3|3", Supported: Lateral),
            new("LATERAL-B-OUTER", [K(NrPosition.Where, B, Outer, NrShadow.InnerColumn,
                    NrBlock.Lateral)],
                _ => Select(Tu.Column("id"), l.Column("x")).From(Tu)
                    .CrossJoinLateral(Select(Max(S.Column("id")).As(l.Column("x"))).From(S)
                        .Where(S.Column("b") == Tu.Column("b")), l),
                null),

            // --- compounds -----------------------------------------------------------
            new("BRANCH-B-UNLISTED", [K(NrPosition.Where, B, Unl, NrShadow.SiblingColumn,
                    NrBlock.CompoundBranch)],
                _ => Select(Tu.Column("id")).From(Tu).Where(Tu.Column("b") == 10)
                    .UnionAll.Select(S.Column("id")).From(S).Where(Tu.Column("b") == 10),
                null, Other: "1,2", Sort: true),
            new("CORD-Q", [K(NrPosition.CompoundOrderBy, Q, Own, b: NrBlock.CompoundBranch)],
                _ => Select(T.Column("id"), T.Column("b")).From(T)
                    .UnionAll.Select(S.Column("id"), S.Column("b")).From(S)
                    .OrderBy(T.Column("b"), T.Column("id")),
                "1|10,2|10,2|20,3|20,1|30,3|30"),
            new("CORD-A-FIRST", [K(NrPosition.CompoundOrderBy, A, Own, b: NrBlock.CompoundBranch)],
                _ =>
                {
                    ExpressionAlias i = T.Column("id").As("i");
                    ExpressionAlias x = T.Column("b").As("x");
                    return Select(i, x).From(T)
                        .UnionAll.Select(S.Column("id"), S.Column("b")).From(S).OrderBy(x, i);
                },
                "1|10,2|10,2|20,3|20,1|30,3|30"),
            new("CORD-A-LATER", [K(NrPosition.CompoundOrderBy, A, Unl, b: NrBlock.CompoundBranch)],
                _ =>
                {
                    ExpressionAlias i = S.Column("id").As("i");
                    ExpressionAlias x = S.Column("b").As("x");
                    return Select(T.Column("id"), T.Column("b")).From(T)
                        .UnionAll.Select(i, x).From(S).OrderBy(x, i);
                },
                "1|10,2|10,2|20,3|20,1|30,3|30"),

            // --- alias collisions ----------------------------------------------------
            new("ORD-A-OWN", [K(NrPosition.OrderBy, A, Own)],
                _ =>
                {
                    ExpressionAlias x = (T.Column("b") * -1).As("x");
                    return Select(T.Column("id"), x).From(T).OrderBy(x);
                },
                "3|-30,2|-20,1|-10"),
            new("ORD-B-ALIAS", [K(NrPosition.OrderBy, B, Own, NrShadow.SelectAlias)],
                d => Select(Tu.Column("id"), (Tu.Column("b") * -1).As(Nm(d, "b"))).From(Tu)
                    .OrderBy(Tu.Column("b")),
                "1|-10,2|-20,3|-30", Other: "3|-30,2|-20,1|-10"),
            new("ORD-Q-ALIAS", [K(NrPosition.OrderBy, Q, Own, NrShadow.SelectAlias)],
                d => Select(T.Column("id"), (T.Column("b") * -1).As(Nm(d, "b"))).From(T)
                    .OrderBy(T.Column("b")),
                "1|-10,2|-20,3|-30", Other: "3|-30,2|-20,1|-10"),
            new("ORD-A-UNSELECTED", [K(NrPosition.OrderBy, A, Unl, NrShadow.SelectAlias)],
                d => Select(T.Column("id")).From(T)
                    .OrderBy((T.Column("b") * -1).As(Nm(d, "b"))),
                "3,2,1", Other: "1,2,3"),
            new("GRP-B-ALIAS", [K(NrPosition.GroupBy, B, Own, NrShadow.SelectAlias)],
                d => Select(Count(Tu.Column("id")), (Tu.Column("g") - Tu.Column("g"))
                    .As(Nm(d, "g"))).From(Tu).GroupBy(Tu.Column("g")),
                "1|0,2|0", Other: "3|0", Sort: true),
            new("GRP-A", [K(NrPosition.GroupBy, A, Own)],
                _ =>
                {
                    ExpressionAlias x = (T.Column("g") * 1).As("x");
                    return Select(Count(T.Column("id")), x).From(T).GroupBy(x);
                },
                "1|2,2|1", Sort: true),
            new("HAV-B-ALIAS", [K(NrPosition.Having, B, Own, NrShadow.SelectAlias)],
                d => Select(Count(Tu.Column("id")).As(Nm(d, "g"))).From(Tu)
                    .GroupBy(Tu.Column("g")).Having(Tu.Column("g") == 1),
                "2", Other: "1"),
            new("WHERE-B-ALIAS", [K(NrPosition.Where, B, Own, NrShadow.SelectAlias)],
                d => Select(Tu.Column("id"), (Tu.Column("b") * -1).As(Nm(d, "b"))).From(Tu)
                    .Where(Tu.Column("b") == 10),
                "1|-10", Other: ""),
            new("WORD-B-ALIAS", [K(NrPosition.WindowOrder, B, Own, NrShadow.SelectAlias)],
                d => Select(Tu.Column("id"), (Tu.Column("b") * -1).As(Nm(d, "b")),
                        RowNumber().Over(OrderBy(Tu.Column("b"))))
                    .From(Tu).OrderBy(Tu.Column("id")),
                "1|-10|1,2|-20|2,3|-30|3", Other: "1|-10|3,2|-20|2,3|-30|1"),
            new("WPART-B-ALIAS", [K(NrPosition.WindowPartition, B, Own, NrShadow.SelectAlias)],
                d => Select(Tu.Column("id"), (Tu.Column("g") - Tu.Column("g")).As(Nm(d, "g")),
                        RowNumber().Over(PartitionBy(Tu.Column("g")).OrderBy(Tu.Column("id"))))
                    .From(Tu).OrderBy(Tu.Column("id")),
                "1|0|1,2|0|2,3|0|1", Other: "1|0|1,2|0|2,3|0|3"),
            new("AGG-B-ALIAS", [K(NrPosition.AggregateOrder, B, Own, NrShadow.SelectAlias)],
                d => Select(Agg(d, Tu.Column("c"), OrderBy(Tu.Column("b"))),
                        (Min(Tu.Column("b")) * -1).As(Nm(d, "b")))
                    .From(Tu),
                "p,q,r|-10"),
            new("WORD-A", [K(NrPosition.WindowOrder, A, Own)],
                _ =>
                {
                    ExpressionAlias x = (T.Column("b") * -1).As("x");
                    return Select(T.Column("id"), x, RowNumber().Over(OrderBy(x))).From(T);
                },
                null),
            new("DON-B-ALIAS", [K(NrPosition.DistinctOn, B, Own, NrShadow.SelectAlias)],
                _ => Select(DistinctOn(Tu.Column("g")), Tu.Column("id"),
                        (Tu.Column("g") - Tu.Column("g")).As("g"))
                    .From(Tu).OrderBy(Tu.Column("g"), Tu.Column("id")),
                "1|0,3|0", Other: "1|0", Supported: d => d == Dbms.PostgreSql),

            // --- correlation names and USING -----------------------------------------
            new("PRED-Q-SAMEALIAS", [K(NrPosition.Where, Q, Outer, NrShadow.SameAlias,
                    NrBlock.PredicateSubquery)],
                _ => Select(Tx.Column("id")).From(Tx).Where(Exists(Select(Sx.Column("id"))
                    .From(Sx).Where((Sx.Column("id") == 2) & (Sx.Column("b") == Tx.Column("b")))))
                    .OrderBy(Tx.Column("id")),
                "1", Other: "1,2,3"),
            new("ON-Q-SAMEALIAS", [K(NrPosition.JoinOn, Q, Own, NrShadow.SameAlias)],
                _ => Select(Tx.Column("id")).From(Tx).InnerJoin(Sx)
                    .On(Sx.Column("id") == Tx.Column("id")),
                null),
            new("USING-Q", [K(NrPosition.SelectItem, Q, Own, NrShadow.UsingColumn),
                    K(NrPosition.JoinUsing, Q, Own)],
                _ => Select(T.Column("id"), T.Column("b")).From(T).InnerJoin(S)
                    .Using(T.Column("id")).OrderBy(T.Column("id")),
                Rows3, Supported: NotSqlServer),
            new("USING-B", [K(NrPosition.SelectItem, B, Own, NrShadow.UsingColumn),
                    K(NrPosition.JoinUsing, B, Own)],
                _ => Select(Tu.Column("id"), Tu.Column("k")).From(Tu).InnerJoin(Su)
                    .Using(Tu.Column("id")).OrderBy(Tu.Column("id")),
                "1|100,2|200,3|300", Supported: NotSqlServer),
            new("USING-OTHER", [K(NrPosition.JoinUsing, Q, Unl)],
                _ => Select(T.Column("id"), S.Column("b")).From(T).InnerJoin(S)
                    .Using(W.Column("id")).OrderBy(T.Column("id")),
                null, Other: "1|30,2|10,3|20", Supported: NotSqlServer),
            new("FOU-Q-OWN", [K(NrPosition.ForUpdateOf, Q, Own)],
                _ => Select(T.Column("id")).From(T).Where(T.Column("id") == 1)
                    .ForUpdate(Of(T), Nowait),
                "1", Supported: ForUpdate),
            new("FOU-Q-UNLISTED", [K(NrPosition.ForUpdateOf, Q, Unl)],
                _ => Select(T.Column("id")).From(T).Where(T.Column("id") == 1)
                    .ForUpdate(Of(S), Nowait),
                null, Supported: ForUpdate),
            new("SEL-P", [K(NrPosition.SelectItem, P, Own)],
                _ => Select(Excluded(T.Column("b"))).From(T),
                null),

            // --- UPDATE / DELETE -----------------------------------------------------
            new("UPD-B-OWN", [K(NrPosition.UpdateSetTarget, B, Own),
                    K(NrPosition.UpdateSetValue, B, Own), K(NrPosition.DmlWhere, B, Own)],
                _ => Update(Wu).Set(Wu.Column("b") == Wu.Column("b") + 89)
                    .Where(Wu.Column("id") == 1),
                "1|99,2|20,3|30", Run: NrRun.Dml),
            new("UPD-B-OTHER", [K(NrPosition.UpdateSetTarget, B, Other)],
                _ => Update(Wu).Set(Su.Column("b") == 99).Where(Wu.Column("id") == 1),
                null, Run: NrRun.Dml),
            new("UPD-Q-OTHER-SWAP", [K(NrPosition.UpdateSetTarget, Q, Other)],
                _ => Update(W).Set(S.Column("b") == W.Column("b")).From(S)
                    .Where(S.Column("id") == W.Column("id")),
                null, Run: NrRun.Dml),
            new("UPD-Q-OUTER", [K(NrPosition.UpdateSetValue, Q, Outer, NrShadow.InnerColumn,
                    NrBlock.PredicateSubquery)],
                _ => Update(W).Set(W.Column("b") == Select(Max(S.Column("id"))).From(S)
                    .Where(S.Column("b") == W.Column("b"))),
                "1|2,2|3,3|1", Other: "1|3,2|3,3|3", Run: NrRun.Dml, Supported: NotSqlServer),
            new("UPD-B-OUTER", [K(NrPosition.DmlWhere, B, Outer, NrShadow.InnerColumn,
                    NrBlock.PredicateSubquery)],
                _ => Update(Wu).Set(Wu.Column("b") == 0).Where(Exists(Select(S.Column("id"))
                    .From(S).Where(S.Column("b") == Wu.Column("b")))),
                null, Run: NrRun.Dml),
            new("DEL-Q-OUTER", [K(NrPosition.DmlWhere, Q, Outer, NrShadow.InnerColumn,
                    NrBlock.PredicateSubquery)],
                _ => DeleteFrom(W).Where(Exists(Select(S.Column("id")).From(S)
                    .Where((S.Column("id") == 2) & (S.Column("b") == W.Column("b"))))),
                "2|20,3|30", Other: "", Run: NrRun.Dml, Supported: NotSqlServer),

            // --- INSERT / upsert -----------------------------------------------------
            new("INS-B-OWN", [K(NrPosition.InsertColumnList, B, Own)],
                _ => InsertInto(Wu, Wu.Column("id"), Wu.Column("b"), Wu.Column("c"))
                    .Values(4, 40, "s"),
                "1|10,2|20,3|30,4|40", Run: NrRun.Dml),
            new("INS-B-OTHER", [K(NrPosition.InsertColumnList, B, Other)],
                _ => InsertInto(Wu, Su.Column("id")).Values(9),
                null, Run: NrRun.Dml),
            new("UPS-P-OWN", [K(NrPosition.UpsertSetValue, P, Own),
                    K(NrPosition.UpsertSetTarget, B, Own), K(NrPosition.ConflictTarget, B, Own)],
                d => UpsertOf(d, () => Wu.Column("b") == Excluded(Wu.Column("b"))),
                "1|77,2|20,3|30", Run: NrRun.Dml, Supported: Upsert),
            new("UPS-P-OTHER", [K(NrPosition.UpsertSetValue, P, Other)],
                d => UpsertOf(d, () => Wu.Column("b") == Excluded(Su.Column("b"))),
                null, Run: NrRun.Dml, Supported: Upsert),
            new("UPS-B-VALUE", [K(NrPosition.UpsertSetValue, B, Own)],
                d => UpsertOf(d, () => Wu.Column("b") == Wu.Column("b") + 1),
                "1|11,2|20,3|30", Run: NrRun.Dml, Supported: Upsert),
            new("UPS-B-OUTER", [K(NrPosition.UpsertSetValue, B, Outer, NrShadow.InnerColumn,
                    NrBlock.PredicateSubquery)],
                d => UpsertOf(d, () => Wu.Column("b") == Select(Max(S.Column("id"))).From(S)
                    .Where(S.Column("b") == Wu.Column("b"))),
                null, Run: NrRun.Dml, Supported: Upsert),
            new("UPS-B-SUBQUERY-OWN", [K(NrPosition.UpsertSetValue, B, Own,
                    b: NrBlock.PredicateSubquery)],
                d => UpsertOf(d, () => Wu.Column("b") == Select(Max(Su.Column("b"))).From(Su)),
                "1|30,2|20,3|30", Run: NrRun.Dml, Supported: Upsert),
            new("UPS-B-TARGET-OTHER", [K(NrPosition.UpsertSetTarget, B, Other)],
                d => UpsertOf(d, () => Su.Column("b") == 1),
                null, Run: NrRun.Dml, Supported: Upsert),
            new("CONFLICT-B-OTHER", [K(NrPosition.ConflictTarget, B, Other)],
                _ => InsertInto(Wu, Wu.Column("id"), Wu.Column("b")).Values(1, 5)
                    .OnConflict(Su.Column("id")).DoNothing(),
                null, Run: NrRun.Dml, Supported: OnConflict),

            // --- MERGE ---------------------------------------------------------------
            new("MRG-Q-OWN", [K(NrPosition.MergeOn, Q, Own), K(NrPosition.MergeSetTarget, Q, Own),
                    K(NrPosition.MergeSetValue, Q, Own)],
                _ => MergeInto(W).Using(S).On(W.Column("id") == S.Column("id"))
                    .WhenMatched().ThenUpdateSet(W.Column("b") == S.Column("b")),
                "1|30,2|10,3|20", Run: NrRun.Dml, Supported: Merge),
            new("MRG-Q-OTHER-SWAP", [K(NrPosition.MergeSetTarget, Q, Other)],
                _ => MergeInto(W).Using(S).On(W.Column("id") == S.Column("id"))
                    .WhenMatched().ThenUpdateSet(S.Column("b") == W.Column("b")),
                null, Run: NrRun.Dml, Supported: Merge),
            new("MRG-INSLIST-OTHER", [K(NrPosition.MergeInsertList, Q, Other)],
                _ => MergeInto(W).Using(S).On(W.Column("id") == S.Column("id"))
                    .WhenNotMatched().ThenInsert(S.Column("id")).Values(S.Column("id")),
                null, Run: NrRun.Dml, Supported: Merge),
            new("MRG-B-SIBLING", [K(NrPosition.MergeOn, B, Own, NrShadow.SiblingColumn)],
                _ => MergeInto(Wu).Using(Su).On(Wu.Column("id") == Su.Column("id"))
                    .WhenMatched().ThenUpdateSet(Wu.Column("c") == "m"),
                null, Run: NrRun.Dml, Supported: Merge),

            // --- RETURNING / OUTPUT --------------------------------------------------
            new("RET-Q-OWN", [K(NrPosition.Returning, Q, Own)],
                _ => Update(W).Set(W.Column("b") == 5).Where(W.Column("id") == 1)
                    .Returning(W.Column("b")),
                "5", Run: NrRun.DmlReturns, Supported: d => d is Dbms.PostgreSql or Dbms.Sqlite),
            new("RET-Q-SAMETABLE", [K(NrPosition.Returning, Q, Outer, NrShadow.SameTableName,
                    NrBlock.PredicateSubquery)],
                _ => Update(W).Set(W.Column("b") == 5).Where(W.Column("id") == 1)
                    .Returning(Select(Count(Wu.Column("id"))).From(Wu)
                        .Where(Wu.Column("id") == W.Column("id"))),
                "1", Other: "3", Run: NrRun.DmlReturns,
                Supported: d => d is Dbms.PostgreSql or Dbms.Sqlite),
            new("OUT-P-OWN", [K(NrPosition.Output, P, Own)],
                _ => Update(Wu).Set(Wu.Column("b") == 5).Output(Inserted(Wu.Column("b")))
                    .Where(Wu.Column("id") == 1),
                "5", Run: NrRun.DmlReturns, Supported: d => d == Dbms.SqlServer),
            new("OUT-P-OTHER", [K(NrPosition.Output, P, Other)],
                _ => Update(Wu).Set(Wu.Column("b") == 5).Output(Inserted(Su.Column("b")))
                    .Where(Wu.Column("id") == 1),
                null, Run: NrRun.DmlReturns, Supported: d => d == Dbms.SqlServer),
            new("OUT-B-OWN", [K(NrPosition.Output, B, Own)],
                _ => Update(Wu).Set(Wu.Column("b") == 5).Output(Wu.Column("b"))
                    .Where(Wu.Column("id") == 1),
                null, Run: NrRun.DmlReturns, Supported: d => d == Dbms.SqlServer),
        ];
    }
}
