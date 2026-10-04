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
    /// <exception cref="ArgumentException">The builder was already built, or the statement breaks
    /// a rule only the whole statement shows (an aliased <c>UPDATE</c> target with no
    /// <c>FROM</c> on SQL Server, for example).</exception>
    SqlStatement Build();

    /// <summary>
    /// Builds the statement for the given dialect.
    /// </summary>
    /// <param name="dbms">The target engine, whose dialect shapes parameter markers, identifier quoting, and other token-level spellings.</param>
    /// <returns>The rendered SQL text and its bound parameters.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="dbms"/> is <see cref="Dbms.Unknown"/> or an undefined value.</exception>
    /// <exception cref="ArgumentException">The builder was already built, or the statement
    /// breaks a rule only the whole statement shows on <paramref name="dbms"/>.</exception>
    SqlStatement Build(Dbms dbms);
}
