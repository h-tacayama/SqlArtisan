using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;

namespace SqlArtisan.Analyzers.Tests;

public class CorrelatedDmlAnalyzerTests
{
    // The marked span is the correlated column reference — the evidence that the
    // statement is correlated; one diagnostic per chain.
    private static string Usage(string statements) => $$"""
        using SqlArtisan;
        using SqlArtisan.Internal;
        using static SqlArtisan.Sql;

        class T : DbTableBase
        {
            public DbColumn Id;
            public DbColumn Dep;
            public T(string alias = "") : base("t", alias) { Id = new DbColumn(this, "id"); Dep = new DbColumn(this, "dep"); }
        }

        class C
        {
            void M()
            {
                T t = new T();
                T r = new T("r");
                {{statements}}
            }
        }
        """;

    private static Task RunReporting(string statements) =>
        RunAsync(Usage(statements), AnalyzerVerifier.EditorConfig("postgresql"), expectWarning: true);

    private static Task RunSilent(string statements, string? dbms = "postgresql") =>
        RunAsync(
            AnalyzerVerifier.Unmarked(Usage(statements)),
            dbms is null ? null : AnalyzerVerifier.EditorConfig(dbms),
            expectWarning: false);

    private static async Task RunAsync(string source, string? editorConfig, bool expectWarning)
    {
        var test = AnalyzerVerifier.Create(source, editorConfig);
        if (expectWarning)
        {
            test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0005").WithLocation(0));
        }

        await test.RunAsync();
    }

    [Fact]
    public Task Update_LocalUnaliasedTargetCorrelatedSetSubquery_ReportsSqla0005() =>
        RunReporting("""
            var q = Update(t).Set(t.Id == Select(Max(r.Id)).From(r).Where(r.Dep == {|#0:t.Dep|}));
            """);

    [Fact]
    public Task DeleteFrom_LocalUnaliasedTargetCorrelatedExistsSubquery_ReportsSqla0005() =>
        RunReporting("""
            var q = DeleteFrom(t).Where(Exists(Select(r.Id).From(r).Where(r.Id == {|#0:t.Id|})));
            """);

    [Fact]
    public Task Update_ReadonlyFieldUnaliasedTarget_ReportsSqla0005() =>
        RunAsync("""
            using SqlArtisan;
            using SqlArtisan.Internal;
            using static SqlArtisan.Sql;

            class T : DbTableBase
            {
                public DbColumn Id;
                public DbColumn Dep;
                public T(string alias = "") : base("t", alias) { Id = new DbColumn(this, "id"); Dep = new DbColumn(this, "dep"); }
            }

            class C
            {
                private readonly T _t = new T();

                void M()
                {
                    T r = new T("r");
                    var q = Update(_t).Set(_t.Id == Select(Max(r.Id)).From(r).Where(r.Dep == {|#0:_t.Dep|}));
                }
            }
            """, AnalyzerVerifier.EditorConfig("postgresql"), expectWarning: true);

    [Fact]
    public Task DeleteFrom_SameInstanceInInnerSelect_ReportsSqla0005() =>
        RunReporting("""
            var q = DeleteFrom(t).Where(t.Id.In(Select({|#0:t.Id|}).From(t)));
            """);

    [Fact]
    public Task DeleteFrom_TargetViaExplicitEmptyAlias_ReportsSqla0005() =>
        RunReporting("""
            T u = new T("");
            var q = DeleteFrom(u).Where(Exists(Select(r.Id).From(r).Where(r.Id == {|#0:u.Id|})));
            """);

    [Fact]
    public Task DeleteFrom_TargetViaNamedEmptyAliasArgument_ReportsSqla0005() =>
        RunReporting("""
            T u = new T(alias: "");
            var q = DeleteFrom(u).Where(Exists(Select(r.Id).From(r).Where(r.Id == {|#0:u.Id|})));
            """);

