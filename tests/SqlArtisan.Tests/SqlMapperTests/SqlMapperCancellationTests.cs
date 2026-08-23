using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using SqlArtisan.Dapper;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

/// <summary>
/// The wiring half of the #486 gate: the token each async verb takes actually reaches
/// the ADO.NET command — <see cref="SqlMapperSignatureTests"/> proves only that the
/// parameter exists. SQLite in-process keeps this out of the nightly matrix.
/// </summary>
public class SqlMapperCancellationTests
{
    private delegate Task AsyncVerb(
        IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        CancellationToken cancellationToken);

    /// <summary>
    /// Every async verb, keyed the way <see cref="SqlMapperSignatureTests"/> keys the
    /// reflected ones so <see cref="AsyncVerbs_CoverEveryPublicAsyncMethod"/> can hold the
    /// two sets equal. Each entry names the token, the spelling the optional tail preserves.
    /// </summary>
    private static readonly (string Key, AsyncVerb Invoke)[] AsyncVerbs =
    [
        ("SqlMapper.ExecuteAsync",
            (c, b, t) => c.ExecuteAsync(b, cancellationToken: t)),
        ("SqlMapper.ExecuteReturningIntoAsync",
            (c, b, t) => c.ExecuteReturningIntoAsync(b, cancellationToken: t)),
        ("SqlMapper.ExecuteScalarAsync",
            (c, b, t) => c.ExecuteScalarAsync(b, cancellationToken: t)),
        ("SqlMapper.ExecuteScalarAsync<T>",
            (c, b, t) => c.ExecuteScalarAsync<long>(b, cancellationToken: t)),
        ("SqlMapper.QuerySingleAsync(Type)",
            (c, b, t) => c.QuerySingleAsync(typeof(long), b, cancellationToken: t)),
        ("SqlMapper.QuerySingleAsync",
            (c, b, t) => c.QuerySingleAsync(b, cancellationToken: t)),
        ("SqlMapper.QuerySingleAsync<T>",
            (c, b, t) => c.QuerySingleAsync<long>(b, cancellationToken: t)),
        ("SqlMapper.QuerySingleOrDefaultAsync(Type)",
            (c, b, t) => c.QuerySingleOrDefaultAsync(typeof(long), b, cancellationToken: t)),
        ("SqlMapper.QuerySingleOrDefaultAsync",
            (c, b, t) => c.QuerySingleOrDefaultAsync(b, cancellationToken: t)),
        ("SqlMapper.QuerySingleOrDefaultAsync<T>",
            (c, b, t) => c.QuerySingleOrDefaultAsync<long>(b, cancellationToken: t)),
        ("SqlMapper.QueryFirstAsync(Type)",
            (c, b, t) => c.QueryFirstAsync(typeof(long), b, cancellationToken: t)),
        ("SqlMapper.QueryFirstAsync",
            (c, b, t) => c.QueryFirstAsync(b, cancellationToken: t)),
        ("SqlMapper.QueryFirstAsync<T>",
            (c, b, t) => c.QueryFirstAsync<long>(b, cancellationToken: t)),
        ("SqlMapper.QueryFirstOrDefaultAsync(Type)",
            (c, b, t) => c.QueryFirstOrDefaultAsync(typeof(long), b, cancellationToken: t)),
        ("SqlMapper.QueryFirstOrDefaultAsync",
            (c, b, t) => c.QueryFirstOrDefaultAsync(b, cancellationToken: t)),
        ("SqlMapper.QueryFirstOrDefaultAsync<T>",
            (c, b, t) => c.QueryFirstOrDefaultAsync<long>(b, cancellationToken: t)),
        ("SqlMapper.QueryAsync(Type)",
            (c, b, t) => c.QueryAsync(typeof(long), b, cancellationToken: t)),
        ("SqlMapper.QueryAsync",
            (c, b, t) => c.QueryAsync(b, cancellationToken: t)),
        ("SqlMapper.QueryAsync<T>",
            (c, b, t) => c.QueryAsync<long>(b, cancellationToken: t)),
        ("SqlMapper.QueryMultipleAsync",
            (c, b, t) => c.QueryMultipleAsync(b, cancellationToken: t)),
        ("SqlMapper.ExecuteReaderAsync",
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
    /// A canceled token is the one input visible without a slow query: ADO.NET rejects the
    /// command before it runs. Which exception surfaces depends on the verb, so the assertion
    /// is on <see cref="OperationCanceledException"/> rather than an exact type.
    /// </summary>
    [Fact]
    public async Task AsyncVerbs_CanceledToken_ThrowsOperationCanceledException()
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
