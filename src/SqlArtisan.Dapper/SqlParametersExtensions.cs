using Dapper;

namespace SqlArtisan.Dapper;

/// <summary>
/// Converts SqlArtisan parameter bindings into a Dapper
/// <see cref="DynamicParameters"/> bag.
/// </summary>
public static class SqlParametersExtensions
{
    /// <summary>
    /// Converts the SqlArtisan <see cref="SqlParameters"/> into a Dapper
    /// <see cref="DynamicParameters"/>, copying each binding's name, value,
    /// DB type, direction, and size.
    /// </summary>
    /// <param name="parameters">The SqlArtisan parameter bindings to convert.</param>
    /// <returns>A <see cref="DynamicParameters"/> carrying the same bindings, ready for Dapper.</returns>
    /// <remarks>Used mainly by this package's <see cref="SqlMapper"/> connection extensions.</remarks>
    public static DynamicParameters ToDynamicParameters(
        this SqlParameters parameters)
    {
        DynamicParameters dynamicParameters = new();

        parameters.ForEach((name, bind) =>
        {
            if (bind is ArrayBindValue)
            {
                // Not passed as a raw value: Dapper would rewrite the marker
                // into an IN list, corrupting = ANY (:n).
                dynamicParameters.Add(name, new ArrayQueryParameter(bind.Value));
                return;
            }

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
