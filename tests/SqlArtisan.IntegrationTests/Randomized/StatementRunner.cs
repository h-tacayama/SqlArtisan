using System.Data;
using System.Data.Common;
using System.Text;
using Dapper;
using SqlArtisan.Dapper;

namespace SqlArtisan.IntegrationTests.Randomized;

/// <summary>One statement a case runs, and the C# chain that writes it.</summary>
internal sealed record PlannedStatement(string Label, string CSharp, Func<ISqlBuilder> Builder)
{
    public static PlannedStatement Of(string label, QuerySpec spec) => new(
        label,
        spec.ToCSharp(),
        () => spec.Query.BuildStatement(new BuildContext(spec.Unaliased), out _));
}

/// <summary>A statement's run: its rows, or why it never produced them.</summary>
internal sealed record Outcome(
    string CSharp,
    string Sql,
    string Parameters,
    List<string>? Rows,
    string? Error)
{
    public string Show(string label) => $"{label}\n  C#: {CSharp.Replace("\n", "\n      ")}\n"
        + $"  SQL: {Sql}\n  PARAMS: {Parameters}\n"
        + (Error is not null
            ? $"  ERROR: {Error}\n"
            : $"  ROWS({Rows!.Count}): {string.Join(" ; ", Rows.Take(20))}\n");
}

/// <summary>Builds statements for one engine and runs them on one connection, reading each
/// one's rows as normalized text.</summary>
internal sealed class StatementRunner
{
    private readonly IDbConnection _connection;

    public StatementRunner(IDbConnection connection, Dbms dbms)
    {
        _connection = connection;
        Dbms = dbms;
    }

    public Dbms Dbms { get; }

    // SqlClient refuses a command that lacks the connection's pending transaction, so a
    // statement run inside one is handed it; Microsoft.Data.Sqlite attaches it unasked.
    public Outcome Run(PlannedStatement planned, IDbTransaction? transaction = null)
    {
        SqlStatement statement;
        try
        {
            statement = planned.Builder().Build(Dbms);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException
            or NotSupportedException)
        {
            return new Outcome(
                planned.CSharp,
                "",
                "",
                null,
                $"Build: {ex.GetType().Name}: {ex.Message}");
        }

        StringBuilder parameters = new();
        statement.Parameters.ForEach((name, bind) =>
            parameters.Append($"{name}={ReferenceEvaluator.Normalize(bind.Value)} "));
        try
        {
            using IDataReader reader = _connection.ExecuteReader(
                statement.Text,
                statement.Parameters.ToDynamicParameters(),
                transaction,
                commandTimeout: 60);
            List<string> rows = [];
            while (reader.Read())
            {
                string[] cells = new string[reader.FieldCount];
                for (int i = 0; i < cells.Length; i++)
                {
                    cells[i] = ReferenceEvaluator.Normalize(
                        reader.IsDBNull(i) ? null : reader.GetValue(i));
                }

                rows.Add(string.Join("|", cells));
            }

            return new Outcome(planned.CSharp, statement.Text, parameters.ToString(), rows, null);
        }
        catch (DbException ex)
        {
            return new Outcome(
                planned.CSharp,
                statement.Text,
                parameters.ToString(),
                null,
                ex.Message.Split('\n')[0].Trim());
        }
    }

    /// <summary>Runs <paramref name="work"/> in a transaction of its own, then rolls it back,
    /// so the next statement starts from the same rows. One transaction per statement, since
    /// PostgreSQL aborts the whole transaction after any error.</summary>
    public T RunInRolledBackTransaction<T>(Func<IDbTransaction, T> work)
    {
        using IDbTransaction transaction = _connection.BeginTransaction();
        try
        {
            return work(transaction);
        }
        finally
        {
            transaction.Rollback();
        }
    }
}
