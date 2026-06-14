using System.Reflection;
using SqlArtisan.Internal;
using MySqlSql = SqlArtisan.Databases.MySql.Sql;
// The per-DBMS facades, aliased so they can sit side by side in one test.
using OracleSql = SqlArtisan.Databases.Oracle.Sql;
using PgSql = SqlArtisan.Databases.PostgreSql.Sql;
using SqlServerSql = SqlArtisan.Databases.SqlServer.Sql;

namespace SqlArtisan.Tests;

// Validates the ③ thin slice integrated into the real library (src/SqlArtisan):
// per-DBMS facade, DBMS folded into the namespace (no-arg Build), and the
// DBMS-affinity guard in the real SqlBuildingBuffer.
public class PerDbmsNamespaceTests
{
    [Fact]
    public void Oracle_Ceil_BuildsThroughRealPipeline()
    {
        SqlStatement sql = OracleSql.Select(OracleSql.Ceil(1)).Build();
        Assert.Equal("SELECT CEIL(:0)", sql.Text);
    }

    [Fact]
    public void SqlServer_Ceiling_BuildsThroughRealPipeline()
    {
        SqlStatement sql = SqlServerSql.Select(SqlServerSql.Ceiling(1)).Build();
        Assert.Equal("SELECT CEILING(@0)", sql.Text);
    }

    // IntelliSense filtering is real: the invalid function is not a member.
    // Reflection is the runnable proof; uncommenting the call fails to compile.
    [Fact]
    public void Facades_Expose_Only_Their_Dialects_Spelling()
    {
        Type oracle = typeof(SqlArtisan.Databases.Oracle.Sql);
        Type sqlServer = typeof(SqlArtisan.Databases.SqlServer.Sql);

        Assert.NotNull(oracle.GetMethod("Ceil", BindingFlags.Public | BindingFlags.Static));
        Assert.Null(oracle.GetMethod("Ceiling", BindingFlags.Public | BindingFlags.Static));

        Assert.NotNull(sqlServer.GetMethod("Ceiling", BindingFlags.Public | BindingFlags.Static));
        Assert.Null(sqlServer.GetMethod("Ceil", BindingFlags.Public | BindingFlags.Static));

        // OracleSql.Ceiling(1);  // would not compile: CS0117
    }

    // The DBMS-affinity guard catches a node authored for one DBMS being built
    // for another — the mixing hole that namespace filtering alone cannot close.
    [Fact]
    public void Mixing_OracleNode_Into_DefaultBuild_Throws()
    {
        // Default Build() targets PostgreSql; the node is tagged Oracle.
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => Sql.Select(OracleSql.Ceil(1)).Build());

