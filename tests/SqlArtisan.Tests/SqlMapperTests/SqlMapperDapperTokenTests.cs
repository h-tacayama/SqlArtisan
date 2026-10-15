using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using SqlArtisan.Dapper;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

/// <summary>
/// Dapper rewrites <c>{=name}</c> and <c>?name?</c> anywhere in the command text, so a
/// caller string SqlArtisan escaped into a literal would reach the engine as another
/// parameter's value; the package refuses such text before Dapper sees it.
/// </summary>
public class SqlMapperDapperTokenTests
{
    [Fact]
    public void ExecuteScalar_LiteralHoldingLiteralToken_ThrowsArgumentException()
    {
        using SqliteConnection cnn = OpenSeededConnection();

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => cnn.ExecuteScalar(KeyQuery("{=0}")));

        Assert.Equal(
            "Dapper rewrites the token '{=0}' even inside a string literal, so this statement "
                + "cannot run through SqlArtisan.Dapper; bind that string with Sql.Bind(...) "
                + "or run the statement through ADO.NET. (Parameter 'sqlBuilder')",
            ex.Message);
    }

    [Fact]
    public async Task QueryAsync_LiteralHoldingUnboundLiteralToken_ThrowsArgumentException()
    {
        using SqliteConnection cnn = OpenSeededConnection();

        ArgumentException ex = await Assert.ThrowsAsync<ArgumentException>(
            () => cnn.QueryAsync(KeyQuery("{=missing}")));

        Assert.Equal(
            "Dapper rewrites the token '{=missing}' even inside a string literal, so this "
                + "statement cannot run through SqlArtisan.Dapper; bind that string with "
                + "Sql.Bind(...) or run the statement through ADO.NET. (Parameter 'sqlBuilder')",
            ex.Message);
    }

    [Fact]
    public void ExecuteReturningInto_LiteralNamingAnOutputParameter_ThrowsArgumentException()
    {
        using SqliteConnection cnn = OpenSeededConnection();
        DbTable t = new("t");
        ISqlBuilder delete =
            DeleteFrom(t)
            .Where(JsonArrowText(t.Column("doc"), "?x?") == "v")
            .Returning(t.Column("id"))
            .Into(new OutputParameter("x", DbType.Int32));

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => cnn.ExecuteReturningInto(delete));

        Assert.Equal(
            "Dapper rewrites the token '?x?' even inside a string literal, so this statement "
                + "cannot run through SqlArtisan.Dapper; bind that string with Sql.Bind(...) "
                + "or run the statement through ADO.NET. (Parameter 'sqlBuilder')",
            ex.Message);
    }

    [Fact]
    public void ExecuteScalar_TextDapperLeavesAlone_ReturnsTheLiteralsValue()
    {
        using SqliteConnection cnn = OpenSeededConnection();

        Assert.Equal("word-led", cnn.ExecuteScalar(KeyQuery("x{=0}")));
        Assert.Equal("no such parameter", cnn.ExecuteScalar(KeyQuery("?y?")));
    }

    [Fact]
    public void ExecuteScalar_BoundKey_ReturnsTheKeysValue()
    {
        using SqliteConnection cnn = OpenSeededConnection();
        DbTable t = new("t");
        ISqlBuilder query =
            Select(JsonArrowText(t.Column("doc"), Bind("{=0}")))
            .From(t)
            .Where(t.Column("id") == 5);

        Assert.Equal("caller key", cnn.ExecuteScalar(query));
    }

    // A verb calling ToDynamicParameters directly would skip the check unseen.
    [Fact]
    public void SqlMapperSources_RouteEveryVerbThroughTheCheck()
    {
        string dir = Path.Combine(FindRepoRoot(), "src", "SqlArtisan.Dapper");
        string[] offenders = [.. Directory.GetFiles(dir, "SqlMapper*.cs")
            .Where(f => File.ReadAllText(f).Contains(
                ".ToDynamicParameters()", StringComparison.Ordinal))
            .Select(Path.GetFileName)!];

        Assert.True(offenders.Length == 0, string.Join(", ", offenders));
    }

    private static ISqlBuilder KeyQuery(string key)
    {
        DbTable t = new("t");
        return Select(JsonArrowText(t.Column("doc"), key)).From(t).Where(t.Column("id") == 5);
    }

    private static SqliteConnection OpenSeededConnection()
    {
        SqliteConnection cnn = new("Data Source=:memory:");
        cnn.Open();
        cnn.Execute("CREATE TABLE t (id INTEGER NOT NULL, doc TEXT NOT NULL)");
        cnn.Execute(
            """
            INSERT INTO t (id, doc) VALUES (5, '{"{=0}":"caller key","5":"another key","x{=0}":"word-led","?y?":"no such parameter"}')
            """);
        return cnn;
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);

        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SqlArtisan.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir!.FullName;
    }
}
