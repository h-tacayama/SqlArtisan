using System.Data;

namespace SqlArtisan.Dapper;

// Bypasses Dapper's IEnumerable list expansion: the array must reach ADO.NET as
// one parameter for = ANY (:n); the provider maps the array value natively.
internal sealed class ArrayQueryParameter(object array) :
    global::Dapper.SqlMapper.ICustomQueryParameter
{
    public void AddParameter(IDbCommand command, string name)
    {
        IDbDataParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = array;
        command.Parameters.Add(parameter);
    }
}
