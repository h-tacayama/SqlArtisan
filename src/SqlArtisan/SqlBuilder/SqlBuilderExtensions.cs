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
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="cnn"/> is
    /// <see langword="null"/> or its provider is unregistered (either resolves to
    /// <see cref="Dbms.Unknown"/>).</exception>
    /// <exception cref="ArgumentException">The builder was already built or was continued by a set
    /// operator, or the statement breaks a rule only the whole statement shows on the resolved
    /// dialect.</exception>
    public static SqlStatement Build(this ISqlBuilder sqlBuilder, IDbConnection cnn)
    {
        Dbms dbms = DbmsResolver.Resolve(cnn);

        // The DBMS is often supported and only the connection type unregistered (a wrapper
        // around a supported provider), so name the type and both ways out (#679).
        if (dbms == Dbms.Unknown)
        {
            string message = cnn is null
                ? "A null connection maps to no DBMS; pass a connection or call Build(Dbms)."
                : $"Connection type {cnn.GetType().FullName} maps to no DBMS; register it with "
                    + "DbmsResolver.RegisterProvider(...) or call Build(Dbms).";
            throw new ArgumentOutOfRangeException(nameof(dbms), dbms, message);
        }

        return sqlBuilder.Build(dbms);
    }
}
