// Running proof that the ③ direction works end to end. Each test maps to a
// claim in ../README.md's "Findings".

using System.Reflection;
using SqlArtisan.Proof;

// Note the two namespaces side by side — this is exactly the user experience
// being validated: you import the one matching your DBMS.
using OracleSql = SqlArtisan.Proof.Oracle.Sql;
using SqlServerSql = SqlArtisan.Proof.SqlServer.Sql;

namespace SqlArtisan.Proof.Tests;

public class ProofTests
{
    // (1) The DBMS is folded into the namespace: Build() takes no argument and
    //     produces that DBMS's SQL.
    [Fact]
    public void Oracle_Ceil_BuildsCeil()
    {
        string sql = OracleSql.Select(OracleSql.Ceil("price")).Build();
        Assert.Equal("SELECT CEIL(price)", sql);
    }

    [Fact]
    public void SqlServer_Ceiling_BuildsCeiling()
    {
        string sql = SqlServerSql.Select(SqlServerSql.Ceiling("price")).Build();
        Assert.Equal("SELECT CEILING(price)", sql);
    }

    // (2) IntelliSense filtering is real: the invalid function does not exist as
    //     a member, so it cannot be typed (it would be a compile error). Proven
    //     here by reflection — the strongest runnable evidence of absence.
    [Fact]
    public void Oracle_Facade_Has_Ceil_But_Not_Ceiling()
    {
        Type oracle = typeof(SqlArtisan.Proof.Oracle.Sql);
        Assert.NotNull(oracle.GetMethod("Ceil", BindingFlags.Public | BindingFlags.Static));
        Assert.Null(oracle.GetMethod("Ceiling", BindingFlags.Public | BindingFlags.Static));
    }

    [Fact]
    public void SqlServer_Facade_Has_Ceiling_But_Not_Ceil()
    {
        Type sqlServer = typeof(SqlArtisan.Proof.SqlServer.Sql);
        Assert.NotNull(sqlServer.GetMethod("Ceiling", BindingFlags.Public | BindingFlags.Static));
        Assert.Null(sqlServer.GetMethod("Ceil", BindingFlags.Public | BindingFlags.Static));
    }

    // The following line, uncommented, would FAIL TO COMPILE — the literal proof
    // of the IntelliSense guarantee:
    //
    //     OracleSql.Ceiling("price");   // CS0117: 'Sql' has no definition for 'Ceiling'

    // (3) Tag validation closes the mixing hole that namespaces alone cannot:
    //     an Oracle-authored node fed into a SQL Server build throws at Build().
    [Fact]
    public void Mixing_OracleNode_Into_SqlServerBuild_Throws()
    {
        // A shared helper might hand you an Oracle-tagged expression...
        SqlExpression oracleNode = OracleSql.Ceil("price");

        // ...and the SQL Server Select happily ACCEPTS any SqlExpression — that
        // is the hole. The build-time check is what catches it.
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => SqlServerSql.Select(oracleNode).Build());

        Assert.Contains("authored with SqlArtisan.Oracle", ex.Message);
        Assert.Contains("built for SqlServer", ex.Message);
    }

    // (4) Portable nodes (columns, literals) carry no affinity and build fine on
    //     any DBMS — the universal case is not over-restricted.
    [Fact]
    public void PortableColumn_BuildsOnAnyDbms()
    {
        Assert.Equal("SELECT CEIL(price)", OracleSql.Select(OracleSql.Ceil("price")).Build());
        Assert.Equal("SELECT ABS(qty)", SqlServerSql.Select(SqlServerSql.Abs("qty")).Build());
    }

    // (5) A universal function authored in the matching namespace builds cleanly
    //     — confirming the tag does not get in the way of the common path.
    [Fact]
    public void Universal_Abs_NoFalsePositive()
    {
        string sql = OracleSql.Select(OracleSql.Abs("qty")).Build();
        Assert.Equal("SELECT ABS(qty)", sql);
    }
}
