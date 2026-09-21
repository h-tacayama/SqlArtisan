namespace SqlArtisan;

/// <summary>
/// A statement builder that renders a <see cref="SqlStatement"/> for a target dialect.
/// </summary>
public interface ISqlBuilder
{
    /// <summary>
    /// Builds the statement for the default dialect (<see cref="SqlArtisanConfig.DefaultDbms"/>).
    /// </summary>
    /// <returns>The rendered SQL text and its bound parameters.</returns>
    SqlStatement Build();

    /// <summary>
    /// Builds the statement for the given dialect.
    /// </summary>
    /// <param name="dbms">The target engine, whose dialect shapes parameter markers, identifier quoting, and other token-level spellings.</param>
    /// <returns>The rendered SQL text and its bound parameters.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="dbms"/> is <see cref="Dbms.Unknown"/> or an undefined value.</exception>
    SqlStatement Build(Dbms dbms);
}
