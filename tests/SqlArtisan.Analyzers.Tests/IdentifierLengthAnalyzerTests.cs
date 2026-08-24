using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;

namespace SqlArtisan.Analyzers.Tests;

public class IdentifierLengthAnalyzerTests
{
    // The alias literal is wrapped in the #0 marker so the expected warning location is
    // the literal itself; silence cases strip the marker before running.
    private static string AliasUsage(string alias) => $$"""
        using SqlArtisan;
        using static SqlArtisan.Sql;

        class C
        {
            void M()
            {
                var x = Bind(1).As({|#0:"{{alias}}"|});
            }
        }
        """;

    private static string Repeat(char c, int count) => new(c, count);

    [Fact]
    public async Task AliasOverPostgreSqlByteLimit_ReportsSqla0103()
    {
        var test = AnalyzerVerifier.Create(AliasUsage(Repeat('a', 64)), AnalyzerVerifier.EditorConfig("postgresql"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0103").WithLocation(0));
        await test.RunAsync();
    }

    [Fact]
    public async Task AliasAtPostgreSqlByteLimit_StaysSilent()
    {
        var test = AnalyzerVerifier.Create(AnalyzerVerifier.Unmarked(AliasUsage(Repeat('a', 63))), AnalyzerVerifier.EditorConfig("postgresql"));
        await test.RunAsync();
    }

    [Fact]
    public async Task MultiByteAliasOverPostgreSqlByteLimit_ReportsSqla0103()
    {
        // 22 three-byte characters = 66 bytes (over 63) while only 22 characters — proves
        // the limit is measured in UTF-8 bytes, not characters.
        var test = AnalyzerVerifier.Create(AliasUsage(Repeat('あ', 22)), AnalyzerVerifier.EditorConfig("postgresql"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0103").WithLocation(0));
        await test.RunAsync();
    }

    [Fact]
    public async Task MultiByteAliasAtPostgreSqlByteLimit_StaysSilent()
    {
        // 21 three-byte characters = exactly 63 bytes.
        var test = AnalyzerVerifier.Create(AnalyzerVerifier.Unmarked(AliasUsage(Repeat('あ', 21))), AnalyzerVerifier.EditorConfig("postgresql"));
        await test.RunAsync();
    }

    [Fact]
    public async Task AliasOverMySqlCharLimit_ReportsSqla0103()
    {
        // MySQL's alias limit is 256 characters (its 64-char limit is for table/column
        // names, not aliases), so an alias only warns past 256.
        var test = AnalyzerVerifier.Create(AliasUsage(Repeat('a', 257)), AnalyzerVerifier.EditorConfig("mysql"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0103").WithLocation(0));
        await test.RunAsync();
    }

    [Fact]
    public async Task AliasAtMySqlCharLimit_StaysSilent()
    {
        var test = AnalyzerVerifier.Create(AnalyzerVerifier.Unmarked(AliasUsage(Repeat('a', 256))), AnalyzerVerifier.EditorConfig("mysql"));
        await test.RunAsync();
    }

    [Fact]
    public async Task AliasOverSqlServerCharLimit_ReportsSqla0103()
    {
        var test = AnalyzerVerifier.Create(AliasUsage(Repeat('a', 129)), AnalyzerVerifier.EditorConfig("sqlserver"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0103").WithLocation(0));
        await test.RunAsync();
    }

    [Fact]
    public async Task AliasAtSqlServerCharLimit_StaysSilent()
    {
        var test = AnalyzerVerifier.Create(AnalyzerVerifier.Unmarked(AliasUsage(Repeat('a', 128))), AnalyzerVerifier.EditorConfig("sqlserver"));
        await test.RunAsync();
    }

    [Fact]
    public async Task AliasOverOracleByteLimit_ReportsSqla0103()
    {
        var test = AnalyzerVerifier.Create(AliasUsage(Repeat('a', 129)), AnalyzerVerifier.EditorConfig("oracle"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0103").WithLocation(0));
        await test.RunAsync();
    }

    [Fact]
    public async Task AliasAtOracleByteLimit_StaysSilent()
    {
        var test = AnalyzerVerifier.Create(AnalyzerVerifier.Unmarked(AliasUsage(Repeat('a', 128))), AnalyzerVerifier.EditorConfig("oracle"));
        await test.RunAsync();
    }

    [Fact]
    public async Task AliasOnSqlite_StaysSilent()
    {
        // SQLite imposes no identifier-length limit, so the check never fires there.
        var test = AnalyzerVerifier.Create(AnalyzerVerifier.Unmarked(AliasUsage(Repeat('a', 200))), AnalyzerVerifier.EditorConfig("sqlite"));
        await test.RunAsync();
    }

    [Fact]
    public async Task NoTargetConfigured_StaysSilent()
    {
        var test = AnalyzerVerifier.Create(AnalyzerVerifier.Unmarked(AliasUsage(Repeat('a', 200))));
        await test.RunAsync();
    }

    [Fact]
    public async Task NonConstantAlias_StaysSilent()
    {
        const string source = """
            using SqlArtisan;
            using static SqlArtisan.Sql;

            class C
            {
                void M(string alias)
                {
                    var x = Bind(1).As(alias);
                }
            }
            """;
        var test = AnalyzerVerifier.Create(source, AnalyzerVerifier.EditorConfig("postgresql"));
        await test.RunAsync();
    }

    [Fact]
    public async Task AsColumnOverload_StaysSilent()
    {
        // As(DbColumn) has no alias literal, and a table's real column name is existing-schema
        // (out of scope) — neither is checked even when the name is over the limit.
        string source = $$"""
            using SqlArtisan;
            using static SqlArtisan.Sql;

            class C
            {
                void M()
                {
                    var t = new DbTable("orders");
                    var x = Bind(1).As(t.Column("{{Repeat('a', 64)}}"));
                }
            }
            """;
        var test = AnalyzerVerifier.Create(source, AnalyzerVerifier.EditorConfig("postgresql"));
        await test.RunAsync();
    }

    [Fact]
    public async Task CteNameOverLimit_ReportsSqla0103()
    {
        string source = $$"""
            using SqlArtisan;
            using static SqlArtisan.Sql;

            class C
            {
                void M()
                {
                    var c = new Cte({|#0:"{{Repeat('a', 64)}}"|});
                }
            }
            """;
        var test = AnalyzerVerifier.Create(source, AnalyzerVerifier.EditorConfig("postgresql"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0103").WithLocation(0));
        await test.RunAsync();
    }

    [Fact]
    public async Task DerivedTableNameOverLimit_ReportsSqla0103()
    {
        string source = $$"""
            using SqlArtisan;
            using static SqlArtisan.Sql;

            class C
            {
                void M()
                {
                    var d = new DerivedTable({|#0:"{{Repeat('a', 64)}}"|});
                }
            }
            """;
        var test = AnalyzerVerifier.Create(source, AnalyzerVerifier.EditorConfig("postgresql"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0103").WithLocation(0));
        await test.RunAsync();
    }

    [Fact]
    public async Task DbTableAliasOverLimit_ReportsSqla0103()
    {
        string source = $$"""
            using SqlArtisan;
            using static SqlArtisan.Sql;

            class C
            {
                void M()
                {
                    var t = new DbTable("orders", {|#0:"{{Repeat('a', 64)}}"|});
                }
            }
            """;
        var test = AnalyzerVerifier.Create(source, AnalyzerVerifier.EditorConfig("postgresql"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0103").WithLocation(0));
        await test.RunAsync();
    }

    [Fact]
    public async Task DbTableName_IsExistingSchema_StaysSilent()
    {
        // Only the minted alias is checked; the real table name is out of scope.
        string source = $$"""
            using SqlArtisan;
            using static SqlArtisan.Sql;

            class C
            {
                void M()
                {
                    var t = new DbTable("{{Repeat('a', 64)}}", "o");
                }
            }
            """;
        var test = AnalyzerVerifier.Create(source, AnalyzerVerifier.EditorConfig("postgresql"));
        await test.RunAsync();
    }

    [Fact]
    public async Task OutputParameterVariableOverLimit_ReportsSqla0103()
    {
        string source = $$"""
            using System.Data;
            using SqlArtisan;

            class C
            {
                void M()
                {
                    var p = new OutputParameter({|#0:"{{Repeat('a', 129)}}"|}, DbType.Int32);
                }
            }
            """;
        var test = AnalyzerVerifier.Create(source, AnalyzerVerifier.EditorConfig("oracle"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0103").WithLocation(0));
        await test.RunAsync();
    }

    [Fact]
    public async Task ValuesAliasOverLimit_ReportsSqla0103()
    {
        string source = $$"""
            using SqlArtisan;
            using static SqlArtisan.Sql;

            class C
            {
                void M()
                {
                    var v = Values({|#0:"{{Repeat('a', 64)}}"|}, ["c"], new object[][] { new object[] { 1 } });
                }
            }
            """;
        var test = AnalyzerVerifier.Create(source, AnalyzerVerifier.EditorConfig("postgresql"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0103").WithLocation(0));
        await test.RunAsync();
    }

    [Fact]
    public async Task ValuesColumnNameOverLimit_ReportsSqla0103PerElement()
    {
        // Only the over-limit column of the list warns, at its own location.
        string source = $$"""
            using SqlArtisan;
            using static SqlArtisan.Sql;

            class C
            {
                void M()
                {
                    var v = Values("s", ["ok", {|#0:"{{Repeat('a', 64)}}"|}], new object[][] { new object[] { 1, 2 } });
                }
            }
            """;
        var test = AnalyzerVerifier.Create(source, AnalyzerVerifier.EditorConfig("postgresql"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0103").WithLocation(0));
        await test.RunAsync();
    }

    [Fact]
    public async Task SubqueryAsTableAliasOverLimit_ReportsSqla0103()
    {
        string source = $$"""
            using SqlArtisan;
            using static SqlArtisan.Sql;

            class C
            {
                void M()
                {
                    var t = new DbTable("orders", "o");
                    var d = Select(Bind(1)).From(t).AsTable({|#0:"{{Repeat('a', 64)}}"|});
                }
            }
            """;
        var test = AnalyzerVerifier.Create(source, AnalyzerVerifier.EditorConfig("postgresql"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0103").WithLocation(0));
        await test.RunAsync();
    }

    [Fact]
    public async Task UnnestAsTableColumnNameOverLimit_ReportsSqla0103PerElement()
    {
        // Only the over-limit column of the list warns, at its own location.
        string source = $$"""
            using SqlArtisan;
            using static SqlArtisan.Sql;

            class C
            {
                void M()
                {
                    var u = Unnest(Array(1, 2)).AsTable("u", "ok", {|#0:"{{Repeat('a', 64)}}"|});
                }
            }
            """;
        var test = AnalyzerVerifier.Create(source, AnalyzerVerifier.EditorConfig("postgresql"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0103").WithLocation(0));
        await test.RunAsync();
    }

    [Fact]
    public async Task InsertValuesInstanceMethod_LongLiteral_StaysSilent()
    {
        // IInsertBuilderTable.Values(params object[]) shares the "Values" dictionary
        // key with Sql.Values(alias, columnNames, rows) (same bare method name, no
        // arity distinction in IdentifierLengthRule) — this pins down that the shared
        // key doesn't misfire, since this overload's "values" parameter never matches
        // the checked "alias"/"columnNames" names.
        string source = $$"""
            using SqlArtisan;
            using static SqlArtisan.Sql;

            class C
            {
                void M()
                {
                    var t = new DbTable("orders", "o");
                    var x = InsertInto(t).Values("{{Repeat('a', 64)}}");
                }
            }
            """;
        var test = AnalyzerVerifier.Create(source, AnalyzerVerifier.EditorConfig("postgresql"));
        await test.RunAsync();
    }

    [Fact]
    public async Task TypedCteBaseNameOverLimit_ReportsSqla0103()
    {
        // The name reaches the base constructor through a subclass initializer.
        string source = $$"""
            using SqlArtisan;

            class LongCte : CteBase
            {
                public LongCte() : base({|#0:"{{Repeat('a', 64)}}"|}) { }
            }
            """;
        var test = AnalyzerVerifier.Create(source, AnalyzerVerifier.EditorConfig("postgresql"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0103").WithLocation(0));
        await test.RunAsync();
    }

    // A generated/hand-written table class forwards its constructor argument to
    // DbTableBase's alias — the primary aliasing path; the rule traces the
    // ": base(...)" chain to check it (round-5 audit).
    private static string TableClassUsage(string alias) => $$"""
        using SqlArtisan;

        class UsersTable : DbTableBase
        {
            public UsersTable(string alias = "") : base("users", alias) { }
        }

        class C
        {
            void M()
            {
                var t = new UsersTable({|#0:"{{alias}}"|});
            }
        }
        """;

    [Fact]
    public async Task TableClassCtorAliasOverPostgreSqlByteLimit_ReportsSqla0103()
    {
        var test = AnalyzerVerifier.Create(TableClassUsage(Repeat('a', 64)), AnalyzerVerifier.EditorConfig("postgresql"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0103").WithLocation(0));
        await test.RunAsync();
    }

    [Fact]
    public async Task TableClassCtorAliasAtPostgreSqlByteLimit_StaysSilent()
    {
        var test = AnalyzerVerifier.Create(
            AnalyzerVerifier.Unmarked(TableClassUsage(Repeat('a', 63))), AnalyzerVerifier.EditorConfig("postgresql"));
        await test.RunAsync();
    }

    [Fact]
    public async Task UserClassNamedDbTable_ArgumentNotForwarded_StaysSilent()
    {
        // The class's simple name collides with a ConstructorIdentifierParams key,
        // but the coincidentally-named parameter never reaches the base alias —
        // only the SqlArtisan assembly's own members may match by name.
        string source = $$"""
            using SqlArtisan;

            namespace My
            {
                class DbTable : DbTableBase
                {
                    public string Comment;

                    public DbTable(string tableAlias) : base("db_table", "d")
                    {
                        Comment = tableAlias;
                    }
                }
            }

            class C
            {
                void M()
                {
                    var t = new My.DbTable("{{Repeat('a', 64)}}");
                }
            }
            """;
        var test = AnalyzerVerifier.Create(source, AnalyzerVerifier.EditorConfig("postgresql"));
        await test.RunAsync();
    }

    [Fact]
    public async Task UserClassNamedCte_ForwardedAlias_ReportsSqla0103()
    {
        // Same name collision, forwarding direction: the key's parameter name ("name")
        // doesn't match this ctor's, so a name-table hit would go silent — the
        // base-chain trace must still see the forwarded alias.
        string source = $$"""
            using SqlArtisan;

            namespace My
            {
                class Cte : DbTableBase
                {
                    public Cte(string alias = "") : base("cte", alias) { }
                }
            }

            class C
            {
                void M()
                {
                    var c = new My.Cte({|#0:"{{Repeat('a', 64)}}"|});
                }
            }
            """;
        var test = AnalyzerVerifier.Create(source, AnalyzerVerifier.EditorConfig("postgresql"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0103").WithLocation(0));
        await test.RunAsync();
    }

    [Fact]
    public async Task TableClassCtorDefaultedAlias_StaysSilent()
    {
        var test = AnalyzerVerifier.Create("""
            using SqlArtisan;

            class UsersTable : DbTableBase
            {
                public UsersTable(string alias = "") : base("users", alias) { }
            }

            class C
            {
                void M()
                {
                    var t = new UsersTable();
                }
            }
            """, AnalyzerVerifier.EditorConfig("postgresql"));
        await test.RunAsync();
    }
}
