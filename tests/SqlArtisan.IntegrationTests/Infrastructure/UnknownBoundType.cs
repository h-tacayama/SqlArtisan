using System.Data;
using SqlArtisan.IntegrationTests.Schema;
using static SqlArtisan.Sql;

namespace SqlArtisan.IntegrationTests.Infrastructure;

// A bound value of a type no provider knows, whose ToString() names user 2, run through the
// provider directly as README's manual execution does: Dapper rejects it first (#810).
internal static class UnknownBoundType
{
    internal static List<string> QueryRaw(IDbConnection connection, Dbms dbms)
    {
        UsersTable u = new();
        return QueryRaw(
            connection,
            Select(u.Name).From(u).Where(u.Id == new BindValue(new TextOfTwo())).Build(dbms));
    }

    internal static List<string> QueryRaw(IDbConnection connection, SqlStatement sql)
    {
        using IDbCommand command = connection.CreateCommand();
        command.CommandText = sql.Text;
        sql.Parameters.ForEach((name, bind) =>
        {
            IDbDataParameter parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = bind.Value;
            command.Parameters.Add(parameter);
        });

        using IDataReader reader = command.ExecuteReader();
        List<string> names = new();
        while (reader.Read())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }

    private sealed class TextOfTwo
    {
        public override string ToString() => "2";
    }
}
