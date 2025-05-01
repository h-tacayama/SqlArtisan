using Dapper;

namespace SqlArtisan.DapperExtensions;

public static class SqlParametersExtensions
{
    public static DynamicParameters ToDynamicParameters(
        this SqlParameters parameters)
    {
        DynamicParameters dynamicParameters = new();

        parameters.ForEach((name, bind) =>
        {
            dynamicParameters.Add(
                name,
                bind.Value,
                bind.DbType,
                bind.Direction,
                bind.Size);
        });

        return dynamicParameters;
    }
}
