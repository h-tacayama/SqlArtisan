namespace SqlArtisan;

/// <summary>
/// Library-wide configuration.
/// </summary>
public static class SqlArtisanConfig
{
    /// <summary>
    /// Gets the dialect used when a statement is built without an explicit one (<see cref="ISqlBuilder.Build()"/>); defaults to <see cref="Dbms.PostgreSql"/>. Change it with <see cref="SetDefaultDbms(Dbms)"/>.
    /// </summary>
    public static Dbms DefaultDbms { get; private set; } = Dbms.PostgreSql;

    /// <summary>
    /// Sets the dialect used by parameterless <see cref="ISqlBuilder.Build()"/> calls.
    /// </summary>
    /// <param name="dbms">The engine to use as the default; not <see cref="Dbms.Unknown"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="dbms"/> is <see cref="Dbms.Unknown"/> or an undefined value.</exception>
    public static void SetDefaultDbms(Dbms dbms)
    {
        if (dbms == Dbms.Unknown || !Enum.IsDefined(dbms))
        {
            throw new ArgumentOutOfRangeException(nameof(dbms), dbms, "Unsupported DBMS.");
        }

        DefaultDbms = dbms;
    }
}