        Assert.Contains("authored with SqlArtisan.Oracle", ex.Message);
        Assert.Contains("built for PostgreSql", ex.Message);
    }

    // The DBMS-neutral Sql.* API is unaffected: its nodes are portable (untagged)
    // and build on any target without tripping the guard.
    [Fact]
    public void NeutralApi_Nodes_Remain_Portable()
    {
        SqlStatement pg = Sql.Select(Sql.Abs(1)).Build();
        SqlStatement ora = Sql.Select(Sql.Abs(1)).Build(Dbms.Oracle);

        Assert.Equal("SELECT ABS(:0)", pg.Text);
        Assert.Equal("SELECT ABS(:0)", ora.Text);
    }

    // ── Clause-level experiment, 2nd shape (#89 MERGE, a full statement) ──────

    // MERGE is exposed by the Oracle/SqlServer namespaces (the inverse of UPSERT:
    // PG/MySql/SQLite have no MERGE). Oracle builds with no trailing semicolon.
    [Fact]
    public void Oracle_Namespace_Merge_BuildsWithoutSemicolon()
    {
        TestTable t = new("t");
        TestTable s = new("s");
        TestTable c = new();

        SqlStatement sql =
            OracleSql.MergeInto(t)
            .Using(s)
            .On(t.Code == s.Code)
            .WhenMatchedThenUpdateSet(t.Name == s.Name)
            .WhenNotMatchedThenInsert(c.Code, c.Name)
            .Values(s.Code, s.Name)
            .Build();

        Assert.EndsWith("VALUES (\"s\".code, \"s\".name)", sql.Text);
        Assert.StartsWith("MERGE INTO test_table \"t\" USING test_table \"s\"", sql.Text);
    }

    // The SqlServer namespace folds in the mandatory trailing semicolon.
    [Fact]
    public void SqlServer_Namespace_Merge_BuildsWithSemicolon()
    {
        TestTable t = new("t");
        TestTable s = new("s");
        TestTable c = new();

        SqlStatement sql =
            SqlServerSql.MergeInto(t)
            .Using(s)
            .On(t.Code == s.Code)
            .WhenMatchedThenUpdateSet(t.Name == s.Name)
            .WhenNotMatchedThenInsert(c.Code, c.Name)
            .Values(s.Code, s.Name)
            .Build();

        Assert.EndsWith(";", sql.Text);
    }

    // MERGE is present only in the Oracle/SqlServer namespaces; UPSERT verbs are
    // present only in PG/MySql. The five DBMS are cleanly partitioned by upsert
    // mechanism, with no overlap — the symmetry the namespace approach buys.
    [Fact]
    public void Namespaces_Partition_Upsert_And_Merge_By_Dialect()
    {
        Assert.NotNull(typeof(OracleSql).GetMethod("MergeInto"));
        Assert.NotNull(typeof(SqlServerSql).GetMethod("MergeInto"));
        Assert.Null(typeof(PgSql).GetMethod("MergeInto"));
        Assert.Null(typeof(MySqlSql).GetMethod("MergeInto"));

        // Inversely, the UPSERT entry points live only on PG/MySql:
        Assert.Null(typeof(OracleSql).GetMethod("InsertInto"));
        Assert.NotNull(typeof(PgSql).GetMethod("InsertInto"));
    }

    // ── Clause-level experiment, 3rd shape (#88 string aggregation) ───────────
    // A divergent *function* (SELECT-list expression), not a fluent statement.
    // Each namespace exposes its own spelling and emits its own structure.

    [Fact]
    public void PostgreSql_Namespace_StringAgg_InlineOrderBy()
    {
        TestTable t = new();
        SqlStatement sql = PgSql.Select(PgSql.StringAgg(t.Name, ", ", t.Code)).Build();
        Assert.Equal("SELECT STRING_AGG(name, :0 ORDER BY code)", sql.Text);
    }

    [Fact]
    public void SqlServer_Namespace_StringAgg_WithinGroup()
    {
        TestTable t = new();
        SqlStatement sql = SqlServerSql.Select(SqlServerSql.StringAgg(t.Name, ", ", t.Code)).Build();
        Assert.Equal("SELECT STRING_AGG(name, @0) WITHIN GROUP (ORDER BY code)", sql.Text);
    }

    [Fact]
    public void Oracle_Namespace_Listagg_WithinGroup()
    {
        TestTable t = new();
        SqlStatement sql = OracleSql.Select(OracleSql.Listagg(t.Name, ", ", t.Code)).Build();
        Assert.Equal("SELECT LISTAGG(name, :0) WITHIN GROUP (ORDER BY code)", sql.Text);
    }

    [Fact]
    public void MySql_Namespace_GroupConcat_InlineSeparatorLiteral()
    {
        TestTable t = new();
        SqlStatement sql = MySqlSql.Select(MySqlSql.GroupConcat(t.Name, t.Code, ", ")).Build();
        Assert.Equal("SELECT GROUP_CONCAT(name ORDER BY code SEPARATOR ', ')", sql.Text);
    }

    // Each namespace exposes only its dialect's spelling — and, unlike MERGE/UPSERT,
    // there are NO wrapper types: the per-DBMS cost is one factory method (depth 0).
    [Fact]
    public void Namespaces_Expose_Only_Their_StringAggSpelling()
    {
        Assert.NotNull(typeof(PgSql).GetMethod("StringAgg"));
        Assert.Null(typeof(PgSql).GetMethod("Listagg"));
        Assert.Null(typeof(PgSql).GetMethod("GroupConcat"));

        Assert.NotNull(typeof(OracleSql).GetMethod("Listagg"));
        Assert.Null(typeof(OracleSql).GetMethod("StringAgg"));

        Assert.NotNull(typeof(MySqlSql).GetMethod("GroupConcat"));
        Assert.Null(typeof(MySqlSql).GetMethod("StringAgg"));
    }
}
