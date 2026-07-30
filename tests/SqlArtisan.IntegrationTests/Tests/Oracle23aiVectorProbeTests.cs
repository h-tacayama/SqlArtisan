using System.Data;
using Dapper;
using SqlArtisan.IntegrationTests.Infrastructure;

namespace SqlArtisan.IntegrationTests.Tests;

// TEMPORARY raw-SQL probe (#343): the 23ai bound sweep rejected the three
// shared distance operators with ORA-22849, which does not say whether the
// CAST(:bind AS vector) operand or the operator itself fell. Each fact below
// isolates one spelling; delete this file once the verdict is recorded.
[Trait("Engine", "Oracle23ai")]
public sealed class Oracle23aiVectorProbeTests : IClassFixture<Oracle23aiFixture>
{
    private readonly Oracle23aiFixture _fixture;

    public Oracle23aiVectorProbeTests(Oracle23aiFixture fixture) => _fixture = fixture;

    [Fact]
    public void Probe_CastLiteralToVector()
    {
        Assert.Null(TryScalar("SELECT CAST('[1,2]' AS vector) FROM dual"));
    }

    [Fact]
    public void Probe_CastBindToVector()
    {
        Assert.Null(TryScalar("SELECT CAST(:0 AS vector) FROM dual", "[1,2]"));
    }

    [Fact]
    public void Probe_ToVectorBind_Distance()
    {
        Assert.Null(TryScalar("SELECT (TO_VECTOR(:0) <-> TO_VECTOR(:1)) FROM dual", "[1,2]", "[3,4]"));
    }

    [Fact]
    public void Probe_ToVectorLiteral_Distance()
    {
        Assert.Null(TryScalar("SELECT (TO_VECTOR('[1,2]') <-> TO_VECTOR('[3,4]')) FROM dual"));
    }

    [Fact]
    public void Probe_CastLiteral_Distance()
    {
        Assert.Null(TryScalar("SELECT (CAST('[1,2]' AS vector) <-> CAST('[3,4]' AS vector)) FROM dual"));
    }

    [Fact]
    public void Probe_ImplicitString_Distance()
    {
        Assert.Null(TryScalar("SELECT ('[1,2]' <-> '[3,4]') FROM dual"));
    }

    [Fact]
    public void Probe_CosineAndInner_ToVectorLiteral()
    {
        Assert.Null(TryScalar("SELECT (TO_VECTOR('[1,2]') <=> TO_VECTOR('[3,4]')) FROM dual"));
        Assert.Null(TryScalar("SELECT (TO_VECTOR('[1,2]') <#> TO_VECTOR('[3,4]')) FROM dual"));
    }

    private string? TryScalar(string sql, params string[] binds)
    {
        using IDbConnection connection = _fixture.OpenConnection();
        DynamicParameters parameters = new();
        for (int i = 0; i < binds.Length; i++)
        {
            parameters.Add(i.ToString(), binds[i]);
        }

        try
        {
            connection.ExecuteScalar(sql, parameters);
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }
}
