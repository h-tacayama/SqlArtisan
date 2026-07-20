using System.Data;

namespace SqlArtisan;

/// <summary>
/// Extension methods for <see cref="ISqlBuilder"/>.
/// </summary>
public static class SqlBuilderExtensions
{
    /// <summary>
    /// Builds the statement for the dialect inferred from an ADO.NET connection.
    /// </summary>
    /// <param name="sqlBuilder">The builder to render.</param>
    /// <param name="cnn">The connection whose provider type selects the dialect via <see cref="DbmsResolver.Resolve(IDbConnection)"/>; an unregistered provider yields <see cref="Dbms.Unknown"/>.</param>
    /// <returns>The rendered SQL text and its bound parameters.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The connection's provider is unregistered (resolves to <see cref="Dbms.Unknown"/>).</exception>
    public static SqlStatement Build(this ISqlBuilder sqlBuilder, IDbConnection cnn)
    {
        Dbms dbms = DbmsResolver.Resolve(cnn);
        return sqlBuilder.Build(dbms);
    }
}
