using System.Data;
using SqlArtisan;
using SqlArtisan.Dapper;
using SqlArtisan.IntegrationTests.Schema;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Tests;

// Throwaway probe for #810: what each driver does with a value of a type it does not know.
// Not for merging.
public readonly record struct Probe810Id(long Value);

public sealed class Probe810Stringy
{
    public override string ToString() => "2";
}

public sealed class Probe810Entity
{
    public long Id { get; set; } = 2;
}

internal static class Probe810
{
    internal static string Run(IDbConnection connection, Dbms dbms)
    {
        List<string> lines = new();
        foreach ((string label, object value) in new (string, object)[]
        {
            ("record struct", new Probe810Id(2)),
            ("ToString()=\"2\" class", new Probe810Stringy()),
            ("entity class", new Probe810Entity()),
        })
        {
            lines.Add($"raw {label}: {Raw(connection, dbms, value)}");
            lines.Add($"dapper {label}: {ViaDapper(connection, value)}");
        }

        return $"PROBE810 {dbms} {connection.GetType().FullName}\n" + string.Join("\n", lines);
    }

    private static string Raw(IDbConnection connection, Dbms dbms, object value)
    {
        UsersTable u = new();
        SqlStatement sql = Select(u.Name).From(u).Where(u.Id == new BindValue(value)).Build(dbms);
        try
        {
            using IDbCommand cmd = connection.CreateCommand();
            cmd.CommandText = sql.Text;
            if (cmd is Oracle.ManagedDataAccess.Client.OracleCommand oc)
            {
                oc.BindByName = true;
            }

            sql.Parameters.ForEach((name, bind) =>
            {
                IDbDataParameter p = cmd.CreateParameter();
                p.ParameterName = name;
                p.Value = bind.Value;
                cmd.Parameters.Add(p);
            });
            using IDataReader r = cmd.ExecuteReader();
            List<string> names = new();
            while (r.Read())
            {
                names.Add(r.GetValue(0)?.ToString() ?? "NULL");
            }

            return $"ROWS [{string.Join(",", names)}]";
        }
        catch (Exception e)
        {
            return $"{e.GetType().FullName}: {e.Message}";
        }
    }

    private static string ViaDapper(IDbConnection connection, object value)
    {
        UsersTable u = new();
        try
        {
            return "ROWS [" + string.Join(",", connection.Query<string>(
                Select(u.Name).From(u).Where(u.Id == new BindValue(value)))) + "]";
        }
        catch (Exception e)
        {
            return $"{e.GetType().FullName}: {e.Message}";
        }
    }
}
