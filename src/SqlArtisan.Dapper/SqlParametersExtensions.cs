using Dapper;
using SqlArtisan.Dapper.Internal;

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
    /// <remarks>The <see cref="SqlMapper"/> verbs also refuse text holding a token Dapper
    /// rewrites (<c>{=name}</c>, or <c>?name?</c> naming a parameter); passing the text
    /// to Dapper yourself skips that check.</remarks>
    public static DynamicParameters ToDynamicParameters(this SqlParameters parameters)
    {
        DynamicParameters dynamicParameters = new();

        parameters.ForEach((name, bind) =>
        {
            if (bind is BindArrayValue)
            {
                // See ArrayQueryParameter for why the array is not a raw value.
                dynamicParameters.Add(name, new ArrayQueryParameter(bind.Value));
                return;
            }

            // Dapper maps no type for DBNull, so an untyped BindNull() goes in as null, which
            // Dapper binds as DBNull with the type left to the driver, as BindNull() documents.
            object? value = bind.Value is DBNull && bind.DbType is null ? null : bind.Value;
            dynamicParameters.Add(name, value, bind.DbType, bind.Direction, bind.Size);
        });

        return dynamicParameters;
    }
}
