namespace SqlArtisan.Analyzers;

/// <summary>
/// The minimum engine version a matrix entry needs per dialect, mirroring
/// <see cref="DbmsSupport"/>'s named-slot shape. A <see langword="null"/> slot
/// means no recorded boundary for that dialect — the entry's plain
/// <see cref="DbmsSupport"/> bool decides, exactly as before #263.
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
