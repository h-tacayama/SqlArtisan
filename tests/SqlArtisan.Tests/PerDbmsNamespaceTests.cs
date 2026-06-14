using System.Reflection;
using SqlArtisan.Internal;

// The two per-DBMS facades, aliased so they can sit side by side in one test.
using OracleSql = SqlArtisan.Databases.Oracle.Sql;
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
}