    [Fact]
    public Task DeleteFrom_NestedSubquery_ReportsSqla0005() =>
        RunReporting("""
            T s = new T("s");
            var q = DeleteFrom(t).Where(t.Id.In(Select(r.Id).From(r).Where(r.Id.In(Select(s.Id).From(s).Where(s.Dep == {|#0:t.Dep|})))));
            """);

    [Fact]
    public Task DeleteFrom_WithCtePrefixCorrelatedWhere_ReportsSqla0005() =>
        RunReporting("""
            var cte = new Cte("c");
            var q = With(cte.As(Select(r.Id).From(r))).DeleteFrom(t).Where(Exists(Select(r.Dep).From(r).Where(r.Id == {|#0:t.Id|})));
            """);

    [Theory]
    [InlineData("mysql")]
    [InlineData("oracle")]
    [InlineData("postgresql")]
    [InlineData("sqlite")]
    [InlineData("sqlserver")]
    public Task DeleteFrom_EveryConfiguredDialect_ReportsSqla0005(string dbms) =>
        RunAsync(Usage("""
            var q = DeleteFrom(t).Where(Exists(Select(r.Id).From(r).Where(r.Id == {|#0:t.Id|})));
            """), AnalyzerVerifier.EditorConfig(dbms), expectWarning: true);

    [Fact]
    public Task DeleteFrom_AliasedTarget_StaysSilent() =>
        RunSilent("""
            T a = new T("a");
            var q = DeleteFrom(a).Where(Exists(Select(r.Id).From(r).Where(r.Id == a.Id)));
            """);

    [Fact]
    public Task Update_UncorrelatedSubquery_StaysSilent() =>
        RunSilent("""
            var q = Update(t).Set(t.Id == 1).Where(t.Id.In(Select(r.Id).From(r)));
            """);

    [Fact]
    public Task Update_TargetColumnsOutsideSubquery_StaysSilent() =>
        RunSilent("""
            var q = Update(t).Set(t.Id == 1).Where(t.Dep == "x");
            """);

    [Fact]
    public Task DeleteFrom_CteBodyReferencingTarget_StaysSilent() =>
        RunSilent("""
            var cte = new Cte("c");
            var q = With(cte.As(Select(t.Id).From(t))).DeleteFrom(t).Where(t.Id.In(Select(cte.Column("id")).From(cte)));
            """);

    [Fact]
    public Task InsertInto_SelectReadingTarget_StaysSilent() =>
        RunSilent("""
            var q = InsertInto(t, t.Id).Select(t.Id).From(t);
            """);

    [Fact]
    public Task DeleteFrom_NoTargetConfigured_StaysSilent() =>
        RunSilent("""
            var q = DeleteFrom(t).Where(Exists(Select(r.Id).From(r).Where(r.Id == t.Id)));
            """, dbms: null);

    [Fact]
    public Task DeleteFrom_LocalReassigned_StaysSilent() =>
        RunSilent("""
            T u = new T();
            u = new T("x");
            var q = DeleteFrom(u).Where(Exists(Select(r.Id).From(r).Where(r.Id == u.Id)));
            """);

    [Fact]
    public Task DeleteFrom_LocalReassignedInsideLambda_StaysSilent() =>
        RunSilent("""
            T u = new T();
            System.Action a = () => { u = new T("x"); };
            var q = DeleteFrom(u).Where(Exists(Select(r.Id).From(r).Where(r.Id == u.Id)));
            """);

    // Deconstruction reassigns the target to an aliased instance; the no-write
    // scan must descend into the tuple or it under-counts into a false positive.
    [Fact]
    public Task DeleteFrom_LocalReassignedViaTupleDeconstruction_StaysSilent() =>
        RunSilent("""
            T u = new T();
            int x;
            (u, x) = (new T("a"), 0);
            var q = DeleteFrom(u).Where(Exists(Select(r.Id).From(r).Where(r.Id == u.Id)));
            """);

