using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;

namespace SqlArtisan.Analyzers.Tests;

public class IdentifierLengthAnalyzerTests
{
    private static string EditorConfig(string dbms) => $"""
        root = true

        [*.cs]
        sqlartisan_target_dbms = {dbms}
        """;

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

    private static string Unmarked(string source) =>
        source.Replace("{|#0:", string.Empty).Replace("|}", string.Empty);

    private static string Repeat(char c, int count) => new(c, count);

    [Fact]
    public async Task AliasOverPostgreSqlByteLimit_ReportsSqla0003()
    {
        var test = AnalyzerVerifier.Create(AliasUsage(Repeat('a', 64)), EditorConfig("postgresql"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0003").WithLocation(0));
        await test.RunAsync();
    }

    [Fact]
    public async Task AliasAtPostgreSqlByteLimit_StaysSilent()
    {
        var test = AnalyzerVerifier.Create(Unmarked(AliasUsage(Repeat('a', 63))), EditorConfig("postgresql"));
        await test.RunAsync();
    }

    [Fact]
    public async Task MultiByteAliasOverPostgreSqlByteLimit_ReportsSqla0003()
    {
        // 22 three-byte characters = 66 bytes (over 63) while only 22 characters — proves
        // the limit is measured in UTF-8 bytes, not characters.
        var test = AnalyzerVerifier.Create(AliasUsage(Repeat('あ', 22)), EditorConfig("postgresql"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0003").WithLocation(0));
        await test.RunAsync();
    }

    [Fact]
    public async Task MultiByteAliasAtPostgreSqlByteLimit_StaysSilent()
    {
        // 21 three-byte characters = exactly 63 bytes.
        var test = AnalyzerVerifier.Create(Unmarked(AliasUsage(Repeat('あ', 21))), EditorConfig("postgresql"));
        await test.RunAsync();
    }

    [Fact]
    public async Task AliasOverMySqlCharLimit_ReportsSqla0003()
    {
        var test = AnalyzerVerifier.Create(AliasUsage(Repeat('a', 65)), EditorConfig("mysql"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0003").WithLocation(0));
        await test.RunAsync();
    }

    [Fact]
    public async Task AliasAtMySqlCharLimit_StaysSilent()
    {
        var test = AnalyzerVerifier.Create(Unmarked(AliasUsage(Repeat('a', 64))), EditorConfig("mysql"));
        await test.RunAsync();
    }

    [Fact]
    public async Task AliasOverSqlServerCharLimit_ReportsSqla0003()
    {
        var test = AnalyzerVerifier.Create(AliasUsage(Repeat('a', 129)), EditorConfig("sqlserver"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0003").WithLocation(0));
        await test.RunAsync();
    }

    [Fact]
    public async Task AliasOverOracleByteLimit_ReportsSqla0003()
    {
        var test = AnalyzerVerifier.Create(AliasUsage(Repeat('a', 129)), EditorConfig("oracle"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0003").WithLocation(0));
        await test.RunAsync();
    }

    [Fact]
    public async Task AliasOnSqlite_StaysSilent()
    {
        // SQLite imposes no identifier-length limit, so the check never fires there.
        var test = AnalyzerVerifier.Create(Unmarked(AliasUsage(Repeat('a', 200))), EditorConfig("sqlite"));
        await test.RunAsync();
    }

    [Fact]
    public async Task NoTargetConfigured_StaysSilent()
    {
        var test = AnalyzerVerifier.Create(Unmarked(AliasUsage(Repeat('a', 200))));
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
        var test = AnalyzerVerifier.Create(source, EditorConfig("postgresql"));
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
        var test = AnalyzerVerifier.Create(source, EditorConfig("postgresql"));
        await test.RunAsync();
    }

    [Fact]
    public async Task CteNameOverLimit_ReportsSqla0003()
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
        var test = AnalyzerVerifier.Create(source, EditorConfig("postgresql"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0003").WithLocation(0));
        await test.RunAsync();
    }

    [Fact]
    public async Task DerivedTableNameOverLimit_ReportsSqla0003()
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
        var test = AnalyzerVerifier.Create(source, EditorConfig("postgresql"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0003").WithLocation(0));
        await test.RunAsync();
    }

    [Fact]
    public async Task DbTableAliasOverLimit_ReportsSqla0003()
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
        var test = AnalyzerVerifier.Create(source, EditorConfig("postgresql"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0003").WithLocation(0));
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
        var test = AnalyzerVerifier.Create(source, EditorConfig("postgresql"));
        await test.RunAsync();
    }

    [Fact]
    public async Task OutputParameterVariableOverLimit_ReportsSqla0003()
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
        var test = AnalyzerVerifier.Create(source, EditorConfig("oracle"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0003").WithLocation(0));
        await test.RunAsync();
    }

    [Fact]
    public async Task ValuesTableAliasOverLimit_ReportsSqla0003()
    {
        string source = $$"""
            using SqlArtisan;
            using static SqlArtisan.Sql;

            class C
            {
                void M()
                {
                    var v = ValuesTable({|#0:"{{Repeat('a', 64)}}"|}, ["c"], new object[][] { new object[] { 1 } });
                }
            }
            """;
        var test = AnalyzerVerifier.Create(source, EditorConfig("postgresql"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0003").WithLocation(0));
        await test.RunAsync();
    }

    [Fact]
    public async Task ValuesTableColumnNameOverLimit_ReportsSqla0003PerElement()
    {
        // Only the over-limit column of the list warns, at its own location.
        string source = $$"""
            using SqlArtisan;
            using static SqlArtisan.Sql;

            class C
            {
                void M()
                {
                    var v = ValuesTable("s", ["ok", {|#0:"{{Repeat('a', 64)}}"|}], new object[][] { new object[] { 1, 2 } });
                }
            }
            """;
        var test = AnalyzerVerifier.Create(source, EditorConfig("postgresql"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0003").WithLocation(0));
        await test.RunAsync();
    }

    [Fact]
    public async Task TypedCteBaseNameOverLimit_ReportsSqla0003()
    {
        // The name reaches the base constructor through a subclass initializer.
        string source = $$"""
            using SqlArtisan;

            class LongCte : CteBase
            {
                public LongCte() : base({|#0:"{{Repeat('a', 64)}}"|}) { }
            }
            """;
        var test = AnalyzerVerifier.Create(source, EditorConfig("postgresql"));
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("SQLA0003").WithLocation(0));
        await test.RunAsync();
    }
}
