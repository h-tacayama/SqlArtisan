using Dapper;
using Microsoft.Data.Sqlite;
using SqlArtisan.Dapper;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

/// <summary>
/// The flags half of the command shape <c>ToCommand</c> assembles: each verb passes
/// the <see cref="CommandFlags"/> Dapper's own string overload for that verb passes,
/// which nothing but that method's doc says. <see cref="CommandFlags.Buffered"/> is
/// what makes a <c>QueryAsync</c> result a materialized sequence instead of a
/// deferred one holding the reader; a copy of the <c>QueryFirst</c> template would
/// pass <see cref="CommandFlags.None"/> there and leave user code that closes the
/// connection before enumerating to break at run time, every other gate green. The
/// converse mix-up is not gatable and not worth gating: Dapper's row path never
/// consults the flag, so <c>Buffered</c> on <c>QueryFirst</c>/<c>QuerySingle</c> is
/// indistinguishable from <c>None</c> and stays pinned by <c>ToCommand</c>'s doc.
/// </summary>
public class SqlMapperBufferingTests
{
    private delegate Task<IEnumerable<object>> SequenceVerb(
        SqliteConnection cnn,
        ISqlBuilder sqlBuilder);

    /// <summary>
    /// Every async verb returning a sequence, keyed the way
    /// <see cref="SqlMapperSignatureTests"/> keys the reflected ones so
    /// <see cref="SequenceVerbs_CoverEverySequenceReturningAsyncMethod"/> can hold
    /// the two sets equal. <c>Cast</c> only re-types the sequence the verb returned;
    /// a deferred one stays deferred through it.
    /// </summary>
    private static readonly (string Key, SequenceVerb Invoke)[] SequenceVerbs =
    [
        ("SqlMapper.QueryAsync(Type)",
            async (c, b) => await c.QueryAsync(typeof(long), b)),
        ("SqlMapper.QueryAsync",
            async (c, b) => (await c.QueryAsync(b)).Cast<object>()),
        ("SqlMapper.QueryAsync<T>",
            async (c, b) => (await c.QueryAsync<long>(b)).Cast<object>()),
    ];

    /// <summary>
    /// A verb absent from <see cref="SequenceVerbs"/> would be silently uncovered, so
    /// the list is held equal to the reflected surface rather than merely subset of it.
    /// </summary>
    [Fact]
    public void SequenceVerbs_CoverEverySequenceReturningAsyncMethod()
    {
        Assert.Equal(
            SqlMapperSignatureTests.SequenceReturningAsyncMethodKeys(),
            [.. SequenceVerbs.Select(v => v.Key).OrderBy(k => k, StringComparer.Ordinal)]);
    }

    /// <summary>
    /// Closing the connection first is what tells the two flags apart: a buffered
    /// result is already in memory, an unbuffered one still needs the reader that
    /// went with the connection.
    /// </summary>
    [Fact]
    public async Task SequenceVerbs_AfterConnectionClose_ReturnTheirRows()
    {
        List<string> offenders = [];

        foreach ((string key, SequenceVerb invoke) in SequenceVerbs)
        {
            IEnumerable<object> rows;

            using (SqliteConnection cnn = OpenSeededConnection())
            {
                rows = await invoke(cnn, Query());
            }

            int count = 0;
            Exception? ex = Record.Exception(() => count = rows.Count());

            if (ex is not null || count != 2)
            {
                offenders.Add(
                    $"{key} returned {ex?.GetType().Name ?? count.ToString()} rather than "
                        + "its 2 rows");
            }
        }

        Assert.True(
            offenders.Count == 0,
            $"{offenders.Count} sequence verbs do not buffer their result — one passes "
                + $"`{nameof(CommandFlags)}.{nameof(CommandFlags.None)}` where "
                + $"`{nameof(CommandFlags.Buffered)}` belongs, so the sequence still holds "
                + "the reader:\n  "
                + string.Join("\n  ", offenders));
    }

    /// <summary>
    /// A fresh chain per call: <c>Build()</c> finishes a builder, so one instance
    /// cannot serve every verb.
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
        cnn.Execute("INSERT INTO t (id) VALUES (2)");
        return cnn;
    }
}
