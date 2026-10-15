using System.Data;
using System.Reflection;
using Dapper;
using Microsoft.Data.Sqlite;
using SqlArtisan.Dapper;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

/// <summary>
/// Dapper rewrites <c>{=name}</c> and <c>?name?</c> anywhere in the command text, so a
/// caller string SqlArtisan escaped into a literal would be rewritten after escaping;
/// the package refuses such text before Dapper sees it.
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
                + "cannot run through SqlArtisan.Dapper; run it through ADO.NET, or bind the "
                + "string with Sql.Bind(...) where its position takes a bound value. "
                + "(Parameter 'sqlBuilder')",
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
                + "statement cannot run through SqlArtisan.Dapper; run it through ADO.NET, or "
                + "bind the string with Sql.Bind(...) where its position takes a bound value. "
                + "(Parameter 'sqlBuilder')",
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
                + "cannot run through SqlArtisan.Dapper; run it through ADO.NET, or bind the "
                + "string with Sql.Bind(...) where its position takes a bound value. "
                + "(Parameter 'sqlBuilder')",
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

    // Calls every public verb, so one added later cannot skip the check unseen.
    [Fact]
    public async Task SqlMapper_EveryVerbGivenLiteralToken_ThrowsArgumentException()
    {
        using SqliteConnection cnn = OpenSeededConnection();
        MethodInfo[] verbs = typeof(SqlArtisan.Dapper.SqlMapper).GetMethods(
            BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        List<string> unguarded = [];

        foreach (MethodInfo declared in verbs)
        {
            MethodInfo verb = declared.IsGenericMethodDefinition
                ? declared.MakeGenericMethod(typeof(object))
                : declared;
            object?[] args = [.. verb.GetParameters().Select(p => VerbArgument(p, cnn))];
            string? message = await ThrownMessage(() => verb.Invoke(null, args));

            if (message != "Dapper rewrites the token '{=0}' even inside a string literal, so "
                + "this statement cannot run through SqlArtisan.Dapper; run it through ADO.NET, "
                + "or bind the string with Sql.Bind(...) where its position takes a bound "
                + "value. (Parameter 'sqlBuilder')")
            {
                unguarded.Add($"{verb}: {message ?? "no throw"}");
            }
        }

        Assert.NotEmpty(verbs);
        Assert.True(unguarded.Count == 0, string.Join(Environment.NewLine, unguarded));
    }

    private static ISqlBuilder KeyQuery(string key)
    {
        DbTable t = new("t");
        return Select(JsonArrowText(t.Column("doc"), key)).From(t).Where(t.Column("id") == 5);
    }

    private static object? VerbArgument(ParameterInfo parameter, SqliteConnection cnn) =>
        parameter.ParameterType switch
        {
            Type type when type.IsInstanceOfType(cnn) => cnn,
            Type type when type == typeof(ISqlBuilder) => KeyQuery("{=0}"),
            Type type when type == typeof(Type) => typeof(object),
            _ when parameter.HasDefaultValue => parameter.DefaultValue,
            Type type when type.IsValueType => Activator.CreateInstance(type),
            _ => null,
        };

    private static async Task<string?> ThrownMessage(Func<object?> call)
    {
        try
        {
            if (call() is Task task)
            {
                await task;
            }

            return null;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is ArgumentException inner)
        {
            return inner.Message;
        }
        catch (ArgumentException ex)
        {
            return ex.Message;
        }
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
}
