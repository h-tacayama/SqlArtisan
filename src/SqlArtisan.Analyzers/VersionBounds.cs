namespace SqlArtisan.Analyzers;

/// <summary>
/// The minimum engine version a matrix entry needs per dialect, mirroring
/// <see cref="DbmsSupport"/>'s slot shape; a <see langword="null"/> slot means
/// the plain <see cref="DbmsSupport"/> bool decides (#263).
/// </summary>
internal readonly struct VersionBounds
{
    public VersionBounds(
        EngineVersion? mySql = null,
        EngineVersion? oracle = null,
        EngineVersion? postgreSql = null,
        EngineVersion? sqlite = null,
        EngineVersion? sqlServer = null)
    {
        MySql = mySql;
        Oracle = oracle;
        PostgreSql = postgreSql;
        Sqlite = sqlite;
        SqlServer = sqlServer;
    }

    public EngineVersion? MySql { get; }
    public EngineVersion? Oracle { get; }
    public EngineVersion? PostgreSql { get; }
    public EngineVersion? Sqlite { get; }
    public EngineVersion? SqlServer { get; }

    public EngineVersion? MinFor(TargetDbms target) => target switch
    {
        TargetDbms.MySql => MySql,
        TargetDbms.Oracle => Oracle,
        TargetDbms.PostgreSql => PostgreSql,
        TargetDbms.Sqlite => Sqlite,
        TargetDbms.SqlServer => SqlServer,
        _ => null,
    };
}
