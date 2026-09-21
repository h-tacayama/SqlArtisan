using System.Data;

namespace SqlArtisan.TableClassGen;

// The one place a catalog query binds a parameter by hand — the rest bind
// through the library; three readers each carried a private copy before this.
internal static class CatalogCommand
{
    public static void AddParameter(IDbCommand command, string name, string value)
    {
        IDbDataParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