    // A deconstruction of a different local must not silence a genuine report.
    [Fact]
    public Task DeleteFrom_UnrelatedTupleDeconstruction_ReportsSqla0005() =>
        RunReporting("""
            T other = new T();
            int x;
            (other, x) = (new T("a"), 0);
            var q = DeleteFrom(t).Where(Exists(Select(r.Id).From(r).Where(r.Id == {|#0:t.Id|})));
            """);

    // A ref alias lets a later write reach the target under another name; taking
    // the ref must count as a write.
    [Fact]
    public Task DeleteFrom_LocalReassignedThroughRefAlias_StaysSilent() =>
        RunSilent("""
            T u = new T();
            ref T v = ref u;
            v = new T("a");
            var q = DeleteFrom(u).Where(Exists(Select(r.Id).From(r).Where(r.Id == u.Id)));
            """);

    // A joined UPDATE/DELETE with an unaliased target throws its own guard (a
    // different message) before the correlated guard arms — reporting "correlated"
    // there would misdescribe it.
    [Fact]
    public Task Update_JoinedFromUnaliasedTarget_StaysSilent() =>
        RunSilent("""
            var q = Update(t).Set(t.Id == 1).From(r).Where(t.Id.In(Select(r.Id).From(r).Where(r.Dep == t.Dep)));
            """);

    [Fact]
    public Task DeleteFrom_JoinedUsingUnaliasedTarget_StaysSilent() =>
        RunSilent("""
            var q = DeleteFrom(t).Using(r).Where(t.Id.In(Select(r.Id).From(r).Where(r.Dep == t.Dep)));
            """);

    [Fact]
    public Task Update_JoinedInnerJoinUnaliasedTarget_StaysSilent() =>
        RunSilent("""
            var q = Update(t).InnerJoin(r).On(t.Id == r.Id).Set(t.Id == 1).Where(t.Id.In(Select(r.Id).From(r).Where(r.Dep == t.Dep)));
            """);

    [Fact]
    public Task Update_NonReadonlyFieldTarget_StaysSilent() =>
        RunAsync("""
            using SqlArtisan;
            using SqlArtisan.Internal;
            using static SqlArtisan.Sql;

            class T : DbTableBase
            {
                public DbColumn Id;
                public DbColumn Dep;
                public T(string alias = "") : base("t", alias) { Id = new DbColumn(this, "id"); Dep = new DbColumn(this, "dep"); }
            }

            class C
            {
                private T _t = new T();

                void M()
                {
                    T r = new T("r");
                    var q = Update(_t).Set(_t.Id == Select(Max(r.Id)).From(r).Where(r.Dep == _t.Dep));
                }
            }
            """, AnalyzerVerifier.EditorConfig("postgresql"), expectWarning: false);

    [Fact]
    public Task Update_ReadonlyFieldAssignedInConstructor_StaysSilent() =>
        RunAsync("""
            using SqlArtisan;
            using SqlArtisan.Internal;
            using static SqlArtisan.Sql;

            class T : DbTableBase
            {
                public DbColumn Id;
                public DbColumn Dep;
                public T(string alias = "") : base("t", alias) { Id = new DbColumn(this, "id"); Dep = new DbColumn(this, "dep"); }
            }

            class C
            {
                private readonly T _t = new T();

                public C()
                {
                    _t = new T("x");
                }

                void M()
                {
                    T r = new T("r");
                    var q = Update(_t).Set(_t.Id == Select(Max(r.Id)).From(r).Where(r.Dep == _t.Dep));
                }
            }
            """, AnalyzerVerifier.EditorConfig("postgresql"), expectWarning: false);

