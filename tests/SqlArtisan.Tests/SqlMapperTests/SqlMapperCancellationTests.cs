using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using SqlArtisan.Dapper;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

/// <summary>
/// The wiring half of the #486 gate: that the token each async verb now takes
/// actually reaches the ADO.NET command. Nothing else here can see it — the SQL
/// text is unchanged, and <see cref="SqlMapperSignatureTests"/> only proves the
/// parameter exists, not that it is passed on. SQLite in-process keeps this in the
/// unit suite instead of the nightly integration matrix.
/// </summary>
public class SqlMapperCancellationTests
{
    private delegate Task AsyncVerb(
        IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        CancellationToken cancellationToken);

    /// <summary>
    /// Every async verb, keyed the way <see cref="SqlMapperSignatureTests"/> keys
    /// the reflected ones so <see cref="AsyncVerbs_CoverEveryPublicAsyncMethod"/>
    /// can hold the two sets equal. Each entry passes the token by name, which is
    /// the spelling the optional-parameter shape exists to preserve.
    /// </summary>
    private static readonly (string Key, AsyncVerb Invoke)[] AsyncVerbs =
    [
        ("ExecuteAsync",
            (c, b, t) => c.ExecuteAsync(b, cancellationToken: t)),
        ("ExecuteReturningIntoAsync",
            (c, b, t) => c.ExecuteReturningIntoAsync(b, cancellationToken: t)),
        ("ExecuteScalarAsync",
            (c, b, t) => c.ExecuteScalarAsync(b, cancellationToken: t)),
        ("ExecuteScalarAsync<T>",
            (c, b, t) => c.ExecuteScalarAsync<long>(b, cancellationToken: t)),
        ("QuerySingleAsync(Type)",
            (c, b, t) => c.QuerySingleAsync(typeof(long), b, cancellationToken: t)),
        ("QuerySingleAsync",
            (c, b, t) => c.QuerySingleAsync(b, cancellationToken: t)),
        ("QuerySingleAsync<T>",
            (c, b, t) => c.QuerySingleAsync<long>(b, cancellationToken: t)),
        ("QuerySingleOrDefaultAsync(Type)",
            (c, b, t) => c.QuerySingleOrDefaultAsync(typeof(long), b, cancellationToken: t)),
        ("QuerySingleOrDefaultAsync",
            (c, b, t) => c.QuerySingleOrDefaultAsync(b, cancellationToken: t)),
        ("QuerySingleOrDefaultAsync<T>",
            (c, b, t) => c.QuerySingleOrDefaultAsync<long>(b, cancellationToken: t)),
        ("QueryFirstAsync(Type)",
            (c, b, t) => c.QueryFirstAsync(typeof(long), b, cancellationToken: t)),
        ("QueryFirstAsync",
            (c, b, t) => c.QueryFirstAsync(b, cancellationToken: t)),
        ("QueryFirstAsync<T>",
            (c, b, t) => c.QueryFirstAsync<long>(b, cancellationToken: t)),
        ("QueryFirstOrDefaultAsync(Type)",
            (c, b, t) => c.QueryFirstOrDefaultAsync(typeof(long), b, cancellationToken: t)),
        ("QueryFirstOrDefaultAsync",
            (c, b, t) => c.QueryFirstOrDefaultAsync(b, cancellationToken: t)),
        ("QueryFirstOrDefaultAsync<T>",
            (c, b, t) => c.QueryFirstOrDefaultAsync<long>(b, cancellationToken: t)),
        ("QueryAsync(Type)",
            (c, b, t) => c.QueryAsync(typeof(long), b, cancellationToken: t)),
        ("QueryAsync",
            (c, b, t) => c.QueryAsync(b, cancellationToken: t)),
        ("QueryAsync<T>",
            (c, b, t) => c.QueryAsync<long>(b, cancellationToken: t)),
        ("QueryMultipleAsync",
            (c, b, t) => c.QueryMultipleAsync(b, cancellationToken: t)),
        ("ExecuteReaderAsync",
            (c, b, t) => c.ExecuteReaderAsync(b, cancellationToken: t)),
    ];

    /// <summary>
    /// A verb absent from <see cref="AsyncVerbs"/> would be silently uncovered, so
    /// the list is held equal to the reflected surface rather than merely subset of it.
    /// </summary>
    [Fact]
    public void AsyncVerbs_CoverEveryPublicAsyncMethod()
    {
        Assert.Equal(
            SqlMapperSignatureTests.AsyncMethodKeys(),
            [.. AsyncVerbs.Select(v => v.Key).OrderBy(k => k, StringComparer.Ordinal)]);
    }

    /// <summary>
    /// A canceled token is the one input whose effect is visible without a slow query:
    /// ADO.NET rejects the command before it runs. Dapper's <c>Execute</c>/
    /// <c>ExecuteScalar</c> paths surface it as <see cref="TaskCanceledException"/> and
    /// the reader paths as <see cref="OperationCanceledException"/>, hence ThrowsAny.
    /// </summary>
    [Fact]
    public async Task AsyncVerbs_CanceledToken_ThrowOperationCanceledException()
    {
        using SqliteConnection cnn = OpenSeededConnection();
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        foreach ((string key, AsyncVerb invoke) in AsyncVerbs)
        {
            Exception ex = await Record.ExceptionAsync(() => invoke(cnn, Query(), cts.Token));

            Assert.True(
                ex is OperationCanceledException,
                $"{key} did not honor the canceled token — it threw "
                    + $"{ex?.GetType().Name ?? "nothing"}. The token is dropped somewhere "
                    + "between the extension method and the CommandDefinition.");
        }
    }

    /// <summary>
    /// A fresh chain per call: <c>Build()</c> finishes a builder, so one instance
    /// cannot serve all 21 verbs.
    /// </summary>
    private static ISqlBuilder Query()
    {
        DbTable t = new("t");
        return Select(t.Column("id")).From(t);
    }

    private static SqliteConnection OpenSeededConnection()
    {
        SqliteConnection cnn = new("Data Source=:memory:");
        cnn.Open();
        cnn.Execute("CREATE TABLE t (id INTEGER NOT NULL)");
        cnn.Execute("INSERT INTO t (id) VALUES (1)");
        return cnn;
    }
}
