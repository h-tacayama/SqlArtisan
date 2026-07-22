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
    public async Task AliasOverPostgreSqlByteLimit_ReportsSqla0006()
    {
        var test = AnalyzerVerifier.Create(AliasUsage(Repeat('a', 64)), AnalyzerVerifier.EditorConfig("postgresql"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0006").WithLocation(0));
        await test.RunAsync();
    }

    [Fact]
    public async Task AliasAtPostgreSqlByteLimit_StaysSilent()
    {
        var test = AnalyzerVerifier.Create(AnalyzerVerifier.Unmarked(AliasUsage(Repeat('a', 63))), AnalyzerVerifier.EditorConfig("postgresql"));
        await test.RunAsync();
    }

    [Fact]
    public async Task MultiByteAliasOverPostgreSqlByteLimit_ReportsSqla0006()
    {
        // 22 three-byte characters = 66 bytes (over 63) while only 22 characters — proves
        // the limit is measured in UTF-8 bytes, not characters.
        var test = AnalyzerVerifier.Create(AliasUsage(Repeat('あ', 22)), AnalyzerVerifier.EditorConfig("postgresql"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0006").WithLocation(0));
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
    public async Task AliasOverMySqlCharLimit_ReportsSqla0006()
    {
        // MySQL's alias limit is 256 characters (its 64-char limit is for table/column
        // names, not aliases), so an alias only warns past 256.
        var test = AnalyzerVerifier.Create(AliasUsage(Repeat('a', 257)), AnalyzerVerifier.EditorConfig("mysql"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0006").WithLocation(0));
        await test.RunAsync();
    }

    [Fact]
    public async Task AliasAtMySqlCharLimit_StaysSilent()
    {
        var test = AnalyzerVerifier.Create(AnalyzerVerifier.Unmarked(AliasUsage(Repeat('a', 256))), AnalyzerVerifier.EditorConfig("mysql"));
        await test.RunAsync();
    }

    [Fact]
    public async Task AliasOverSqlServerCharLimit_ReportsSqla0006()
    {
        var test = AnalyzerVerifier.Create(AliasUsage(Repeat('a', 129)), AnalyzerVerifier.EditorConfig("sqlserver"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0006").WithLocation(0));
        await test.RunAsync();
    }

    [Fact]
    public async Task AliasOverOracleByteLimit_ReportsSqla0006()
    {
        var test = AnalyzerVerifier.Create(AliasUsage(Repeat('a', 129)), AnalyzerVerifier.EditorConfig("oracle"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0006").WithLocation(0));
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
    public async Task CteNameOverLimit_ReportsSqla0006()
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
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0006").WithLocation(0));
        await test.RunAsync();
    }

    [Fact]
    public async Task DerivedTableNameOverLimit_ReportsSqla0006()
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
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0006").WithLocation(0));
        await test.RunAsync();
    }

    [Fact]
    public async Task DbTableAliasOverLimit_ReportsSqla0006()
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
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0006").WithLocation(0));
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
    public async Task OutputParameterVariableOverLimit_ReportsSqla0006()
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
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0006").WithLocation(0));
        await test.RunAsync();
    }

    [Fact]
    public async Task ValuesAliasOverLimit_ReportsSqla0006()
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
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0006").WithLocation(0));
        await test.RunAsync();
    }

    [Fact]
    public async Task ValuesColumnNameOverLimit_ReportsSqla0006PerElement()
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
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0006").WithLocation(0));
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
    public async Task TypedCteBaseNameOverLimit_ReportsSqla0006()
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
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0006").WithLocation(0));
        await test.RunAsync();
    }
}