    // The case a zero-argument-constructor shortcut would false-positive on:
    // the parameterless ctor hardcodes a real alias.
    [Fact]
    public Task DeleteFrom_TargetCtorHardcodesAlias_StaysSilent() =>
        RunAsync("""
            using SqlArtisan;
            using SqlArtisan.Internal;
            using static SqlArtisan.Sql;

            class T : DbTableBase
            {
                public DbColumn Id;
                public T(string alias = "") : base("t", alias) { Id = new DbColumn(this, "id"); }
            }

            class A : DbTableBase
            {
                public DbColumn Id;
                public A() : base("a", "x") { Id = new DbColumn(this, "id"); }
            }

            class C
            {
                void M()
                {
                    A a = new A();
                    T r = new T("r");
                    var q = DeleteFrom(a).Where(Exists(Select(r.Id).From(r).Where(r.Id == a.Id)));
                }
            }
            """, AnalyzerVerifier.EditorConfig("postgresql"), expectWarning: false);

    [Fact]
    public Task DeleteFrom_TargetCtorAliasNotConstant_StaysSilent() =>
        RunAsync("""
            using SqlArtisan;
            using SqlArtisan.Internal;
            using static SqlArtisan.Sql;

            class T : DbTableBase
            {
                public DbColumn Id;
                public T(string alias = "") : base("t", alias) { Id = new DbColumn(this, "id"); }
            }

            class B : DbTableBase
            {
                public DbColumn Id;
                public B() : base("b", F()) { Id = new DbColumn(this, "id"); }
                static string F() => "";
            }

            class C
            {
                void M()
                {
                    B b = new B();
                    T r = new T("r");
                    var q = DeleteFrom(b).Where(Exists(Select(r.Id).From(r).Where(r.Id == b.Id)));
                }
            }
            """, AnalyzerVerifier.EditorConfig("postgresql"), expectWarning: false);

    [Fact]
    public Task Update_BuilderHeldInVariable_StaysSilent() =>
        RunSilent("""
            var b = Update(t);
            var q = b.Set(t.Id == Select(Max(r.Id)).From(r).Where(r.Dep == t.Dep));
            """);

    [Fact]
    public Task DeleteFrom_TargetFromParameter_StaysSilent() =>
        RunAsync("""
            using SqlArtisan;
            using SqlArtisan.Internal;
            using static SqlArtisan.Sql;

            class T : DbTableBase
            {
                public DbColumn Id;
                public T(string alias = "") : base("t", alias) { Id = new DbColumn(this, "id"); }
            }

            class C
            {
                void M(T p)
                {
                    T r = new T("r");
                    var q = DeleteFrom(p).Where(Exists(Select(r.Id).From(r).Where(r.Id == p.Id)));
                }
            }
            """, AnalyzerVerifier.EditorConfig("postgresql"), expectWarning: false);

    [Fact]
    public Task Update_FieldOnOtherInstanceInSubquery_StaysSilent() =>
        RunAsync("""
            using SqlArtisan;
            using SqlArtisan.Internal;
            using static SqlArtisan.Sql;

            class T : DbTableBase
            {
                public DbColumn Id;
                public DbColumn Dep;
                public T(string alias = "") : base("t", alias) { Id = new DbColumn(this, "id"); Dep = new DbColumn(this, "dep"); }
            }

            class C
            {
                private readonly T _t = new T();

                void M(C other)
                {
                    T r = new T("r");
                    var q = Update(_t).Set(_t.Id == Select(Max(r.Id)).From(r).Where(r.Dep == other._t.Dep));
                }
            }
            """, AnalyzerVerifier.EditorConfig("postgresql"), expectWarning: false);

    // Documented false negative: the subquery's chain head is With, not Select.
    [Fact]
    public Task DeleteFrom_WithHeadedSubquery_StaysSilent() =>
        RunSilent("""
            var cte = new Cte("c");
            var q = DeleteFrom(t).Where(t.Id.In(With(cte.As(Select(r.Id).From(r))).Select(cte.Column("id")).From(cte).Where(cte.Column("id") == t.Dep)));
            """);
}
